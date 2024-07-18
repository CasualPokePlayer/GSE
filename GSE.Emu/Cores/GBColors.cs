// Copyright (c) 2024 CasualPokePlayer & Lior Halphon
// SPDX-License-Identifier: MPL-2.0 or MIT

using System;

namespace GSE.Emu.Cores;

/// <summary>
/// Color correction LUT provider, using formulas from SameBoy
/// https://github.com/LIJI32/SameBoy/blob/4cf3b3c/Core/display.c#L249-L390
/// </summary>
internal static class GBColors
{
	private const int GB_COLOR_LUT_LEN = 0x8000;
	private static readonly uint[] _trueColorLut = new uint[GB_COLOR_LUT_LEN];
	private static readonly uint[] _cgbColorLut = new uint[GB_COLOR_LUT_LEN];
	private static readonly uint[] _agbColorLut = new uint[GB_COLOR_LUT_LEN];
	private static readonly uint[] _sgbColorLut = new uint[GB_COLOR_LUT_LEN];

	private static readonly byte[] _cgbColorCurve = [ 0, 6, 12, 20, 28, 36, 45, 56, 66, 76, 88, 100, 113, 125, 137, 149, 161, 172, 182, 192, 202, 210, 218, 225, 232, 238, 243, 247, 250, 252, 254, 255 ];
	private static readonly byte[] _agbColorCurve = [ 0, 3, 8, 14, 20, 26, 33, 40, 47, 54, 62, 70, 78, 86, 94, 103, 112, 120, 129, 138, 147, 157, 166, 176, 185, 195, 205, 215, 225, 235, 245, 255 ];
	private static readonly byte[] _sgbColorCurve = [ 0, 2, 5, 9, 15, 20, 27, 34, 42, 50, 58, 67, 76, 85, 94, 104, 114, 123, 133, 143, 153, 163, 173, 182, 192, 202, 211, 220, 229, 238, 247, 255 ];

	private const double _gamma = 2.2;

	private static uint ToTrueColor(byte r, byte g, byte b)
	{
		r = (byte)((r * 0xFF + 0xF) / 0x1F);
		g = (byte)((g * 0xFF + 0xF) / 0x1F);
		b = (byte)((b * 0xFF + 0xF) / 0x1F);
		return 0xFFu << 24 | (uint)r << 16 | (uint)g << 8 | b;
	}

	private static uint ToCgbColor(byte r, byte g, byte b)
	{
		r = _cgbColorCurve[r];
		g = _cgbColorCurve[g];
		b = _cgbColorCurve[b];

		if (g != b)
		{
			g = (byte)Math.Round(Math.Pow((Math.Pow(g / 255.0, _gamma) * 3 + Math.Pow(b / 255.0, _gamma)) / 4, 1 / _gamma) * 255, MidpointRounding.AwayFromZero);
		}

		return 0xFFu << 24 | (uint)r << 16 | (uint)g << 8 | b;
	}

	private static uint ToAgbColor(byte r, byte g, byte b)
	{
		r = _agbColorCurve[r];
		g = _agbColorCurve[g];
		b = _agbColorCurve[b];

		if (g != b)
		{
			g = (byte)Math.Round(Math.Pow((Math.Pow(g / 255.0, _gamma) * 5 + Math.Pow(b / 255.0, _gamma)) / 6, 1 / _gamma) * 255, MidpointRounding.AwayFromZero);
		}

		return 0xFFu << 24 | (uint)r << 16 | (uint)g << 8 | b;
	}

	public static uint ToSgbColor(byte r, byte g, byte b)
	{
		r = _sgbColorCurve[r];
		g = _sgbColorCurve[g];
		b = _sgbColorCurve[b];
		return 0xFFu << 24 | (uint)r << 16 | (uint)g << 8 | b;
	}

	static GBColors()
	{
		for (var i = 0; i < GB_COLOR_LUT_LEN; i++)
		{
			var r = (byte)(i & 0x1F);
			var g = (byte)((i >> 5) & 0x1F);
			var b = (byte)((i >> 10) & 0x1F);
			_trueColorLut[i] = ToTrueColor(r, g, b);
			_cgbColorLut[i] = ToCgbColor(r, g, b);
			_agbColorLut[i] = ToAgbColor(r, g, b);
			_sgbColorLut[i] = ToSgbColor(r, g, b);
		}
	}

	public static ReadOnlySpan<uint> TrueColorLut => _trueColorLut;

	public static ReadOnlySpan<uint> GetLut(GBPlatform gbPlatform) => gbPlatform switch
	{
		GBPlatform.GB => _trueColorLut,
		GBPlatform.GBC => _cgbColorLut,
		GBPlatform.GBA or GBPlatform.GBP => _agbColorLut,
		GBPlatform.SGB2 => _sgbColorLut,
		_ => throw new InvalidOperationException()
	};
}
