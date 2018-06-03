using System;
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
		
		#endregion

		#region Props

		public string ProcessName { private set; get; }
		public int ProcessId { private set; get; }
		
		public ProcessPriorityClass ProcessPriority { get; }
		public string CommandLine { get; }
		public int StartTimeSeconds { get; }	// in seconds
		public int EndTimeSeconds { get; }		// in seconds

		public string ExePath { private set; get; }
		public string Arguments { private set; get; } = string.Empty;

		public bool IsStartedSuccessfully { private set; get; }
		public bool IsStoppedSuccessfully { private set; get; }

		public bool IsBeingWatched => EndTimeSeconds != -1;
		public bool IsAlive =>
			_processObject != null
			&& (
				!_processObject.HasExited
				||
				ProcessHelper.IsProcessAlive(ProcessId)
			);

		#endregion

		#region Ctor

		public WatchedProcess(string commandLine, int startTimeSeconds, int endTimeSeconds, Action<string> reportProcessStatus, ProcessPriorityClass priority = ProcessPriorityClass.High)
		{
			CommandLine = commandLine.Trim();
			SplitCommandLine();
			StartTimeSeconds = startTimeSeconds;
			EndTimeSeconds = endTimeSeconds;
			_reportProcessStatus = reportProcessStatus;
			ProcessPriority = priority;
		}

		#endregion

		#region Start / stop methods

		public void Start()
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
					_reportProcessStatus($"Started process '{ProcessName}' PID={ProcessId}");
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

			Thread.Sleep(StartTimeSeconds * 1000);
		}

		public void TryStop()
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

				_reportProcessStatus($"\tStopped process '{ProcessName}' PID={ProcessId}");
				IsStoppedSuccessfully = true;
			}
			catch (Exception)
			{
				_reportProcessStatus($"\tFailed to stop process '{ProcessName}' PID={ProcessId}");
			}

			ProcessId = -1;
			ProcessName = string.Empty;
			Thread.Sleep(EndTimeSeconds * 1000);
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
				if (ExePath.Contains("'"))
				{
					_isCommandLineValid = false;
				}

				Arguments = CommandLine.Remove(firstQuoteIndex, lastQuoteIndex - firstQuoteIndex + 1).Trim();
			}
			else
			{
				var splitArgs = CommandLine.Split(new []{" "}, StringSplitOptions.RemoveEmptyEntries);
				ExePath = splitArgs.First();
				if (splitArgs.Length > 1)
				{
					Arguments = string.Join(" ", splitArgs.Skip(1)).Trim();
				}
			}
		}
		
		#endregion
	}
}
