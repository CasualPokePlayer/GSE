// Copyright (c) 2026 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

#if GSE_WINDOWS
using System;
#endif
using System.CommandLine;
using System.CommandLine.Parsing;

#if GSE_WINDOWS
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;
#endif

using GSE.Emu;

namespace GSE;

internal sealed record CliArgs(
	string RomPath,
	string GbBiosPath,
	string GbcBiosPath,
	string Sgb2BiosPath,
	string GbaBiosPath,
	GBPlatform? GbPlatform,
	bool? ApplyColorCorrection,
	bool? DisableGbaRtc,
	bool? HideSgbBorder,
	bool? HideStatusBar,
	bool? HideMenuBarOnUnpause,
	bool SoftwareRenderer,
	int? WindowScale,
	bool? DisableWin11RoundCorners
)
{
	private static readonly Argument<string> _romArgument = new(name: "rom")
	{
		Description = "Path to ROM to be loaded",
		Arity = ArgumentArity.ZeroOrOne
	};

	private static readonly Option<string> _gbBiosOption = new(name: "--gb-bios")
	{
		Description = "Path to GB BIOS to be loaded",
		Arity = ArgumentArity.ExactlyOne
	};

	private static readonly Option<string> _gbcBiosOption = new(name: "--gbc-bios")
	{
		Description = "Path to GBC BIOS to be loaded",
		Arity = ArgumentArity.ExactlyOne
	};

	private static readonly Option<string> _sgb2BiosOption = new(name: "--sgb2-bios")
	{
		Description = "Path to SGB2 BIOS to be loaded",
		Arity = ArgumentArity.ExactlyOne
	};

	private static readonly Option<string> _gbaBiosOption = new(name: "--gba-bios")
	{
		Description = "Path to GBA BIOS to be loaded",
		Arity = ArgumentArity.ExactlyOne
	};

	private static readonly Option<GBPlatform?> _gbPlatformOption = new(name: "--gb-platform")
	{
		Description = "The platform to use for GB/C games",
		Arity = ArgumentArity.ExactlyOne
	};

	private static readonly Option<bool?> _applyColorCorrectionOption = new(name: "--apply-color-correction")
	{
		Description = "If true, apply color correction",
		Arity = ArgumentArity.ExactlyOne
	};

	private static readonly Option<bool?> _disableGbaRtcOption = new(name: "--disable-gba-rtc")
	{
		Description = "If true, disable GBA RTC",
		Arity = ArgumentArity.ExactlyOne
	};

	private static readonly Option<bool?> _hideSgbBorderOption = new(name: "--hide-sgb-border")
	{
		Description = "If true, hide the SGB border",
		Arity = ArgumentArity.ExactlyOne
	};

	private static readonly Option<bool?> _hideStatusBarOption = new(name: "--hide-status-bar")
	{
		Description = "If true, hides the status bar in favor of an OSD overlay for messages",
		Arity = ArgumentArity.ExactlyOne
	};

	private static readonly Option<bool?> _hideMenuBarOnUnpauseOption = new(name: "--hide-menu-bar-on-unpause")
	{
		Description = "If true, hides the menu bar while the emulator is unpaused",
		Arity = ArgumentArity.ExactlyOne
	};

	private static readonly Option<bool> _softwareRendererOption = new(name: "--software-renderer")
	{
		Description = "Sets the render driver to the software renderer",
		Arity = ArgumentArity.Zero
	};

	private static readonly Option<int?> _windowScaleOption = new(name: "--window-scale")
	{
		Description = "The scale factor for the window",
		Arity = ArgumentArity.ExactlyOne
	};

	private static readonly Option<bool?> _disableWin11RoundCorners = new(name: "--disable-win11-round-corners")
	{
		Description = "If true, Windows 11 round corners are disabled for the window",
		Arity = ArgumentArity.ExactlyOne,
		Hidden = true
	};

	private static void ValidateGbPlatform(OptionResult optionResult)
	{
		if (optionResult.Tokens.Count > 0 &&
		    Enum.TryParse<GBPlatform>(optionResult.Tokens[^1].Value, ignoreCase: true, out var gbPlatform))
		{
			if (!Enum.IsDefined(gbPlatform))
			{
				optionResult.AddError("Undefined enum value for option '--gb-platform' with expected type 'GSE.Emu.GBPlatform'.");
			}
		}
	}

	private static void ValidateWindowScale(OptionResult optionResult)
	{
		if (optionResult.Tokens.Count > 0 &&
		    int.TryParse(optionResult.Tokens[^1].Value, out var windowScale))
		{
			if (windowScale is < 1 or > 15)
			{
				optionResult.AddError("Option '--window-scale' may only range from 1 to 15.");
			}
		}
	}

	static CliArgs()
	{
		_romArgument.AcceptLegalFilePathsOnly();
		_gbBiosOption.AcceptLegalFilePathsOnly();
		_gbcBiosOption.AcceptLegalFilePathsOnly();
		_sgb2BiosOption.AcceptLegalFilePathsOnly();
		_gbaBiosOption.AcceptLegalFilePathsOnly();
		_gbPlatformOption.Validators.Add(ValidateGbPlatform);
		_windowScaleOption.Validators.Add(ValidateWindowScale);
#if GSE_WINDOWS
		if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
		{
			_disableWin11RoundCorners.Hidden = false;
		}
#endif
	}

	public static (int? ReturnCode, CliArgs cliArgs) Parse(string[] args)
	{
		var root = new RootCommand(description: $"GSE (Game Boy Speedrun Emulator) v{GSEVersion.FullSemVer}")
		{
			_romArgument,
			_gbBiosOption,
			_gbcBiosOption,
			_sgb2BiosOption,
			_gbaBiosOption,
			_gbPlatformOption,
			_applyColorCorrectionOption,
			_disableGbaRtcOption,
			_hideSgbBorderOption,
			_hideStatusBarOption,
			_hideMenuBarOnUnpauseOption,
			_softwareRendererOption,
			_windowScaleOption,
			_disableWin11RoundCorners
		};

		// Remove version option (doesn't make sense here)
		for (var i = 0; i < root.Options.Count; i++)
		{
			if (root.Options[i] is VersionOption)
			{
				root.Options.RemoveAt(i--);
			}
		}

		var result = CommandLineParser.Parse(root, args);
#if GSE_WINDOWS
		var attachedConsole = true;
		if (result.Errors.Count > 0 || result.Action?.Terminating is true)
		{
			// On Windows, the command line won't necessarily be attached
			// This is due to GSE being a GUI app rather than a CLI app
			// So this hack must be used to attach the console
			var consoleWindow = PInvoke.GetConsoleWindow();
			if (consoleWindow.IsNull)
			{
				attachedConsole = PInvoke.AttachConsole(PInvoke.ATTACH_PARENT_PROCESS);
				// Make sure the output starts on a new line
				Console.WriteLine(string.Empty);
			}
		}
#endif
		var invokeResult = result.Invoke();
		if (invokeResult != 0 || result.Action?.Terminating is true)
		{
#if GSE_WINDOWS
			if (attachedConsole)
			{
				var consoleWindow = PInvoke.GetConsoleWindow();
				if (!consoleWindow.IsNull)
				{
					_ = PInvoke.FreeConsole();
					// An enter press is needed to actually "exit" the console attachment
					_ = PInvoke.PostMessage(consoleWindow, PInvoke.WM_CHAR, (nuint)VIRTUAL_KEY.VK_RETURN, 0);
				}
			}
#endif
			return (invokeResult, null);
		}

		var romPath = result.GetValue(_romArgument);
		var gbBiosPath = result.GetValue(_gbBiosOption);
		var gbcBiosPath = result.GetValue(_gbcBiosOption);
		var sgb2BiosPath = result.GetValue(_sgb2BiosOption);
		var gbaBiosPath = result.GetValue(_gbaBiosOption);
		var gbPlatform = result.GetValue(_gbPlatformOption);
		var applyColorCorrection = result.GetValue(_applyColorCorrectionOption);
		var disableGbaRtc = result.GetValue(_disableGbaRtcOption);
		var hideSgbBorder = result.GetValue(_hideSgbBorderOption);
		var hideStatusBar = result.GetValue(_hideStatusBarOption);
		var hideMenuBarOnUnpause = result.GetValue(_hideMenuBarOnUnpauseOption);
		var softwareRenderer = result.GetValue(_softwareRendererOption);
		var windowScale = result.GetValue(_windowScaleOption);
		var disableWin11RoundCorners = result.GetValue(_disableWin11RoundCorners);

		return (null, new(
			romPath,
			gbBiosPath,
			gbcBiosPath,
			sgb2BiosPath,
			gbaBiosPath,
			gbPlatform,
			applyColorCorrection,
			disableGbaRtc,
			hideSgbBorder,
			hideStatusBar,
			hideMenuBarOnUnpause,
			softwareRenderer,
			windowScale,
			disableWin11RoundCorners));
	}
}
