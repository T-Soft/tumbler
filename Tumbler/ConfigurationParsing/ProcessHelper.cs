using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Tumbler.ConfigurationParsing
{
	public static class ProcessHelper
	{
		public static bool IsProcessAlive(int processId)
		{
			try
			{
				Process.GetProcessById(processId);
				return true;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}
	}
}
