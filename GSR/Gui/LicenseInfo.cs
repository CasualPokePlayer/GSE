// Copyright (c) 2024 CasualPokePlayer
// SPDX-License-Identifier: MPL-2.0

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace GSR.Gui;

internal static class Licensing
{
	// mostly matching SPDX license ids
	private const string MPL20 = "MPL-2.0";
	private const string GPL20ONLY = "GPL-2.0-only";
	private const string LGPL21LATER = "LGPL-2.1-or-later";
	private const string ZLIB = "Zlib";
	private const string MIT = "MIT";
	// not an SPDX license id (Expat is just MIT under a different name)
	private const string EXPAT = "Expat";

	public record CopyrightInfo(string Product, string ProductUrl, string CopyrightHolder, string LicenseId);

	public static readonly ImmutableArray<CopyrightInfo> CopyrightInfos =
	[
		new("GSR", "https://github.com/CasualPokePlayer/GSR", "CasualPokePlayer", MPL20),
		new("Gambatte", "https://github.com/pokemon-speedrunning/gambatte-core", "sinamas", GPL20ONLY),
		new("mGBA", "https://github.com/mgba-emu/mgba", "Jeffrey Pfau", MPL20),
		new("SDL2-CS", "https://github.com/CasualPokePlayer/GSR/blob/2a0f18a/externals/SDL2-CS/SDL2.cs", "Ethan Lee & CasualPokePlayer", ZLIB),
		new("SDL2", "https://github.com/libsdl-org/SDL", "Sam Lantinga", ZLIB),
		new("libusb", "https://github.com/libusb/libusb", "libusb contributors", LGPL21LATER),
		new("ImGui.NET", "https://github.com/ImGuiNET/ImGui.NET", "Eric Mellino and ImGui.NET contributors", MIT),
		new("cimgui", "https://github.com/cimgui/cimgui", "Stephan Dilly", MIT),
		new("Dear ImGui", "https://github.com/ocornut/imgui", "Omar Cornut", MIT),
		new("SameBoy", "https://github.com/LIJI32/SameBoy", "Lior Halphon", EXPAT),
		new("blip_buf", "https://github.com/CasualPokePlayer/GSR/blob/2a0f18a/GSR.Audio/BlipBuffer.cs", "CasualPokePlayer & Shay Green & EkeEke", LGPL21LATER),
		new("SharpCompress", "https://github.com/adamhathcock/sharpcompress", "Adam Hathcock", MIT),
		new("CsWin32", "https://github.com/microsoft/CsWin32", "Microsoft Corporation", MIT),
		new("GitVersion", "https://github.com/GitTools/GitVersion", "NServiceBus Ltd, GitTools and contributors", MIT),
		new(".NET Runtime", "https://github.com/dotnet/runtime", ".NET Foundation and Contributors", MIT),
	];

	public static readonly FrozenDictionary<string, string> Licenses = new Dictionary<string, string>
	{
		[MPL20] = GetLicense(MPL20),
		[GPL20ONLY] = GetLicense(GPL20ONLY),
		[LGPL21LATER] = GetLicense(LGPL21LATER),
		[ZLIB] = GetLicense(ZLIB),
		[MIT] = GetLicense(MIT),
		[EXPAT] = GetLicense(MIT),
	}.ToFrozenDictionary();

	private static string GetLicense(string licenseId)
	{
		using var license = typeof(Licensing).Assembly
			.GetManifestResourceStream($"{typeof(Licensing).Assembly.GetName().Name}.res.{licenseId}")!;
		using var reader = new StreamReader(license);
		return reader.ReadToEnd();
	}
}