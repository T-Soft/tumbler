using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Tumbler.ConfigurationParsing;
using Tumbler.Helpers;

namespace Tumbler.Model
{
	public sealed class WatchedProcess
	{
		#region Private

		private Process _processObject;
		private readonly Action<string> _reportProcessStatus;
		private bool _isCommandLineValid;
		private DateTime? _lastRestartDateTime;
		
		#endregion

		#region Props

		public string ProcessName { private set; get; }
		public int ProcessId { private set; get; }
		
		public ProcessPriorityClass ProcessPriority { get; }
		public string CommandLine { get; }
		public int StartTimeSeconds { get; }	// in seconds
		public int EndTimeSeconds { get; }      // in seconds

		/// <summary>
		/// Gets or sets the process restart times in current timezone.
		/// </summary>
		/// <value>
		/// The restart times.
		/// </value>
		public IList<DateTime> RestartTimes { get; }

		public string ExePath { private set; get; }
		public string Arguments { private set; get; } = string.Empty;

		public bool IsStartedSuccessfully { private set; get; }
		public bool IsStoppedSuccessfully { private set; get; }

		public bool IsBeingWatched => EndTimeSeconds != -1;
		public bool IsAlive => ProcessHelper.IsProcessAlive(ProcessId);

		#endregion

		#region Ctor

		public WatchedProcess(
			string processName,
			string commandLine,
			int startTimeSeconds,
			int endTimeSeconds,
			Action<string> reportProcessStatus,
			IList<DateTime> restartTimes = null,
			ProcessPriorityClass priority = ProcessPriorityClass.High)
		{
			ProcessName = processName;
			CommandLine = commandLine.Trim();
			SplitCommandLine();
			StartTimeSeconds = startTimeSeconds;
			EndTimeSeconds = endTimeSeconds;
			_reportProcessStatus = reportProcessStatus;
			ProcessPriority = priority;
			RestartTimes = restartTimes ?? new List<DateTime>();
		}

		#endregion

		#region Start / stop / restart methods

		public void TryRestart()
		{
			DateTime now = DateTime.Now;

			if (!RestartTimes.Any()
				|| (/*now.Month == _lastRestartDateTime.Month &&*/ now.Day == _lastRestartDateTime?.Day))
			{
				return; // no restart times defined or there had already been restart today
			}

			var restartTime = RestartTimes.First(); // TODO: support multiple restart times
			if (now.IsTimePast(restartTime))
			{
				_reportProcessStatus($"-+ Restarting process '{ProcessName}' PID={ProcessId}.");
				TryStop(isForced: true);
				Start(isForced: true);
				_lastRestartDateTime = DateTime.Now;
			}
		}

		public void Start(bool isForced = false)
		{
			if (!_isCommandLineValid)
			{
				_reportProcessStatus($"Invalid command line for process start : {CommandLine}");
				return;
			}

			ProcessStartInfo startInfo = new ProcessStartInfo(ExePath, Arguments);
			try
			{
				var startedProcess = Process.Start(startInfo);
				if (startedProcess != null)
				{
					_processObject = startedProcess;
					if (_processObject.HasExited)
					{
						_reportProcessStatus($"Process {ExePath} has already exited");
						return;
					}

					startedProcess.PriorityClass = ProcessPriority;
					ProcessName = startedProcess.ProcessName;
					ProcessId = startedProcess.Id;
					IsStartedSuccessfully = true;
					_reportProcessStatus($"++ Started process '{ProcessName}' PID={ProcessId}");
				}
			}
			catch (InvalidOperationException)
			{
				_reportProcessStatus("No file was specified.");
			}
			catch (FileNotFoundException)
			{
				_reportProcessStatus($"File {ExePath} not found.");
			}
			catch (Exception ex)
			{
				_reportProcessStatus($"An error happened during process activation : {Environment.NewLine}{ex}");
			}

			if (!isForced)
			{
				Thread.Sleep(StartTimeSeconds * 1000);
			}
		}

		public void TryStop(bool isForced = false)
		{
			try
			{
				if (_processObject.CloseMainWindow())
				{
					_processObject.Close();
				}
				else
				{
					_processObject.Kill();
					_processObject.Close();
				}

				_reportProcessStatus($"-- Stopped process '{ProcessName}' PID={ProcessId}");
				IsStoppedSuccessfully = true;
			}
			catch (Exception)
			{
				// ignore
			}

			ProcessId = -1;
			ProcessName = string.Empty;

			if (!isForced)
			{
				Thread.Sleep(EndTimeSeconds * 1000);
			}
		}

		#endregion
		
		#region Service methods

		private void SplitCommandLine()
		{
			var firstQuoteIndex = CommandLine.IndexOf("'", StringComparison.Ordinal);
			var lastQuoteIndex = CommandLine.LastIndexOf("'", StringComparison.Ordinal);
			if (CommandLine.Contains("'")
				&& firstQuoteIndex != lastQuoteIndex)
			{
				ExePath = CommandLine.Substring(firstQuoteIndex, lastQuoteIndex - firstQuoteIndex + 1).Trim('\'');
				Arguments = CommandLine.Remove(firstQuoteIndex, lastQuoteIndex - firstQuoteIndex + 1).Trim();
				if (ExePath.Contains("'"))
				{
					_isCommandLineValid = false;
				}
				else
				{
					_isCommandLineValid = true;
				}
			}
			else
			{
				var splitArgs = CommandLine.Split(new []{" "}, StringSplitOptions.RemoveEmptyEntries);
				ExePath = splitArgs.First();
				if (splitArgs.Length > 1)
				{
					Arguments = string.Join(" ", splitArgs.Skip(1)).Trim();
				}

				_isCommandLineValid = true;
			}
		}
		
		#endregion
	}
}
