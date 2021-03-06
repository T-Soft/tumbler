﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
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

			if (Path.GetExtension(filePath).ToLower() != ".xml")
			{
				reportFileError($"Non XML files are not supported yet. File '{filePath}' is supposed to be not XML");
				return null;
			}

			XDocument config = XDocument.Load(filePath);

			if (config.Root == null)
			{
				reportFileError($"File '{filePath}' is invalid XML");
				return null;
			}

			watchInvterval = int.Parse(config.Root.Attribute("watch_time")?.Value);
			var watchedProcesses = config.Root.Descendants("Process").Select(proc =>
			{
				var name = proc.Attribute("name")?.Value;

				var priority = proc.Attribute("priorirty")?.Value;
				Enum.TryParse(priority, true, out ProcessPriorityClass processPriority);
				
				var command = proc.Attribute("command")?.Value;
				var startTime = int.Parse(proc.Attribute("start_time")?.Value);
				var endTime = int.Parse(proc.Attribute("end_time")?.Value);

				var restartTimes = new List<DateTime>();
				var restartTimeAttributeValue = proc.Attribute("restart_time")?.Value;
				if (!string.IsNullOrEmpty(restartTimeAttributeValue))
				{
					if(DateTime.TryParse(restartTimeAttributeValue, out DateTime restartTime))
					{
						restartTimes.Add(restartTime); // TODO: support multiple restart times
					}
					else
					{
						reportFileError($"Unable to parse restart_time attribute value {restartTimeAttributeValue}");
					}
				}

				return new WatchedProcess(
					name,
					command,
					startTime,
					endTime,
					reportProcessStatus,
					restartTimes,
					processPriority);
			});
			
			return watchedProcesses.ToList();
		}
	}
}
