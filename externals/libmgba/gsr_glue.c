#include <mgba/core/blip_buf.h>
#include <mgba/core/core.h>
#include <mgba/core/log.h>
#include <mgba/core/serialize.h>
#include <mgba/internal/gba/gba.h>
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
}

static void set_default_logger()
{
	static bool default_logger_set = false;
	if (!default_logger_set)
	{
		static struct mLogger logger;
		logger.log = stub_logger;
		mLogSetDefaultLogger(&logger);
	}
}

typedef struct
{
	struct mCore* core;
	void* rom;
	struct VFile* romVf;
	uint8_t bios[0x4000];
	struct VFile* biosVf;
	uint8_t sram[0x20000 + 16];
	struct VFile* sramVf;
	color_t vbuf[GBA_VIDEO_HORIZONTAL_PIXELS * GBA_VIDEO_VERTICAL_PIXELS];
	uint32_t color_lut[0x10000];
	struct VFile* stateVf;
} GSR_ctx;

GSR_EXPORT void mgba_destroy(GSR_ctx* ctx);
GSR_EXPORT void mgba_reset(GSR_ctx* ctx);

GSR_EXPORT GSR_ctx* mgba_create(const void* romData, uint32_t romLength, const void* biosData, uint32_t biosLength)
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
	ctx->romVf = VFileFromMemory(ctx->rom, romLength);
	if (!ctx->romVf)
	{
		mgba_destroy(ctx);
		return NULL;
	}

	if (!ctx->core->loadROM(ctx->core, ctx->romVf))
	{
		ctx->romVf->close(ctx->romVf);
		mgba_destroy(ctx);
		return NULL;
	}

	memcpy(ctx->bios, biosData, sizeof(ctx->bios));
	ctx->biosVf = VFileFromMemory(ctx->bios, sizeof(ctx->bios));
	if (!ctx->biosVf)
	{
		mgba_destroy(ctx);
		return NULL;
	}

	if (!ctx->core->loadBIOS(ctx->core, ctx->biosVf, 0))
	{
		ctx->biosVf->close(ctx->biosVf);
		mgba_destroy(ctx);
		return NULL;
	}

	memset(ctx->sram, 0xFF, sizeof(ctx->sram));
	ctx->sramVf = VFileFromMemory(ctx->sram, sizeof(ctx->sram));
	if (!ctx->sramVf)
	{
		mgba_destroy(ctx);
		return NULL;
	}

	ctx->core->loadSave(ctx->core, ctx->sramVf);

	ctx->core->setVideoBuffer(ctx->core, ctx->vbuf, GBA_VIDEO_HORIZONTAL_PIXELS);
	ctx->core->setAudioBufferSize(ctx->core, 1024);

	blip_set_rates(ctx->core->getAudioChannel(ctx->core, 0), ctx->core->frequency(ctx->core), 32768);
	blip_set_rates(ctx->core->getAudioChannel(ctx->core, 1), ctx->core->frequency(ctx->core), 32768);

	// sameboy "modern balanced" agb color correction
	static const uint8_t sameboy_agb_color_curve[] = { 0, 3, 8, 14, 20, 26, 33, 40, 47, 54, 62, 70, 78, 86, 94, 103, 112, 120, 129, 138, 147, 157, 166, 176, 185, 195, 205, 215, 225, 235, 245, 255 };
	for (uint32_t i = 0; i < 0x8000; i++)
	{
		uint8_t r = i & 0x1F;
		uint8_t g = (i >> 5) & 0x1F;
		uint8_t b = (i >> 10) & 0x1F;

		r = sameboy_agb_color_curve[r];
		g = sameboy_agb_color_curve[g];
		b = sameboy_agb_color_curve[b];

		if (g != b)
		{
			const double gamma = 2.2;
			g = round(pow((pow(g / 255.0, gamma) * 5 + pow(b / 255.0, gamma)) / 6, 1 / gamma) * 255);
		}

		ctx->color_lut[i] = (0xFF << 24) | (b << 16) | (g << 8) | r;
		ctx->color_lut[0x8000 + i] = ctx->color_lut[i];
	}

	mgba_reset(ctx);
	return ctx;
}

GSR_EXPORT void mgba_destroy(GSR_ctx* ctx)
{
	ctx->core->deinit(ctx->core); // this will close rom/bios/sram vfiles
	free(ctx->rom);
	free(ctx);
}

GSR_EXPORT void mgba_advance(GSR_ctx* ctx, uint16_t buttons, uint32_t* videoBuf, int16_t* soundBuf, uint32_t* samples, uint32_t* cpuCycles)
{
	int32_t startCycle = mTimingCurrentTime(ctx->core->timing);

	ctx->core->setKeys(ctx->core, buttons);
	ctx->core->runFrame(ctx->core);

	for (unsigned i = 0; i < GBA_VIDEO_HORIZONTAL_PIXELS * GBA_VIDEO_VERTICAL_PIXELS; i++)
	{
		videoBuf[i] = ctx->color_lut[ctx->vbuf[i]];
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
	gba->memory.hw.devices &= ~HW_RTC; // TODO: selectable RTC support
}

GSR_EXPORT void mgba_savesavedata(GSR_ctx* ctx, uint8_t* dest)
{
	ctx->sramVf->seek(ctx->sramVf, 0, SEEK_SET);
	ctx->sramVf->read(ctx->sramVf, dest, ctx->sramVf->size(ctx->sramVf));
}

GSR_EXPORT void mgba_loadsavedata(GSR_ctx* ctx, const uint8_t* data)
{
	ctx->sramVf->seek(ctx->sramVf, 0, SEEK_SET);
	ctx->sramVf->write(ctx->sramVf, data, ctx->sramVf->size(ctx->sramVf));
}

GSR_EXPORT uint32_t mgba_getsavedatalength(GSR_ctx* ctx)
{
	return ctx->sramVf->size(ctx->sramVf);
}

GSR_EXPORT uint32_t mgba_getsavestatelength(GSR_ctx* ctx)
{
	ctx->stateVf = VFileMemChunk(NULL, 0);
	if (!mCoreSaveStateNamed(ctx->core, ctx->stateVf, SAVESTATE_SAVEDATA))
	{
		ctx->stateVf->close(ctx->stateVf);
		ctx->stateVf = NULL;
		return 0;
	}

	return ctx->stateVf->size(ctx->stateVf);
}

GSR_EXPORT bool mgba_savestate(GSR_ctx* ctx, uint8_t* stateBuf)
{
	if (!ctx->stateVf)
	{
		return false;
	}

	ctx->stateVf->seek(ctx->stateVf, 0, SEEK_SET);
	ctx->stateVf->read(ctx->stateVf, stateBuf, ctx->stateVf->size(ctx->stateVf));
	ctx->stateVf->close(ctx->stateVf);
	ctx->stateVf = NULL;
	return true;
}

GSR_EXPORT bool mgba_loadstate(GSR_ctx* ctx, const uint8_t* stateBuf, uint32_t size)
{
	struct VFile* vf = VFileFromConstMemory(stateBuf, size);
	bool ret = mCoreLoadStateNamed(ctx->core, vf, SAVESTATE_SAVEDATA);
	vf->close(vf);
	return ret;
}
