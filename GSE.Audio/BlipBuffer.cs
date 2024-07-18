// Copyright (c) 2024 CasualPokePlayer & Shay Green & EkeEke
// SPDX-License-Identifier: LGPL-2.1-or-later

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace GSE.Audio;

/// <summary>
/// C# implementation of blargg's blip_buf + gpgx's improvements
/// https://github.com/ekeeke/Genesis-Plus-GX/blob/41285e1/core/sound/blip_buf.c
/// </summary>
internal sealed unsafe class BlipBuffer : IDisposable
{
	private const int BlipMaxRatio = 1 << 20;
	// private const int BlipMaxFrame = 4000;

	private const int PreShift = 32;

	private const int TimeBits = PreShift + 20;

	private const ulong TimeUnit = 1L << TimeBits;

	private const int BassShift = 9;
	private const int EndFrameExtra = 2;

	private const int HalfWidth = 8;
	private const int BufExtra = HalfWidth * 2 + EndFrameExtra;
	private const int PhaseBits = 5;
	private const int PhaseCount = 1 << PhaseBits;
	private const int DeltaBits = 15;
	private const int DeltaUnit = 1 << DeltaBits;
	private const int FracBits = TimeBits - PreShift;
	private const int PhaseShift = FracBits - PhaseBits;

	private ulong _factor;
	private ulong _offset;
	private readonly uint _size;
	private int _leftIntegrator, _rightIntegrator;
	private readonly void* _sampleBuffer;
	private readonly int* _leftSamples, _rightSamples;

	// size is in stereo samples (2 16-bit samples = 1 stereo sample)
	public BlipBuffer(uint size)
	{
		_sampleBuffer = NativeMemory.Alloc((size + BufExtra) * 2, sizeof(int));
		_leftSamples = (int*)_sampleBuffer;
		_rightSamples = _leftSamples + size + BufExtra;
		_factor = TimeUnit / BlipMaxRatio;
		_size = size;
		Clear();
	}

	public void Dispose()
	{
		NativeMemory.Free(_sampleBuffer);
	}

	public void SetRates(double clockRate, double sampleRate)
	{
		var factor = TimeUnit * sampleRate / clockRate;
		_factor = (ulong)Math.Ceiling(factor);
	}

	public void Clear()
	{
		_offset = _factor / 2;
		_leftIntegrator = _rightIntegrator = 0;
		NativeMemory.Clear(_sampleBuffer, (_size + BufExtra) * 2 * sizeof(int));
	}

#if false
	public int ClocksNeeded(int samples)
	{
		var needed = (ulong)samples * TimeUnit;
		if (needed < _offset)
		{
			return 0;
		}

		return (int)((needed - _offset + _factor - 1) / _factor);
	}
#endif

	public void EndFrame(uint t)
	{
		_offset += t * _factor;
	}

	public uint SamplesAvail => (uint)(_offset >> TimeBits);

	private void RemoveSamples(uint count)
	{
		var remain = SamplesAvail + BufExtra - count;
		_offset -= count * TimeUnit;

		NativeMemory.Copy(_leftSamples + count, _leftSamples, remain * sizeof(int));
		NativeMemory.Clear(_leftSamples + remain, count * sizeof(int));

		NativeMemory.Copy(_rightSamples + count, _rightSamples, remain * sizeof(int));
		NativeMemory.Clear(_rightSamples + remain, count * sizeof(int));
	}

	public uint ReadSamples(Span<short> output, int volume)
	{
		var dbVolume = volume < 100 ? _volumeDbScaled[volume] : 1.0;
		var count = Math.Min((uint)(output.Length / 2), SamplesAvail);
		if (count != 0)
		{
			var sumL = _leftIntegrator;
			var sumR = _rightIntegrator;

			for (var i = 0; i < count; i++)
			{
				var s = Math.Clamp(sumL >> DeltaBits, short.MinValue, short.MaxValue);
				output[i * 2 + 0] = (short)Math.Round(s * dbVolume, MidpointRounding.AwayFromZero);
				sumL += _leftSamples[i];
				sumL -= s << (DeltaBits - BassShift);

				s = Math.Clamp(sumR >> DeltaBits, short.MinValue, short.MaxValue);
				output[i * 2 + 1] = (short)Math.Round(s * dbVolume, MidpointRounding.AwayFromZero);
				sumR += _rightSamples[i];
				sumR -= s << (DeltaBits - BassShift);
			}

			_leftIntegrator = sumL;
			_rightIntegrator = sumR;
			RemoveSamples(count);
		}

		return count;
	}

	private static readonly short[,] BlStep =
	{
		{ 43, -115, 350, -488, 1136, -914,  5861, 21022 },
		{ 44, -118, 348, -473, 1076, -799,  5274, 21001 },
		{ 45, -121, 344, -454, 1011, -677,  4706, 20936 },
		{ 46, -122, 336, -431,  942, -549,  4156, 20829 },
		{ 47, -123, 327, -404,  868, -418,  3629, 20679 },
		{ 47, -122, 316, -375,  792, -285,  3124, 20488 },
		{ 47, -120, 303, -344,  714, -151,  2644, 20256 },
		{ 46, -117, 289, -310,  634,  -17,  2188, 19985 },
		{ 46, -114, 273, -275,  553,  117,  1758, 19675 },
		{ 44, -108, 255, -237,  471,  247,  1356, 19327 },
		{ 43, -103, 237, -199,  390,  373,   981, 18944 },
		{ 42, -98,  218, -160,  310,  495,   633, 18527 },
		{ 40, -91,  198, -121,  231,  611,   314, 18078 },
		{ 38, -84,  178,  -81,  153,  722,    22, 17599 },
		{ 36, -76,  157,  -43,   80,  824,  -241, 17092 },
		{ 34, -68,  135,   -3,    8,  919,  -476, 16558 },
		{ 32, -61,  115,   34,  -60, 1006,  -683, 16001 },
		{ 29, -52,   94,   70, -123, 1083,  -862, 15422 },
		{ 27, -44,   73,  106, -184, 1152, -1015, 14824 },
		{ 25, -36,   53,  139, -239, 1211, -1142, 14210 },
		{ 22, -27,   34,  170, -290, 1261, -1244, 13582 },
		{ 20, -20,   16,  199, -335, 1301, -1322, 12942 },
		{ 18, -12,   -3,  226, -375, 1331, -1376, 12293 },
		{ 15, -4,   -19,  250, -410, 1351, -1408, 11638 },
		{ 13, 3,    -35,  272, -439, 1361, -1419, 10979 },
		{ 11, 9,    -49,  292, -464, 1362, -1410, 10319 },
		{ 9,  16,   -63,  309, -483, 1354, -1383, 9660  },
		{ 7,  22,   -75,  322, -496, 1337, -1339, 9005  },
		{ 6,  26,   -85,  333, -504, 1312, -1280, 8355  },
		{ 4,  31,   -94,  341, -507, 1278, -1205, 7713  },
		{ 3,  35,  -102,  347, -506, 1238, -1119, 7082  },
		{ 1,  40,  -110,  350, -499, 1190, -1021, 6464  },
		{ 0,  43,  -115,  350, -488, 1136,  -914, 5861  }
	};

	private static readonly Vector512<int>[] _blStep512 = new Vector512<int>[PhaseCount];
	private static readonly Vector512<int>[] _blStep512HW = new Vector512<int>[PhaseCount];

	// humans don't hear perceive loudness linearly, but rather in a logarithmic scale
	// due to this, we want to adjust volume by a decibel (dB) scale
	private static readonly double[] _volumeDbScaled;

	static BlipBuffer()
	{
		_volumeDbScaled = new double[100];

		// 0 in logarithmic scale is meaningless
		// for our purposes we'll just force it as the "muted" value
		_volumeDbScaled[0] = 0;

		// this is rather -60 to 0
		// TODO: should this range be larger/smaller?
		const int DB_RANGE = 60;
		for (var i = 1; i < 100; i++)
		{
			var db = DB_RANGE - i / 100.0 * DB_RANGE;
			_volumeDbScaled[i] = Math.Pow(10, -db / 20);
		}

		for (var i = 0; i < PhaseCount; i++)
		{
			fixed (short*
			       input = &BlStep[i, 0],
			       inputHW = &BlStep[i + 1, 0],
			       rev = &BlStep[PhaseCount - i, 0],
			       revHW = &BlStep[PhaseCount - i - 1, 0])
			{
				var input256 = Vector256.Create(input[0], input[1], input[2], input[3], input[4], input[5], input[6], input[7]);
				var rev256 = Vector256.Create(rev[7], rev[6], rev[5], rev[4], rev[3], rev[2], rev[1], rev[0]);
				_blStep512[i] = Vector512.Create(input256, rev256);
				var inputHW256 = Vector256.Create(inputHW[0], inputHW[1], inputHW[2], inputHW[3], inputHW[4], inputHW[5], inputHW[6], inputHW[7]);
				var revHW256 = Vector256.Create(revHW[7], revHW[6], revHW[5], revHW[4], revHW[3], revHW[2], revHW[1], revHW[0]);
				_blStep512HW[i] = Vector512.Create(inputHW256, revHW256);
			}
		}
	}

	public void AddDelta(uint time, int deltaL, int deltaR)
	{
		if ((deltaL | deltaR) != 0)
		{
			var fixedSample = (uint)((time * _factor + _offset) >> PreShift);
			var phase = fixedSample >> PhaseShift & (PhaseCount - 1);
			var interp = (int)(fixedSample >> (PhaseShift - DeltaBits) & (DeltaUnit - 1));
			var pos = fixedSample >> FracBits;

			var step = _blStep512[phase];
			var stepHW = _blStep512HW[phase];

			var delta = (deltaL * interp) >> DeltaBits;
			var delta512 = Vector512.Create(delta);
			var deltaL512 = Vector512.Create(deltaL - delta);

			var outL = _leftSamples + pos;
			var outL512 = Vector512.Load(outL);
			outL512 += step * deltaL512 + stepHW * delta512;
			outL512.Store(outL);

			delta = (deltaR * interp) >> DeltaBits;
			delta512 = Vector512.Create(delta);
			var deltaR512 = Vector512.Create(deltaR - delta);

			var outR = _rightSamples + pos;
			var outR512 = Vector512.Load(outR);
			outR512 += step * deltaR512 + stepHW * delta512;
			outR512.Store(outR);
		}
	}

#if false
	// Reference non-vector implementation
	public void AddDelta(uint time, int deltaL, int deltaR)
	{
		if ((deltaL | deltaR) != 0)
		{
			var fixedSample = (uint)((time * _factor + _offset) >> PreShift);
			var phase = fixedSample >> PhaseShift & (PhaseCount - 1);
			fixed (short*
			       input = &BlStep[phase, 0],
			       inputHW = &BlStep[phase + 1, 0],
			       rev = &BlStep[PhaseCount - phase, 0],
			       revHW = &BlStep[PhaseCount - phase - 1, 0])
			{
				var interp = (int)(fixedSample >> (PhaseShift - DeltaBits) & (DeltaUnit - 1));
				var pos = fixedSample >> FracBits;

				var outL = _leftSamples + pos;
				var outR = _rightSamples + pos;

				var delta = (deltaL * interp) >> DeltaBits;
				deltaL -= delta;
				outL[0] += input[0] * deltaL + inputHW[0] * delta;
				outL[1] += input[1] * deltaL + inputHW[1] * delta;
				outL[2] += input[2] * deltaL + inputHW[2] * delta;
				outL[3] += input[3] * deltaL + inputHW[3] * delta;
				outL[4] += input[4] * deltaL + inputHW[4] * delta;
				outL[5] += input[5] * deltaL + inputHW[5] * delta;
				outL[6] += input[6] * deltaL + inputHW[6] * delta;
				outL[7] += input[7] * deltaL + inputHW[7] * delta;
				outL[8] += rev[7] * deltaL + revHW[7] * delta;
				outL[9] += rev[6] * deltaL + revHW[6] * delta;
				outL[10] += rev[5] * deltaL + revHW[5] * delta;
				outL[11] += rev[4] * deltaL + revHW[4] * delta;
				outL[12] += rev[3] * deltaL + revHW[3] * delta;
				outL[13] += rev[2] * deltaL + revHW[2] * delta;
				outL[14] += rev[1] * deltaL + revHW[1] * delta;
				outL[15] += rev[0] * deltaL + revHW[0] * delta;

				delta = (deltaR * interp) >> DeltaBits;
				deltaR -= delta;
				outR[0] += input[0] * deltaR + inputHW[0] * delta;
				outR[1] += input[1] * deltaR + inputHW[1] * delta;
				outR[2] += input[2] * deltaR + inputHW[2] * delta;
				outR[3] += input[3] * deltaR + inputHW[3] * delta;
				outR[4] += input[4] * deltaR + inputHW[4] * delta;
				outR[5] += input[5] * deltaR + inputHW[5] * delta;
				outR[6] += input[6] * deltaR + inputHW[6] * delta;
				outR[7] += input[7] * deltaR + inputHW[7] * delta;
				outR[8] += rev[7] * deltaR + revHW[7] * delta;
				outR[9] += rev[6] * deltaR + revHW[6] * delta;
				outR[10] += rev[5] * deltaR + revHW[5] * delta;
				outR[11] += rev[4] * deltaR + revHW[4] * delta;
				outR[12] += rev[3] * deltaR + revHW[3] * delta;
				outR[13] += rev[2] * deltaR + revHW[2] * delta;
				outR[14] += rev[1] * deltaR + revHW[1] * delta;
				outR[15] += rev[0] * deltaR + revHW[0] * delta;
			}
		}
	}
#endif
}
