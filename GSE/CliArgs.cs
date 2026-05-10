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
	bool? HideSgbBorder
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

	static CliArgs()
	{
		_romArgument.AcceptLegalFileNamesOnly();
		_gbBiosOption.AcceptLegalFileNamesOnly();
		_gbcBiosOption.AcceptLegalFileNamesOnly();
		_sgb2BiosOption.AcceptLegalFileNamesOnly();
		_gbaBiosOption.AcceptLegalFileNamesOnly();
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
			_hideSgbBorderOption
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

		return (null, new(
			romPath,
			gbBiosPath,
			gbcBiosPath,
			sgb2BiosPath,
			gbaBiosPath,
			gbPlatform,
			applyColorCorrection,
			disableGbaRtc,
			hideSgbBorder));
	}
}
