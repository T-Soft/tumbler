﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Tumbler.Helpers;
using Tumbler.ConfigurationParsing;

[assembly: AssemblyVersion("2.0.*")]
namespace Tumbler
{
	class Program
	{
		#region Strings (program version && help)

		private static string ProgramName => "TUMBLER utility. "
			+ $"V {Assembly.GetExecutingAssembly().GetName().Version} "
			+ $"[For .NET Core 2.0]{Environment.NewLine}";

		private static string UsageExample =>
			$"Usage example: tumbler.exe <watch interval (seconds) > 0> <process_path_1> <process 1 start time (seconds)> <process 1 end time (seconds)> ... <process_path_n> <process n start time (seconds)> <process n end time (seconds)>{Environment.NewLine}"
			+ $"if <process_end_time> == -1 -> process is excluded from watch list.{Environment.NewLine}";

		#endregion

		private static int _watchInterval = 60; // seconds
		private const string LOG_FILE_NAME = "tumbler.log";

		private static void Main(string[] args)
		{
			Console.WriteLine(ProgramName);

			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

			Console.WriteLine(UsageExample);

			if (Process.GetProcessesByName("tumbler").Length > 1)
			{
				Console.WriteLine("Another copy of TUMBLER utility is already running. Close it before opening a new one.");
				return;
			}
			
			WatchedProcessFactory processFactory = new WatchedProcessFactory(ArgError, WriteLog, Console.WriteLine);
			var watchedProcesses = processFactory.CreateWatchedProcesses(args, out _watchInterval);

			if (watchedProcesses == null)
			{
				return;
			}

			WriteLog(Environment.NewLine);
			WriteLog($"First start...");
			
			// start all processes
			watchedProcesses.ForEach(p=>p.Start());
			if (watchedProcesses.Any(p => !p.IsStartedSuccessfully))
			{
				return;
			}

			Console.WriteLine($"{Environment.NewLine}To keep processes and exit press 'CTRL+C';{Environment.NewLine}"
				+ $"To close all processes and exit press 'ESC'.{Environment.NewLine}");

			ConsoleKeyInfo c = new ConsoleKeyInfo();
			do
			{
				if (Console.KeyAvailable)
				{
					c = Console.ReadKey();
				}

				var deadProcesses = watchedProcesses.Where(p => p.IsBeingWatched && !p.IsAlive).ToList();

				if (deadProcesses.Any())
				{
					foreach (var deadProcess in deadProcesses)
					{
						WriteLog($"No process PID={deadProcess.ProcessId}. Stopping remaining list...");
					}

					watchedProcesses.ForEach(p => p.TryStop());

					Console.WriteLine(
						$"{Environment.NewLine}{DateTime.Now.ToDiagnosticString()} Remaining processes stopped. Restarting...");

					watchedProcesses.ForEach(p => p.Start());
					
					Console.WriteLine(
						$"{Environment.NewLine}{DateTime.Now.ToDiagnosticString()} Process list restart complete. {Environment.NewLine}");
					Console.WriteLine(
						$"To keep processes and exit press 'CTRL+C';{Environment.NewLine}"
						+ $"To close all processes and exit press 'ESC'.{Environment.NewLine}");
				}

				// try restart watched processes
				watchedProcesses.ForEach(p=>p.TryRestart());

				Thread.Sleep(_watchInterval * 1000);
			}
			while (c.Key != ConsoleKey.Escape);

			WriteLog($"Exit sequence. Stopping all processes...");
			// stop all processes
			watchedProcesses.ForEach(p=>p.TryStop());
			WriteLog($"Processes stopped. Exiting...");
		}
		
		#region Console && Log writing methods

		static void WriteLog(string data)
		{
			string dataToWrite = $"{DateTime.Now.ToDiagnosticString()} " + data + Environment.NewLine;
			if (data == Environment.NewLine)
			{
				dataToWrite = Environment.NewLine;
			}
			
			Console.WriteLine(dataToWrite.TrimEnd());
			File.AppendAllText(LOG_FILE_NAME, dataToWrite);
		}

		private static void ArgError(string argv)
		{
			if (string.IsNullOrEmpty(argv))
			{
				Console.WriteLine("Invalid number of arguments passed. ");
			}
			else
			{
				Console.WriteLine($"Wrong argument '{argv}' passed. ");
			}
		} 

		#endregion

	}
}
