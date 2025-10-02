// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#include <mgba/flags.h>

#include <mgba/core/core.h>
#include <mgba/core/log.h>
#include <mgba/core/serialize.h>
#include <mgba/gba/core.h>
#include <mgba/gba/interface.h>
#include <mgba-util/vfs.h>

#ifdef GSE_SHARED
	#ifdef _WIN32
		#define GSE_EXPORT __declspec(dllexport)
	#else
		#define GSE_EXPORT __attribute__((visibility("default")))
	#endif
#else
	#define GSE_EXPORT
#endif

static void stub_logger(struct mLogger* logger, int category, enum mLogLevel level, const char* format, va_list args)
{
	(void)logger;
	(void)category;
	(void)level;
	(void)format;
	(void)args;
}

static void set_default_logger()
{
	static bool default_logger_set = false;
	if (!default_logger_set)
	{
		static struct mLogger logger;
		logger.log = stub_logger;
		mLogSetDefaultLogger(&logger);
		default_logger_set = true;
	}
}

typedef struct
{
	struct mRTCSource base;
	int64_t unix_time;
	uint32_t cycles;
	uint32_t frequency;
	bool deserialization_success;
} GSE_Rtc;

static time_t rtc_unix_time_callback(struct mRTCSource* source)
{
	GSE_Rtc* rtc = (GSE_Rtc*)source;
	return rtc->unix_time;
}

static const uint64_t GSE_RTC_MAGIC = ((uint64_t)'G' << 0) | ((uint64_t)'S' << 8) | ((uint64_t)'E' << 16) |
	((uint64_t)'R' << 24) | ((uint64_t)'T' << 32) | ((uint64_t)'C' << 40) | ((uint64_t)'S' << 48) | ((uint64_t)'T' << 56);

typedef struct
{
	uint64_t magic;
	int64_t unix_time;
	uint32_t cycles;
} GSE_RtcState;

static void rtc_serialize_callback(struct mRTCSource* source, struct mStateExtdataItem* item)
{
	GSE_Rtc* rtc = (GSE_Rtc*)source;
	GSE_RtcState* rtcState = malloc(sizeof(GSE_RtcState));
	if (!rtcState)
	{
		memset(item, 0, sizeof(struct mStateExtdataItem));
		return;
	}

	STORE_64LE(GSE_RTC_MAGIC, 0, &rtcState->magic);
	STORE_64LE(rtc->unix_time, 0, &rtcState->unix_time);
	STORE_32LE(rtc->cycles, 0, &rtcState->cycles);
	item->size = sizeof(GSE_RtcState);
	item->data = rtcState;
	item->clean = free;
}

static bool rtc_deserialize_callback(struct mRTCSource* source, const struct mStateExtdataItem* item)
{
	GSE_Rtc* rtc = (GSE_Rtc*)source;
	if (!item->data || item->size != sizeof(GSE_RtcState))
	{
		return false;
	}

	GSE_RtcState* rtcState = item->data;
	uint64_t magic;
	LOAD_64LE(magic, 0, &rtcState->magic);
	if (magic != GSE_RTC_MAGIC)
	{
		return false;
	}

	LOAD_64LE(rtc->unix_time, 0, &rtcState->unix_time);
	LOAD_32LE(rtc->cycles, 0, &rtcState->cycles);
	rtc->cycles %= rtc->frequency;
	rtc->deserialization_success = true;
	return true;
}

typedef struct
{
	struct mAVStream base;
	uint32_t sample_rate;
	uint32_t sample_rate_quotient;
	uint32_t sample_index;
	struct mStereoSample samples[0x2000];
} GSE_Audio;

static void audio_rate_changed_callback(struct mAVStream* stream, unsigned rate)
{
	GSE_Audio* audio = (GSE_Audio*)stream;
	audio->sample_rate_quotient = audio->sample_rate / rate;
}

static void post_audio_frame_callback(struct mAVStream* stream, int16_t left, int16_t right)
{
	GSE_Audio* audio = (GSE_Audio*)stream;
	for (unsigned i = 0; i < audio->sample_rate_quotient; i++)
	{
		if (audio->sample_index == sizeof(audio->samples) / sizeof(audio->samples[0]))
		{
			break;
		}

		audio->samples[audio->sample_index].left = left;
		audio->samples[audio->sample_index].right = right;
		audio->sample_index++;
	}
}

typedef struct
{
	struct mCore* core;
	void* rom;
	struct VFile* rom_vf;
	uint8_t bios[0x4000]; // GBA_SIZE_BIOS
	struct VFile* bios_vf;
	uint8_t sram[0x20000 + 16]; // GBA_SIZE_FLASH1M + sizeof(struct GBASavedataRTCBuffer)
	struct VFile* sram_vf;
	mColor vbuf[GBA_VIDEO_HORIZONTAL_PIXELS * GBA_VIDEO_VERTICAL_PIXELS];
	uint32_t color_lut[0x8000];
	struct VFile* state_vf;
	GSE_Rtc rtc_source;
	GSE_Audio audio_stream;
} GSE_ctx;

GSE_EXPORT void mgba_destroy(GSE_ctx* ctx);

GSE_EXPORT GSE_ctx* mgba_create(const uint8_t* romData, uint32_t romLength, const uint8_t* biosData, uint32_t biosLength, bool forceDisableRtc, int64_t rtcStartTime)
{
	set_default_logger();

	GSE_ctx* ctx = calloc(1, sizeof(GSE_ctx));
	if (!ctx)
	{
		return NULL;
	}

	if (biosLength != sizeof(ctx->bios))
	{
		free(ctx);
		return NULL;
	}

	ctx->core = GBACoreCreate();
	if (!ctx->core)
	{
		free(ctx);
		return NULL;
	}

	if (!ctx->core->init(ctx->core))
	{
		ctx->core->deinit(ctx->core);
		free(ctx);
		return NULL;
	}

	mCoreInitConfig(ctx->core, NULL);
	mCoreConfigSetValue(&ctx->core->config, "idleOptimization", "ignore");
	mCoreConfigSetIntValue(&ctx->core->config, "vbaBugCompat", 0); // BIOS is forced, so this doesn't really matter
	ctx->core->opts.volume = 0x100;
	ctx->core->loadConfig(ctx->core, &ctx->core->config);

	ctx->rom = malloc(romLength);
	if (!ctx->rom)
	{
		mgba_destroy(ctx);
		return NULL;
	}

	memcpy(ctx->rom, romData, romLength);
	ctx->rom_vf = VFileFromMemory(ctx->rom, romLength);
	if (!ctx->rom_vf)
	{
		mgba_destroy(ctx);
		return NULL;
	}

	if (!ctx->core->loadROM(ctx->core, ctx->rom_vf))
	{
		ctx->rom_vf->close(ctx->rom_vf);
		mgba_destroy(ctx);
		return NULL;
	}

	memcpy(ctx->bios, biosData, sizeof(ctx->bios));
	ctx->bios_vf = VFileFromMemory(ctx->bios, sizeof(ctx->bios));
	if (!ctx->bios_vf)
	{
		mgba_destroy(ctx);
		return NULL;
	}

	if (!ctx->core->loadBIOS(ctx->core, ctx->bios_vf, 0))
	{
		ctx->bios_vf->close(ctx->bios_vf);
		mgba_destroy(ctx);
		return NULL;
	}

	memset(ctx->sram, 0xFF, sizeof(ctx->sram));
	ctx->sram_vf = VFileFromMemory(ctx->sram, sizeof(ctx->sram));
	if (!ctx->sram_vf)
	{
		mgba_destroy(ctx);
		return NULL;
	}

	ctx->sram_vf->truncate(ctx->sram_vf, 0);
	ctx->core->loadSave(ctx->core, ctx->sram_vf);

	ctx->rtc_source.base.sample = NULL;
	ctx->rtc_source.base.unixTime = rtc_unix_time_callback;
	ctx->rtc_source.base.serialize = rtc_serialize_callback;
	ctx->rtc_source.base.deserialize = rtc_deserialize_callback;
	ctx->rtc_source.unix_time = rtcStartTime;
	ctx->rtc_source.cycles = 0;
	ctx->rtc_source.frequency = ctx->core->frequency(ctx->core);
	mCoreSetRTC(ctx->core, &ctx->rtc_source.base);

	ctx->core->setVideoBuffer(ctx->core, ctx->vbuf, GBA_VIDEO_HORIZONTAL_PIXELS);
	ctx->core->setAudioBufferSize(ctx->core, 8192);

	ctx->audio_stream.base.videoDimensionsChanged = NULL;
	ctx->audio_stream.base.audioRateChanged = audio_rate_changed_callback;
	ctx->audio_stream.base.postVideoFrame = NULL;
	ctx->audio_stream.base.postAudioFrame = post_audio_frame_callback;
	ctx->audio_stream.base.postAudioBuffer = NULL;
	ctx->audio_stream.sample_rate = ctx->core->frequency(ctx->core) / (0x200 >> 3); // 262144Hz, maximum sample rate by the core, all other sample rates are divided from this
	ctx->audio_stream.sample_rate_quotient = ctx->audio_stream.sample_rate / ctx->core->audioSampleRate(ctx->core);
	ctx->audio_stream.sample_index = 0;
	ctx->core->setAVStream(ctx->core, &ctx->audio_stream.base);

	// ensure default overrides get applied immediately
	ctx->core->reset(ctx->core);

	struct mGameInfo gameInfo;
	ctx->core->getGameInfo(ctx->core, &gameInfo);

	struct GBACartridgeOverride override;
	memcpy(override.id, gameInfo.code, sizeof(override.id));
	if (GBAOverrideFind(NULL, &override))
	{
		if (forceDisableRtc)
		{
			override.hardware &= ~HW_RTC;
		}

		override.idleLoop = GBA_IDLE_LOOP_NONE;
		ctx->core->setOverride(ctx->core, &override);
		ctx->core->reset(ctx->core);
	}
	else if (forceDisableRtc)
	{
		override.savetype = GBA_SAVEDATA_AUTODETECT;
		override.hardware = HW_NONE;
		override.idleLoop = GBA_IDLE_LOOP_NONE;
		ctx->core->setOverride(ctx->core, &override);
		ctx->core->reset(ctx->core);
	}

	return ctx;
}

GSE_EXPORT void mgba_destroy(GSE_ctx* ctx)
{
	mCoreConfigDeinit(&ctx->core->config);
	ctx->core->deinit(ctx->core); // this will close rom/bios/sram vfiles
	free(ctx->rom);
	free(ctx);
}

GSE_EXPORT void mgba_setcolorlut(GSE_ctx* ctx, const uint32_t* colorLut)
{
	memcpy(ctx->color_lut, colorLut, sizeof(ctx->color_lut));
}

GSE_EXPORT void mgba_advance(GSE_ctx* ctx, uint16_t buttons, uint32_t* videoBuf, int16_t* soundBuf, uint32_t* samples, uint32_t* cpuCycles)
{
	int32_t startCycle = mTimingCurrentTime(ctx->core->timing);

	ctx->core->setKeys(ctx->core, buttons);
	ctx->core->runFrame(ctx->core);

	for (unsigned i = 0; i < GBA_VIDEO_HORIZONTAL_PIXELS * GBA_VIDEO_VERTICAL_PIXELS; i++)
	{
		videoBuf[i] = ctx->color_lut[ctx->vbuf[i] & 0x7FFF];
	}

	*samples = ctx->audio_stream.sample_index;
	memcpy(soundBuf, ctx->audio_stream.samples, ctx->audio_stream.sample_index * sizeof(struct mStereoSample));
	ctx->audio_stream.sample_index = 0;

	*cpuCycles = mTimingCurrentTime(ctx->core->timing) - startCycle;

	ctx->rtc_source.cycles += *cpuCycles;
	if (ctx->rtc_source.cycles >= ctx->rtc_source.frequency)
	{
		ctx->rtc_source.unix_time++;
		ctx->rtc_source.cycles -= ctx->rtc_source.frequency;
	}
}

GSE_EXPORT void mgba_reset(GSE_ctx* ctx)
{
	ctx->core->reset(ctx->core);
}

GSE_EXPORT uint32_t mgba_savesavedata(GSE_ctx* ctx, uint8_t* dest)
{
	uint32_t size = ctx->sram_vf->size(ctx->sram_vf);
	ctx->sram_vf->seek(ctx->sram_vf, 0, SEEK_SET);
	ctx->sram_vf->read(ctx->sram_vf, dest, size);
	return size;
}

GSE_EXPORT void mgba_loadsavedata(GSE_ctx* ctx, const uint8_t* data, uint32_t size, int64_t rtcStartTime)
{
	ctx->sram_vf->seek(ctx->sram_vf, 0, SEEK_SET);
	ctx->sram_vf->write(ctx->sram_vf, data, size);
	ctx->rtc_source.unix_time = rtcStartTime;
	ctx->rtc_source.cycles = 0;
}

GSE_EXPORT int64_t mgba_getrtctime(GSE_ctx* ctx)
{
	return ctx->rtc_source.unix_time;
}

GSE_EXPORT uint32_t mgba_getsavestatelength(GSE_ctx* ctx)
{
	if (ctx->state_vf)
	{
		ctx->state_vf->close(ctx->state_vf);
		ctx->state_vf = NULL;
	}

	ctx->state_vf = VFileMemChunk(NULL, 0);
	if (!mCoreSaveStateNamed(ctx->core, ctx->state_vf, SAVESTATE_SAVEDATA | SAVESTATE_RTC))
	{
		ctx->state_vf->close(ctx->state_vf);
		ctx->state_vf = NULL;
		return 0;
	}

	return ctx->state_vf->size(ctx->state_vf);
}

GSE_EXPORT bool mgba_savestate(GSE_ctx* ctx, uint8_t* stateBuf)
{
	if (!ctx->state_vf)
	{
		return false;
	}

	ctx->state_vf->seek(ctx->state_vf, 0, SEEK_SET);
	ctx->state_vf->read(ctx->state_vf, stateBuf, ctx->state_vf->size(ctx->state_vf));
	ctx->state_vf->close(ctx->state_vf);
	ctx->state_vf = NULL;
	return true;
}

GSE_EXPORT bool mgba_loadstate(GSE_ctx* ctx, const uint8_t* stateBuf, uint32_t size, int64_t rtcTime)
{
	ctx->rtc_source.deserialization_success = false;

	struct VFile* vf = VFileFromConstMemory(stateBuf, size);
	bool ret = mCoreLoadStateNamed(ctx->core, vf, SAVESTATE_SAVEDATA | SAVESTATE_RTC);
	vf->close(vf);

	if (!ctx->rtc_source.deserialization_success)
	{
		ctx->rtc_source.unix_time = rtcTime;
		ctx->rtc_source.cycles = 0;
	}

	return ret;
}

enum MemoryBlock
{
	MEMORY_BLOCK_IWRAM = 0,
	MEMORY_BLOCK_EWRAM = 1,
	MEMORY_BLOCK_SRAM = 2,
};

GSE_EXPORT void mgba_getmemoryblock(GSE_ctx* ctx, enum MemoryBlock which, void** ptr, size_t* len)
{
	*ptr = NULL;
	*len = 0;

	const struct mCoreMemoryBlock* blocks;
	size_t numBlocks = ctx->core->listMemoryBlocks(ctx->core, &blocks);

	switch (which)
	{
		case MEMORY_BLOCK_IWRAM:
			for (size_t i = 0; i < numBlocks; i++)
			{
				if (blocks[i].start == 0x3000000) // GBA_BASE_IWRAM
				{
					*ptr = ctx->core->getMemoryBlock(ctx->core, blocks[i].id, len);
					break;
				}
			}
			break;
		case MEMORY_BLOCK_EWRAM:
			for (size_t i = 0; i < numBlocks; i++)
			{
				if (blocks[i].start == 0x2000000) // GBA_BASE_EWRAM
				{
					*ptr = ctx->core->getMemoryBlock(ctx->core, blocks[i].id, len);
					break;
				}
			}
			break;
		case MEMORY_BLOCK_SRAM:
			// we don't know how much sram will actually be used yet, so give the entire block
			*ptr = ctx->sram;
			*len = sizeof(ctx->sram);
			break;
	}
}

// TODO: Remove this eventually (this is only needed due to localtime_r / mktime usage within the core, which use user timezones!)

#ifdef _WIN32
GSE_EXPORT errno_t _localtime64_s(struct tm* tm, const __time64_t* time)
{
	return _gmtime64_s(tm, time);
}

GSE_EXPORT __time64_t _mktime64(struct tm* tm)
{
	return _mkgmtime64(tm);
}

GSE_EXPORT errno_t _localtime32_s(struct tm* tm, const __time32_t* time)
{
	return _gmtime32_s(tm, time);
}

GSE_EXPORT __time32_t _mktime32(struct tm* tm)
{
	return _mkgmtime32(tm);
}
#else
struct tm* localtime_r(const time_t* restrict time, struct tm* restrict tm)
{
	return gmtime_r(time, tm);
}

time_t mktime(struct tm* tm)
{
	return timegm(tm);
}
#endif
