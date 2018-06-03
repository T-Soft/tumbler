using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Tumbler.Helpers
{
	public static class ProcessHelper
	{
		public static bool IsProcessAlive(int processId)
		{
			try
			{
				var process = Process.GetProcessById(processId);
				return !process.HasExited;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}
	}
}
