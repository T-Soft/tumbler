using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Tumbler.Helpers
{
	/// <summary>
	/// Helper class for working with dates and times
	/// </summary>
	public static class DateTimeHelper
	{
		/// <summary>
		/// Determines whether target datetime's time is past the specified base datetime's time. Does not take both date values into account.
		/// </summary>
		/// <param name="targetDateTime">The target date time.</param>
		/// <param name="baseDateTime">The base date time.</param>
		/// <returns>
		///   <c>true</c> if target datetime's time is past the specified base datetime's time; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsTimePast(this DateTime targetDateTime, DateTime baseDateTime)
		{
			int GetSecondsFromMidnight(DateTime target)
			{
				return target.Hour * 60 * 60 + target.Minute * 60 + target.Second;
			}
			
			if (GetSecondsFromMidnight(targetDateTime) > GetSecondsFromMidnight(baseDateTime))
			{
				return true;
			}

			return false;
		}
	}
}
