using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Tumbler
{
	public sealed class WatchedProcess
	{
		#region Props

		public string ProcessName { private set; get; }
		public ProcessPriorityClass ProcessPriority { private set; get; } = ProcessPriorityClass.High;
		public string CommandLine { get; }
		public int StartTime { get; }
		public int EndTime { get; }

		#endregion

		#region Ctor

		public WatchedProcess(string commandLine, int startTime, int endTime)
		{
			CommandLine = commandLine;
			StartTime = startTime;
			EndTime = endTime;
		}

		#endregion

		#region Methods

		public bool Start()
		{
			throw new NotImplementedException();
		}

		public void SetProcessName(string name)
		{
			ProcessName = name;
		}

		public void SetProcessPriority(ProcessPriorityClass newPriority)
		{
			ProcessPriority = newPriority;
		}

		#endregion
	}
}
