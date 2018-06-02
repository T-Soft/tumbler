using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Tumbler.Model;

namespace Tumbler.ConfigurationParsing
{
	public class WatchedProcessFactory
	{
		private readonly Action<string> _printArgumentError;
		private readonly Action<string> _reportProcessStatus;
		private readonly Action<string> _reportFileError;
		
		public WatchedProcessFactory(Action<string> printArgumentError, Action<string> reportProcessStatus, Action<string> reportFileError)
		{
			_printArgumentError = printArgumentError;
			_reportProcessStatus = reportProcessStatus;
			_reportFileError = reportFileError;
		}

		public IList<WatchedProcess> CreateWatchedProcesses(string[] args, out int watchInterval)
		{
			List<WatchedProcess> ret = new List<WatchedProcess>();
			watchInterval = -1;
			
			if (args.Length == 1)
			{
				return FileParser.Parse(args[0], _reportFileError, _reportProcessStatus, out watchInterval);
			}
			
			if (args.Length > 4 && (args.Length-1) % 3 == 0)
			{
				if (!int.TryParse(args[0], out watchInterval))
				{
					_printArgumentError(args[0]);
					return null;
				}

				int groupCount = (args.Length-1) / 3;
				for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
				{
					// Each group has the following format
					//1			2			0
					//proc.bat	(start)		(end)

					string processCommandLine = args[groupIndex * 3 + 1];
					string processStartTimeString = args[groupIndex * 3 + 2];
					string processEndTimeString = args[groupIndex * 3 + 3];
					if (!int.TryParse(processStartTimeString, out int processStartTime))
					{
						_printArgumentError(processStartTimeString);
						return null;
					}

					if (!int.TryParse(processEndTimeString, out int processEndTime))
					{
						_printArgumentError(processEndTimeString);
						return null;
					}

					ret.Add(new WatchedProcess(processCommandLine, processStartTime, processEndTime, _reportProcessStatus));
				}

				return ret;
			}
			
			_printArgumentError(null);
			return null;
		}
	}
}
