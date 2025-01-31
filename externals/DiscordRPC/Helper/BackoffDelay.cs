using System;

namespace DiscordRPC.Helper;

internal sealed class BackoffDelay(int min, int max)
{
	/// <summary>
	/// The maximum time the backoff can reach
	/// </summary>
	public int Maximum { get; } = max;

	/// <summary>
	/// The minimum time the backoff can start at
	/// </summary>
	public int Minimum { get; } = min;

	/// <summary>
	/// The current time of the backoff
	/// </summary>
	private int _current = min;

	/// <summary>
	/// The current number of failures
	/// </summary>
	private int _fails;

	/// <summary>
	/// Resets the backoff
	/// </summary>
	public void Reset()
	{
		_fails = 0;
		_current = Minimum;
	}

	public int NextDelay()
	{
		// Increment the failures
		_fails++;

		var diff = (Maximum - Minimum) / 100.0;
		_current = (int)Math.Floor(diff * _fails) + Minimum;

		return Math.Min(Math.Max(_current, Minimum), Maximum);
	}
}
