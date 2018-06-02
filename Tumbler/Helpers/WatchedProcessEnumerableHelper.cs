using System;
using System.Collections.Generic;
using System.Text;
using Tumbler.Model;

namespace Tumbler.ConfigurationParsing
{
	public static class WatchedProcessEnumerableHelper
	{
		public static void ForEach(this IEnumerable<WatchedProcess> targetCollection, Action<WatchedProcess> action)
		{
			foreach (var watchedProcess in targetCollection)
			{
				action(watchedProcess);
			}
		}
	}
}
