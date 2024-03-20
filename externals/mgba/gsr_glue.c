// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#include <mgba/core/blip_buf.h>
#include <mgba/core/core.h>
#include <mgba/core/log.h>
#include <mgba/core/serialize.h>
#include <mgba/internal/gba/gba.h>
#include <mgba/internal/gba/memory.h>
#include <mgba/internal/gba/savedata.h>
#include <mgba/gba/core.h>
#include <mgba/gba/interface.h>
#include <mgba-util/vfs.h>

#ifdef GSR_SHARED
	#ifdef _WIN32
		#define GSR_EXPORT __declspec(dllexport)
	#else
		#define GSR_EXPORT __attribute__((visibility("default")))
	#endif
#else
	#define GSR_EXPORT
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
	struct mCore* core;
	void* rom;
	struct VFile* rom_vf;
	uint8_t bios[SIZE_BIOS];
	struct VFile* bios_vf;
	uint8_t sram[SIZE_CART_FLASH1M + sizeof(struct GBASavedataRTCBuffer)];
	struct VFile* sram_vf;
	color_t vbuf[GBA_VIDEO_HORIZONTAL_PIXELS * GBA_VIDEO_VERTICAL_PIXELS];
	uint32_t color_lut[0x8000];
	struct VFile* state_vf;
	bool force_disable_rtc;
} GSR_ctx;

GSR_EXPORT void mgba_destroy(GSR_ctx* ctx);
GSR_EXPORT void mgba_reset(GSR_ctx* ctx);

GSR_EXPORT GSR_ctx* mgba_create(const uint8_t* romData, uint32_t romLength, const uint8_t* biosData, uint32_t biosLength, bool forceDisableRtc)
{
	set_default_logger();

	GSR_ctx* ctx = calloc(1, sizeof(GSR_ctx));
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

	mCoreInitConfig(ctx->core, NULL);

	if (!ctx->core->init(ctx->core))
	{
		mgba_destroy(ctx);
		return NULL;
	}

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

	ctx->core->loadSave(ctx->core, ctx->sram_vf);

	ctx->core->setVideoBuffer(ctx->core, ctx->vbuf, GBA_VIDEO_HORIZONTAL_PIXELS);
	ctx->core->setAudioBufferSize(ctx->core, 1024);

	blip_set_rates(ctx->core->getAudioChannel(ctx->core, 0), ctx->core->frequency(ctx->core), 32768);
	blip_set_rates(ctx->core->getAudioChannel(ctx->core, 1), ctx->core->frequency(ctx->core), 32768);

	ctx->force_disable_rtc = forceDisableRtc;

	mgba_reset(ctx);

	struct GBA* gba = ctx->core->board;
	if (gba->memory.hw.devices & HW_RTC)
	{
		// do rtc init again, as our 0xFF filled buffer would have trashed rtc state
		GBAHardwareInitRTC(&gba->memory.hw);
	}

	return ctx;
}

GSR_EXPORT void mgba_destroy(GSR_ctx* ctx)
{
	ctx->core->deinit(ctx->core); // this will close rom/bios/sram vfiles
	free(ctx->rom);
	free(ctx);
}

GSR_EXPORT void mgba_setcolorlut(GSR_ctx* ctx, const uint32_t* colorLut)
{
	memcpy(ctx->color_lut, colorLut, sizeof(ctx->color_lut));
}

GSR_EXPORT void mgba_advance(GSR_ctx* ctx, uint16_t buttons, uint32_t* videoBuf, int16_t* soundBuf, uint32_t* samples, uint32_t* cpuCycles)
{
	int32_t startCycle = mTimingCurrentTime(ctx->core->timing);

	ctx->core->setKeys(ctx->core, buttons);
	ctx->core->runFrame(ctx->core);

	for (unsigned i = 0; i < GBA_VIDEO_HORIZONTAL_PIXELS * GBA_VIDEO_VERTICAL_PIXELS; i++)
	{
		videoBuf[i] = ctx->color_lut[ctx->vbuf[i] & 0x7FFF];
	}

	*samples = blip_samples_avail(ctx->core->getAudioChannel(ctx->core, 0));
	if (*samples > 1024)
	{
		*samples = 1024;
	}

	blip_read_samples(ctx->core->getAudioChannel(ctx->core, 0), soundBuf + 0, 1024, true);
	blip_read_samples(ctx->core->getAudioChannel(ctx->core, 1), soundBuf + 1, 1024, true);

	*cpuCycles = mTimingCurrentTime(ctx->core->timing) - startCycle;
}

GSR_EXPORT void mgba_reset(GSR_ctx* ctx)
{
	ctx->core->reset(ctx->core);

	struct GBA* gba = ctx->core->board;
	gba->idleOptimization = IDLE_LOOP_IGNORE;
	if (ctx->force_disable_rtc)
	{
		gba->memory.hw.devices &= ~HW_RTC;
	}
}

GSR_EXPORT uint32_t mgba_getsavedatalength(GSR_ctx* ctx);

GSR_EXPORT void mgba_savesavedata(GSR_ctx* ctx, uint8_t* dest)
{
	ctx->sram_vf->seek(ctx->sram_vf, 0, SEEK_SET);
	ctx->sram_vf->read(ctx->sram_vf, dest, mgba_getsavedatalength(ctx));
}

GSR_EXPORT void mgba_loadsavedata(GSR_ctx* ctx, const uint8_t* data)
{
	ctx->sram_vf->seek(ctx->sram_vf, 0, SEEK_SET);
	ctx->sram_vf->write(ctx->sram_vf, data, mgba_getsavedatalength(ctx));
}

GSR_EXPORT uint32_t mgba_getsavedatalength(GSR_ctx* ctx)
{
	struct GBA* gba = ctx->core->board;
	uint32_t saveDataSize = GBASavedataSize(&gba->memory.savedata);
	if ((saveDataSize & 0xFF) == 0 && gba->memory.hw.devices & HW_RTC)
	{
		saveDataSize += sizeof(struct GBASavedataRTCBuffer);
	}

	return saveDataSize;
}

GSR_EXPORT uint32_t mgba_getsavestatelength(GSR_ctx* ctx)
{
	ctx->state_vf = VFileMemChunk(NULL, 0);
	if (!mCoreSaveStateNamed(ctx->core, ctx->state_vf, SAVESTATE_SAVEDATA))
	{
		ctx->state_vf->close(ctx->state_vf);
		ctx->state_vf = NULL;
		return 0;
	}

	return ctx->state_vf->size(ctx->state_vf);
}

GSR_EXPORT bool mgba_savestate(GSR_ctx* ctx, uint8_t* stateBuf)
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

GSR_EXPORT bool mgba_loadstate(GSR_ctx* ctx, const uint8_t* stateBuf, uint32_t size)
{
	struct VFile* vf = VFileFromConstMemory(stateBuf, size);
	bool ret = mCoreLoadStateNamed(ctx->core, vf, SAVESTATE_SAVEDATA);
	vf->close(vf);
	return ret;
}

enum MemoryBlock
{
	MEMORY_BLOCK_IWRAM = 0,
	MEMORY_BLOCK_EWRAM = 1,
	MEMORY_BLOCK_SRAM = 2,
};

GSR_EXPORT void mgba_getmemoryblock(GSR_ctx* ctx, enum MemoryBlock which, void** ptr, size_t* len)
{
	*ptr = NULL;
	*len = 0;

	switch (which)
	{
		case MEMORY_BLOCK_IWRAM:
			*ptr = ctx->core->getMemoryBlock(ctx->core, REGION_WORKING_IRAM, len);
			break;
		case MEMORY_BLOCK_EWRAM:
			*ptr = ctx->core->getMemoryBlock(ctx->core, REGION_WORKING_RAM, len);
			break;
		case MEMORY_BLOCK_SRAM:
			// we don't know how much sram will actually be used yet, so give the entire block
			*ptr = ctx->sram;
			*len = sizeof(ctx->sram);
			break;
	}
}
