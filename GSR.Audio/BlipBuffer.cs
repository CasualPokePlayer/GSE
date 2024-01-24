using System;
using System.Runtime.InteropServices;

namespace GSR.Audio;

// C# implementation of blargg's blip_buf + gpgx's improvements (LGPLv2.1+)
// https://github.com/ekeeke/Genesis-Plus-GX/blob/41285e1/core/sound/blip_buf.c
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

	public uint ReadSamples(Span<short> output)
	{
		var count = Math.Min((uint)(output.Length / 2), SamplesAvail);
		if (count != 0)
		{
			var sumL = _leftIntegrator;
			var sumR = _rightIntegrator;

			for (var i = 0; i < count; i++)
			{
				var s = Math.Clamp(sumL >> DeltaBits, short.MinValue, short.MaxValue);
				output[i * 2 + 0] = (short)s;
				sumL += _leftSamples[i];
				sumL -= s << (DeltaBits - BassShift);

				s = Math.Clamp(sumR >> DeltaBits, short.MinValue, short.MaxValue);
				output[i * 2 + 1] = (short)s;
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

				int delta;
				if (deltaL == deltaR)
				{
					delta = (deltaL * interp) >> DeltaBits;
					deltaL -= delta;
					var outV = input[0] * deltaL + inputHW[0] * delta;
					outL[0] += outV;
					outR[0] += outV;
					outV = input[1] * deltaL + inputHW[1] * delta;
					outL[1] += outV;
					outR[1] += outV;
					outV = input[2] * deltaL + inputHW[2] * delta;
					outL[2] += outV;
					outR[2] += outV;
					outV = input[3] * deltaL + inputHW[3] * delta;
					outL[3] += outV;
					outR[3] += outV;
					outV = input[4] * deltaL + inputHW[4] * delta;
					outL[4] += outV;
					outR[4] += outV;
					outV = input[5] * deltaL + inputHW[5] * delta;
					outL[5] += outV;
					outR[5] += outV;
					outV = input[6] * deltaL + inputHW[6] * delta;
					outL[6] += outV;
					outR[6] += outV;
					outV = input[7] * deltaL + inputHW[7] * delta;
					outL[7] += outV;
					outR[7] += outV;
					outV = rev[7] * deltaL + revHW[7] * delta;
					outL[8] += outV;
					outR[8] += outV;
					outV = rev[6] * deltaL + revHW[6] * delta;
					outL[9] += outV;
					outR[9] += outV;
					outV = rev[5] * deltaL + revHW[5] * delta;
					outL[10] += outV;
					outR[10] += outV;
					outV = rev[4] * deltaL + revHW[4] * delta;
					outL[11] += outV;
					outR[11] += outV;
					outV = rev[3] * deltaL + revHW[3] * delta;
					outL[12] += outV;
					outR[12] += outV;
					outV = rev[2] * deltaL + revHW[2] * delta;
					outL[13] += outV;
					outR[13] += outV;
					outV = rev[1] * deltaL + revHW[1] * delta;
					outL[14] += outV;
					outR[14] += outV;
					outV = rev[0] * deltaL + revHW[0] * delta;
					outL[15] += outV;
					outR[15] += outV;
				}
				else
				{
					delta = (deltaL * interp) >> DeltaBits;
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
	}
}
