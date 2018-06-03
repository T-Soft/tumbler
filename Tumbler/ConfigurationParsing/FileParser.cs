using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tumbler.Model;

namespace Tumbler.ConfigurationParsing
{
	public static class FileParser
	{
		public static IList<WatchedProcess> Parse(string filePath, Action<string> reportFileError, Action<string> reportProcessStatus, out int watchInvterval)
		{
			watchInvterval = -1;
			if (!File.Exists(filePath))
			{
				reportFileError($"File '{filePath}' not found");
				return null;
			}



			throw new NotImplementedException();
		}
	}
}
