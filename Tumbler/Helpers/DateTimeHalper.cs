using System;
using System.Collections.Generic;
using System.Text;

namespace Tumbler.Helpers
{
    public static class DateTimeHalper
    {
		public static string ToDiagnosticString(this DateTime targeTime)
		{
			return "[" + targeTime.ToString("u").TrimEnd('Z') + "] ";
		}
	}
}
