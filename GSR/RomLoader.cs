using System;
using System.Collections.Immutable;
using System.Security.Cryptography;

using GSR.Emu;
using GSR.Emu.Controllers;
using GSR.Emu.Cores;
using GSR.Gui;

using ImGuiNET;

using static SDL2.SDL;

namespace GSR;

// ReSharper disable SuggestBaseTypeForParameterInConstructor
internal sealed class RomLoader(Config config, EmuManager emuManager, PostProcessor postProcessor, OSDManager osdManager, GBController gbController, GBAController gbaController, ImGuiWindow mainWindow)
{
	private static readonly ImmutableArray<string> _romExtensions = [ ".gb", ".gbc", ".gba" ];
	private static readonly ImmutableArray<string> _biosExtensions = [ ".bin", ".rom" ];

	public static ImmutableArray<string> RomAndCompressionExtensions { get; } = [.._romExtensions, ..GSRFile.SupportedCompressionExtensions];

	public static ImmutableArray<string> BiosAndCompressionExtensions { get; } = [.._biosExtensions, ..GSRFile.SupportedCompressionExtensions];

	private static readonly ImmutableArray<string> _gbBiosHashes =
	[
		"CF053ECCB4CCAFFF9E67339D4E78E98DCE7D1ED59BE819D2A1BA2232C6FCE1C7", // DMG
		"A8CB5F4F1F16F2573ED2ECD8DAEDB9C5D1DD2C30A481F9B179B5D725D95EAFE2", // MGB
	];

	private static readonly ImmutableArray<string> _gbcBiosHashes =
	[
		"B4F2E416A35EEF52CBA161B159C7C8523A92594FACB924B3EDE0D722867C50C7", // CGB
	];

	private static readonly ImmutableArray<string> _sgbBiosHashes =
	[
		"0E4DDFF32FC9D1EEAAE812A157DD246459B00C9E14F2F61751F661F32361E360", // SGB1
		"FD243C4FB27008986316CE3DF29E9CFBCDC0CD52704970555A8BB76EDBEC3988", // SGB2
	];

	private static readonly ImmutableArray<string> _gbaBiosHashes =
	[
		"FD2547724B505F487E6DCB29EC2ECFF3AF35A841A77AB2E85FD87350ABD36570", // GBA
		"782EB3894237EC6AA411B78FFEE19078BACF10413856D33CDA10B44FD9C2856B", // DS
	];

	private GSRFile ObtainBiosFile(bool gbaRom)
	{
		var biosPath = gbaRom
			? config.GbaBiosPath
			: config.GbPlatform switch
			{
				GBPlatform.GB => config.GbBiosPath,
				GBPlatform.GBC or GBPlatform.GBA or GBPlatform.GBP => config.GbcBiosPath,
				GBPlatform.SGB2 => config.Sgb2BiosPath,
				_ => throw new InvalidOperationException()
			};

		try
		{
			return new(biosPath, _biosExtensions);
		}
		catch
		{
			return null;
		}
	}

	private bool VerifyBiosFile(bool isGbaRom, ReadOnlyMemory<byte> biosData)
	{
		var (acceptableHashes, expectedSize) = isGbaRom
			? (_gbaBiosHashes, 0x4000)
			: config.GbPlatform switch
			{
				GBPlatform.GB => (_gbBiosHashes, 0x100),
				GBPlatform.GBC or GBPlatform.GBA or GBPlatform.GBP => (_gbcBiosHashes, 0x900),
				GBPlatform.SGB2 => (_sgbBiosHashes, 0x100),
				_ => throw new InvalidOperationException()
			};

		if (biosData.Length != expectedSize)
		{
			return false;
		}

		var hash = Convert.ToHexString(SHA256.HashData(biosData.Span));
		return acceptableHashes.Contains(hash);
	}

	public void LoadRomFile(string path)
	{
		// we might end up opening up a message box, so pause the emulator while we load up a ROM
		using (new EmuPause(emuManager))
		{
			try
			{
				var romFile = new GSRFile(path, _romExtensions);
				var isGbaRom = romFile.UnderlyingExtension.Equals(".gba", StringComparison.OrdinalIgnoreCase);
				var biosFile = ObtainBiosFile(isGbaRom);
				if (biosFile == null)
				{
					_ = SDL_ShowSimpleMessageBox(
						flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
						title: "BIOS Load Failure",
						message: "The required BIOS path was not correctly configured. You must configure BIOS paths before loading a ROM.",
						window: mainWindow.SdlWindow
					);

					return;
				}

				if (!VerifyBiosFile(isGbaRom, biosFile.UnderlyingFile))
				{
					_ = SDL_ShowSimpleMessageBox(
						flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
						title: "BIOS Load Failure",
						message: "The configured BIOS is incorrect. You must use a legitimate BIOS file.",
						window: mainWindow.SdlWindow
					);

					return;
				}

				emuManager.LoadRom(new(
					CoreType: isGbaRom ? EmuCoreType.mGBA : EmuCoreType.Gambatte,
					EmuController: isGbaRom ? gbaController : gbController,
					RomData: romFile.UnderlyingFile,
					BiosData: biosFile.UnderlyingFile,
					RomDirectory: romFile.Directory,
					RomName: romFile.UnderlyingFileName,
					HardResetCallback: osdManager.OnHardReset,
					GbPlatform: isGbaRom ? GBPlatform.GBA : config.GbPlatform,
					ApplyColorCorrection: config.ApplyColorCorrection,
					DisableGbaRtc: config.DisableGbaRtc
				));
				
				osdManager.OnRomLoaded(romFile.UnderlyingFileName, romFile.UnderlyingFile.Span);

				config.RecentRoms.RemoveAll(r => r == path);
				config.RecentRoms.Insert(0, path);
				if (config.RecentRoms.Count > 10) // arbitrary size limit
				{
					config.RecentRoms.RemoveRange(10, config.RecentRoms.Count - 10);
				}

				// reset our emu video texture immediately, mainly so we don't try to render a null texture with a rom loaded
				var (emuWidth, emuHeight) = emuManager.GetVideoDimensions(false);
				postProcessor.ResetEmuTexture(emuWidth, emuHeight);

				// lame copy paste code
				if (!config.AllowManualResizing)
				{
					(emuWidth, emuHeight) = emuManager.GetVideoDimensions(config.HideSgbBorder);
					var windowScale = config.WindowScale;
					var extraBarsHeight = (int)ImGui.GetFrameHeight() * (config.HideStatusBar ? 1 : 2);
					mainWindow.SetWindowSize(emuWidth * windowScale, emuHeight * windowScale + extraBarsHeight);
				}
			}
			catch (Exception e)
			{
				emuManager.UnloadRom();
				osdManager.OnRomUnloaded();
				Console.WriteLine(e);
				_ = SDL_ShowSimpleMessageBox(
					flags: SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
					title: "ROM Load Failure",
					message: "Failed to load ROM file",
					window: mainWindow.SdlWindow
				);
			}
		}
	}
}
