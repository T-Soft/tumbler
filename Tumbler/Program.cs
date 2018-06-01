using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Tumbler.Helpers;

[assembly: AssemblyVersion("1.0.*")]
namespace Tumbler
{
	class Program
	{
		private static int _watchInterval = 60;
		private static string _logFile = "upper.log";
		private static bool _isFirstStart = true;

		private static string ProgramName => "TUMBLER utility. "
			+ $"V {Assembly.GetExecutingAssembly().GetName().Version} "
			+ $"[For .NET Core 2.0]{Environment.NewLine}";
		
		private static readonly List<string> ProcessList = new List<string>();
		private static readonly Dictionary<string, int> StartTimes = new Dictionary<string, int>();
		private static readonly Dictionary<string, int> EndTimes = new Dictionary<string, int>();
		private static readonly Dictionary<int, string> ProcFiles = new Dictionary<int, string>();

		private static readonly List<WatchedProcess> WatchedProcesses = new List<WatchedProcess>();

		private static List<Process> StartAll()
		{
			if (ProcessList.Count > 0 && StartTimes.Count == ProcessList.Count)
			{
				List<Process> myPlist = new List<Process>();
				ProcFiles.Clear();
				foreach (string pname in ProcessList)
				{
					Process p = Process.Start(pname);
					p.PriorityClass = ProcessPriorityClass.High;

					WriteLog($"Started process '{pname}' PID={p.Id}");
					if (EndTimes[pname] != -1)
					{
						myPlist.Add(p);
					}
					ProcFiles.Add(p.Id, pname);
					Thread.Sleep(StartTimes[pname]);
				}
				if (_isFirstStart)
				{
					_isFirstStart = false;
				}
				return myPlist;
			}
			return null;
		}
		
		private static void StopAll(List<Process> pList)
		{
			pList.Reverse();
			foreach (Process p in pList)
			{
				int sleepTime = 0;
				try
				{
					Process pStop = Process.GetProcessById(p.Id);
					string pName = ProcFiles[pStop.Id];
					sleepTime = EndTimes[pName];

					if (sleepTime == -1)
					{
						continue;
					}

					if (pStop.CloseMainWindow())
					{
						pStop.Close();
					}
					else
					{
						pStop.Kill();
						pStop.Close();
					}

					Console.WriteLine($"\tStopped process '{pName}' PID={p.Id}");
				}
				catch(ArgumentException)
				{
					//Process with this pid was not found. Ignore.
				}
				Thread.Sleep(sleepTime);
			}
		}
		
		static void WriteLog(string data, bool writeToConsole = true)
		{
			if (writeToConsole)
			{
				Console.WriteLine(data);
			}

			File.AppendAllText(_logFile, data + Environment.NewLine);
		}
		
		private static void ArgError(string argv)
		{
			Console.WriteLine($"Wrong argument '{argv}' passed. "
				+ $"Usage example: tumbler.exe <watch interval (seconds) > 0> <process_path_1> <process 1 start time (seconds)> <process 1 end time (seconds)> ... <process_path_n> <process n start time (seconds)> <process n end time (seconds)>{Environment.NewLine}");
		}
		
		private static bool ParseArgs(string[] args)
		{
			if (args.Count() > 4)
			{
				//=1		2	0
				//proc.bat (start) (end)
				StartTimes.Clear();
				EndTimes.Clear();

				Regex re = new Regex(@"^\d+$");

				if (re.Match(args[0]).Success)
				{
					_watchInterval = int.Parse(args[0]) * 1000;
					if (_watchInterval <= 0)
					{
						ArgError(args[0]);
						return false;
					}
				}
				else
				{
					ArgError(args[0]);
					return false;
				}

				for (int i = 1; i < args.Count(); i++)
				{
					switch (i % 3)
					{
						//start times
						case 2:
							if (re.Match(args[i]).Success)
							{
								StartTimes.Add(args[i - 1], Int32.Parse(args[i]) * 1000);
							}
							else
							{
								ArgError(args[i]);
								return false;
							}
							break;
						//process names
						case 1:
							ProcessList.Add(args[i]);
							break;
						//end times
						case 0:
							if (args[i] == "-1")
							{
								EndTimes.Add(args[i - 2], -1);
							}
							else
							{
								if (re.Match(args[i]).Success)
								{
									EndTimes.Add(args[i - 2], Int32.Parse(args[i]) * 1000);
								}
								else
								{
									ArgError(args[i]);
									return false;
								}
							}

							break;
					}
				}
				return true;
			}
			else
			{
				Console.WriteLine("Too few arguments passed. Usage example: tumbler.exe <watch interval (seconds) > 0> <process_path_1> <process 1 start time (seconds)> <process 1 end time (seconds)> ... <process_path_n> <process n start time (seconds)> <process n end time (seconds)>\nUsage example: tumbler.exe <watch interval (seconds) > 0> <process_path_1> <process 1 start time (seconds)> <process 1 end time (seconds)> ... <process_path_n> <process n start time (seconds)> <process n end time (seconds)>\n");
				return false;
			}
		}
		
		private static void Main(string[] args)
		{
			Console.WriteLine(ProgramName);

			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

			Console.WriteLine(
				"Usage example: tumbler.exe <watch interval (seconds) > 0> <process_path_1> <process 1 start time (seconds)> <process 1 end time (seconds)> ... <process_path_n> <process n start time (seconds)> <process n end time (seconds)>\n" +
				"if <process_end_time> == -1 -> process is excluded from watch list.\n"
			);
			
			if (Process.GetProcessesByName("tumbler").Length > 1)
			{
				Console.WriteLine("Another copy of TUMBLER utility is already running. Close it before opening a new one.");
				return;
			}

			if (!ParseArgs(args))
			{
				return;
			}
			
			List<Process> myPlist = new List<Process>();

			WriteLog($"{Environment.NewLine}{DateTime.Now.ToDiagnosticString()} First start...");

			myPlist = StartAll();

			Console.WriteLine($"{Environment.NewLine}To keep processes and exit press 'CTRL+C';{Environment.NewLine}"
				+ $"To close all processes and exit press 'ESC'.{Environment.NewLine}");

			ConsoleKeyInfo c = new ConsoleKeyInfo();
			do
			{
				if (Console.KeyAvailable)
				{
					c = Console.ReadKey();
				}

				foreach (Process myP in myPlist)
				{
					try
					{
						Process.GetProcessById(myP.Id);
					}
					catch
					{
						WriteLog($"{DateTime.Now.ToDiagnosticString()} No process PID={myP.Id}. Stopping remaining list...");

						StopAll(myPlist);

						Console.WriteLine(
							$"{Environment.NewLine}{DateTime.Now.ToDiagnosticString()} Remaining processes stopped. Restarting...");

						myPlist.Clear();
						myPlist = StartAll();

						Console.WriteLine(
							$"{Environment.NewLine}{DateTime.Now.ToDiagnosticString()} Process list restart complete. {Environment.NewLine}");
						Console.WriteLine(
							$"To keep processes and exit press 'CTRL+C';{Environment.NewLine}"
							+ $"To close all processes and exit press 'ESC'.{Environment.NewLine}");

						break;
					}
				}

				Thread.Sleep(_watchInterval);
			}
			while (c.Key != ConsoleKey.Escape);

			WriteLog($"{DateTime.Now.ToDiagnosticString()} Exit sequence. Stopping all processes...");
			StopAll(myPlist);
			WriteLog($"{Environment.NewLine}{DateTime.Now.ToDiagnosticString()} Processes stopped. Exiting...");
		}
	}
}
