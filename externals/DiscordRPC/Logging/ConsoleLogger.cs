using System;

namespace DiscordRPC.Logging;

/// <summary>
/// Logs the outputs to the console using <see cref="Console.WriteLine()"/>
/// </summary>
public sealed class ConsoleLogger : ILogger
{
	/// <summary>
	/// The level of logging to apply to this logger.
	/// </summary>
	public LogLevel Level { get; set; }

	/// <summary>
	/// Should the output be coloured?
	/// </summary>
	public bool Coloured { get; set; }

	/// <summary>
	/// Creates a new instance of a Console Logger.
	/// </summary>
	public ConsoleLogger()
	{
		Level = LogLevel.Info;
		Coloured = false;
	}

	/// <summary>
	/// Informative log messages
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void Trace(string message, params object[] args)
	{
		if (Level > LogLevel.Trace) return;

		if (Coloured) Console.ForegroundColor = ConsoleColor.Gray;

		var prefixedMessage = "TRACE: " + message;

		if (args.Length > 0)
		{
			Console.WriteLine(prefixedMessage, args);
		}
		else
		{
			Console.WriteLine(prefixedMessage);
		}
	}

	/// <summary>
	/// Informative log messages
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void Info(string message, params object[] args)
	{
		if (Level > LogLevel.Info) return;

		if (Coloured) Console.ForegroundColor = ConsoleColor.White;

		var prefixedMessage = "INFO: " + message;

		if (args.Length > 0)
		{
			Console.WriteLine(prefixedMessage, args);
		}
		else
		{
			Console.WriteLine(prefixedMessage);
		}
	}

	/// <summary>
	/// Warning log messages
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void Warning(string message, params object[] args)
	{
		if (Level > LogLevel.Warning) return;

		if (Coloured) Console.ForegroundColor = ConsoleColor.Yellow;

		var prefixedMessage = "WARN: " + message;

		if (args.Length > 0)
		{
			Console.WriteLine(prefixedMessage, args);
		}
		else
		{
			Console.WriteLine(prefixedMessage);
		}
	}

	/// <summary>
	/// Error log messsages
	/// </summary>
	/// <param name="message"></param>
	/// <param name="args"></param>
	public void Error(string message, params object[] args)
	{
		if (Level > LogLevel.Error) return;

		if (Coloured) Console.ForegroundColor = ConsoleColor.Red;

		var prefixedMessage = "ERR : " + message;

		if (args.Length > 0)
		{
			Console.WriteLine(prefixedMessage, args);
		}
		else
		{
			Console.WriteLine(prefixedMessage);
		}
	}
}
