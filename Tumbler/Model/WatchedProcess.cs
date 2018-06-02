﻿using System;
using System.Diagnostics;
using System.IO;
using Tumbler.ConfigurationParsing;

namespace Tumbler.Model
{
	public sealed class WatchedProcess
	{
		#region Private

		private Process _processObject;
		private readonly Action<string> _reportProcessStatus;
		
		#endregion

		#region Props

		public string ProcessName { private set; get; }
		public int ProcessId { private set; get; }
		
		public ProcessPriorityClass ProcessPriority { get; }
		public string CommandLine { get; }
		public int StartTime { get; }
		public int EndTime { get; }
		
		public string ExePath => GetExePath();
		public string Arguments => GetArguments();

		public bool IsStartedSuccessfully { private set; get; }
		public bool IsStoppedSuccessfully { private set; get; }

		public bool IsBeingWatched => EndTime != -1;
		public bool IsAlive =>
			_processObject != null
			&& (
				!_processObject.HasExited
				||
				ProcessHelper.IsProcessAlive(ProcessId)
			);

		#endregion

		#region Ctor

		public WatchedProcess(string commandLine, int startTime, int endTime, Action<string> reportProcessStatus, ProcessPriorityClass priority = ProcessPriorityClass.High)
		{
			CommandLine = commandLine;
			StartTime = startTime;
			EndTime = endTime;
			_reportProcessStatus = reportProcessStatus;
			ProcessPriority = priority;
		}

		#endregion

		#region Start / stop methods

		public void Start()
		{
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
		}

		#endregion
		
		#region Service methods

		private string GetExePath()
		{
			throw new NotImplementedException();
		}

		private string GetArguments()
		{
			throw new NotImplementedException();
		}
		
		#endregion
	}
}
