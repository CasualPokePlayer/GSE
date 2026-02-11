// Copyright (c) 2026 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#include <Core/GBA/GbaConsole.h>
#include <Core/GBA/GbaPpu.h>
#include <Core/GBA/Input/GbaController.h>
#include <Core/Shared/BaseControlManager.h>
#include <Core/Shared/BatteryManager.h>
#include <Core/Shared/Emulator.h>
#include <Core/Shared/SaveStateManager.h>
#include <Core/Shared/Audio/SoundMixer.h>
#include <Core/Shared/Interfaces/IAudioProvider.h>
#include <Core/Shared/Interfaces/IInputProvider.h>

#ifdef GSE_SHARED
	#ifdef _WIN32
		#define GSE_EXPORT extern "C" __declspec(dllexport)
	#else
		#define GSE_EXPORT extern "C" __attribute__((visibility("default")))
	#endif
#else
	#define GSE_EXPORT extern "C"
#endif

class GSEAudioProvider : public IAudioProvider
{
public:
	void MixAudio(int16_t* out, uint32_t sampleCount, uint32_t sampleRate) override
	{
		if (sample_index + sampleCount >= sizeof(samples) / sizeof(samples[0]))
		{
			sampleCount = sizeof(samples) / sizeof(samples[0]) - sample_index;
		}

		memcpy(&samples[sample_index], out, sampleCount * sizeof(samples[0]));
		sample_index += sampleCount;
	}

	struct StereoSample
	{
		int16_t left;
		int16_t right;
	};

	StereoSample samples[8192] = {};
	uint32_t sample_index = 0;
};

class GSEBatteryProvider : public IBatteryProvider
{
public:
	void SaveBattery(std::string extension, uint8_t* data, uint32_t length) override
	{
		if (extension == ".sav")
		{
			sav.resize(length);
			memcpy(sav.data(), data, length);
		}

		if (extension == ".rtc")
		{
			rtc.resize(length);
			memcpy(rtc.data(), data, length);
		}
	}

	std::vector<uint8_t> LoadBattery(std::string extension) override
	{
		if (extension == ".sav")
		{
			return sav;
		}

		if (extension == ".rtc")
		{
			return rtc;
		}

		return {};
	}

	std::vector<uint8_t> sav = {};
	std::vector<uint8_t> rtc = {};
};

class GSEInputProvider : public IInputProvider
{
public:
	bool SetInput(BaseControlDevice* device) override
	{
		if (device->GetControllerType() == ControllerType::GbaController)
		{
			for (uint16_t b = (uint16_t)GbaController::A; b <= (uint16_t)GbaController::L; b++)
			{
				device->SetBitValue(b, (keys & (1 << b)) != 0);
			}

			return true;
		}

		return false;
	}

	uint16_t keys = 0;
};

struct GSE_ctx
{
	std::unique_ptr<Emulator> emu = {};
	std::unique_ptr<GbaConsole> console = {};
	uint32_t color_lut[0x8000] = {};
	std::unique_ptr<GSEAudioProvider> audio_provider = {};
	std::shared_ptr<GSEBatteryProvider> battery_provider = {};
	std::unique_ptr<GSEInputProvider> input_provider = {};
	std::vector<uint8_t> state_buf = {};
};

GSE_EXPORT GSE_ctx* mesen_create(const uint8_t* romData, uint32_t romLength, const uint8_t* biosData, uint32_t biosLength, bool forceDisableRtc, int64_t rtcStartTime)
{
	if (biosLength != GbaConsole::BootRomSize)
	{
		return nullptr;
	}

	auto ctx = std::make_unique<GSE_ctx>();
	ctx->emu = std::make_unique<Emulator>();

	auto& audioConfig = ctx->emu->GetSettings()->GetAudioConfig();
	audioConfig.DisableDynamicSampleRate = true;
	audioConfig.SampleRate = 48000; // meh

	ctx->audio_provider = std::make_unique<GSEAudioProvider>();
	ctx->emu->GetSoundMixer()->RegisterAudioProvider(ctx->audio_provider.get());

	auto& gbaConfig = ctx->emu->GetSettings()->GetGbaConfig();
	gbaConfig.Controller.Type = ControllerType::GbaController;
	gbaConfig.SkipBootScreen = false;
	gbaConfig.DisableFrameSkipping = true;
	gbaConfig.RamPowerOnState = RamState::AllOnes;
	gbaConfig.RtcType = forceDisableRtc ? GbaRtcType::Disabled : GbaRtcType::AutoDetect;
	gbaConfig.GbaCustomDate = rtcStartTime;

	uint32_t paddedRomLength = 1;
	while (paddedRomLength < romLength)
	{
		paddedRomLength <<= 1;
	}

	auto paddedRom = std::make_unique<uint8_t[]>(paddedRomLength);
	memcpy(paddedRom.get(), romData, romLength);
	if (romLength != paddedRomLength)
	{
		memset(&paddedRom[romLength], 0xFF, paddedRomLength - romLength);
	}

	auto rom = VirtualFile(paddedRom.get(), paddedRomLength, "rom.gba");

	ctx->battery_provider = std::make_shared<GSEBatteryProvider>();
	ctx->emu->GetBatteryManager()->Initialize("rom.gba");
	ctx->emu->GetBatteryManager()->SetBatteryProvider(ctx->battery_provider);

	ctx->console = std::make_unique<GbaConsole>(ctx->emu.get());
	if (ctx->console->LoadRom(rom) != LoadRomResult::Success)
	{
		return nullptr;
	}

	// manually load up the BIOS
	auto* biosMemory = ctx->emu->GetMemory(MemoryType::GbaBootRom).Memory;
	memcpy(biosMemory, biosData, GbaConsole::BootRomSize);
	ctx->console->Reset(); // need to reset in order to get CPU pipeline in the right state

	ctx->input_provider = std::make_unique<GSEInputProvider>();
	ctx->console->GetControlManager()->RegisterInputProvider(ctx->input_provider.get());
	ctx->console->GetControlManager()->UpdateControlDevices();

	return ctx.release();
}

GSE_EXPORT void mesen_destroy(GSE_ctx* ctx)
{
	delete ctx;
}

GSE_EXPORT void mesen_setcolorlut(GSE_ctx* ctx, const uint32_t* colorLut)
{
	memcpy(ctx->color_lut, colorLut, sizeof(ctx->color_lut));
}

GSE_EXPORT void mesen_advance(GSE_ctx* ctx, uint16_t buttons, uint32_t* videoBuf, int16_t* soundBuf, uint32_t* samples, uint32_t* cpuCycles)
{
	uint64_t startCycle = ctx->console->GetMasterClock();

	// this just updates inputs
	ctx->input_provider->keys = buttons;
	ctx->console->ProcessEndOfFrame();

	ctx->console->RunFrame();

	auto* screenBuffer = ctx->console->GetPpu()->GetScreenBuffer();
	for (unsigned i = 0; i < GbaConstants::PixelCount; i++)
	{
		videoBuf[i] = ctx->color_lut[screenBuffer[i] & 0x7FFF];
	}

	*samples = ctx->audio_provider->sample_index;
	memcpy(soundBuf, ctx->audio_provider->samples, ctx->audio_provider->sample_index * sizeof(ctx->audio_provider->samples[0]));
	ctx->audio_provider->sample_index = 0;

	*cpuCycles = ctx->console->GetMasterClock() - startCycle;
}

GSE_EXPORT void mesen_reset(GSE_ctx* ctx)
{
	// save SaveRAM before reloading the ROM;
	ctx->console->Reset();
}

GSE_EXPORT uint32_t mesen_savesavedata(GSE_ctx* ctx, uint8_t* dest)
{
	ctx->console->SaveBattery();

	uint32_t savSize = ctx->battery_provider->sav.size();
	uint32_t rtcSize = ctx->battery_provider->rtc.size();

	if (savSize > 0)
	{
		memcpy(dest, ctx->battery_provider->sav.data(), savSize);
	}

	if (rtcSize > 0)
	{
		memcpy(dest + savSize, ctx->battery_provider->rtc.data(), rtcSize);
	}

	return savSize + rtcSize;
}

GSE_EXPORT void mesen_loadsavedata(GSE_ctx* ctx, const uint8_t* data, uint32_t size, int64_t rtcStartTime)
{
	uint32_t savSize = ctx->battery_provider->sav.size();
	uint32_t rtcSize = ctx->battery_provider->rtc.size();

	if (savSize > 0)
	{
		uint32_t savSizeInData = size & ~0xFF;
		memcpy(ctx->battery_provider->sav.data(), data, std::min(savSize, savSizeInData));		
	}

	if (rtcSize > 0)
	{
		uint32_t rtcSizeInData = size & 0xFF;
		if (rtcSize == rtcSizeInData)
		{
			memcpy(ctx->battery_provider->rtc.data(), data + size - rtcSizeInData, rtcSize);
		}
	}

	ctx->emu->GetSettings()->GetGbaConfig().GbaCustomDate = rtcStartTime;

	ctx->console->LoadBattery();
}

GSE_EXPORT int64_t mesen_getrtctime(GSE_ctx* ctx)
{
	return ctx->emu->GetSettings()->GetGbaConfig().GbaCustomDate;
}

GSE_EXPORT uint32_t mesen_getsavestatelength(GSE_ctx* ctx)
{
	Serializer s(SaveStateManager::FileFormatVersion, true);
	SV(*ctx->emu->GetSettings());
	SV(ctx->console);
	s.SaveTo(ctx->state_buf);
	return ctx->state_buf.size();
}

GSE_EXPORT bool mesen_savestate(GSE_ctx* ctx, uint8_t* stateBuf)
{
	if (ctx->state_buf.empty())
	{
		return false;
	}

	memcpy(stateBuf, ctx->state_buf.data(), ctx->state_buf.size());
	return true;
}

GSE_EXPORT bool mesen_loadstate(GSE_ctx* ctx, const uint8_t* stateBuf, uint32_t size)
{
	Serializer s(SaveStateManager::FileFormatVersion, false);
	if (!s.LoadFrom(stateBuf, size))
	{
		return false;
	}

	SV(*ctx->emu->GetSettings());
	SV(ctx->console);

	return !s.HasError();
}

enum MemoryBlock
{
	MEMORY_BLOCK_IWRAM = 0,
	MEMORY_BLOCK_EWRAM = 1,
	MEMORY_BLOCK_SRAM = 2,
};

GSE_EXPORT void mesen_getmemoryblock(GSE_ctx* ctx, enum MemoryBlock which, void** ptr, size_t* len)
{
	ConsoleMemoryInfo memoryInfo = {};
	switch (which)
	{
		case MEMORY_BLOCK_IWRAM:
			memoryInfo = ctx->emu->GetMemory(MemoryType::GbaIntWorkRam);
			break;
		case MEMORY_BLOCK_EWRAM:
			memoryInfo = ctx->emu->GetMemory(MemoryType::GbaExtWorkRam);
			break;
		case MEMORY_BLOCK_SRAM:
			memoryInfo = ctx->emu->GetMemory(MemoryType::GbaSaveRam);
			break;
	}

	*ptr = memoryInfo.Memory;
	*len = memoryInfo.Size;
}
