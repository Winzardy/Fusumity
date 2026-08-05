using System;
using Sapientia.Pooling;
using Sapientia.Extensions;

namespace Fusumity.Editor
{
	/// <summary>
	/// Общий формат длительностей расписания — без нулевых частей
	/// </summary>
	public static class ScheduleEditorFormat
	{
		/// <summary>«5 day 2 hr» вместо «5 day 2 hr 0 min 0 sec» — нулевые части только шумят</summary>
		public static string CompactSpan(long seconds)
			=> CompactSpan(TimeSpan.FromSeconds(seconds));

		/// <inheritdoc cref="CompactSpan(long)"/>
		public static string CompactSpan(TimeSpan span)
		{
			using (StringBuilderPool.Get(out var sb))
			{
				if (span.Days > 0)
					sb.Append(span.Days).Append(' ').Append(TimeUtility.DAY_LABEL).Append(' ');

				if (span.Hours > 0)
					sb.Append(span.Hours).Append(' ').Append(TimeUtility.HOUR_LABEL).Append(' ');

				if (span.Minutes > 0)
					sb.Append(span.Minutes).Append(' ').Append(TimeUtility.MINUTE_LABEL).Append(' ');

				if (span.Seconds > 0)
					sb.Append(span.Seconds).Append(' ').Append(TimeUtility.SECOND_LABEL).Append(' ');

				if (sb.Length == 0)
					return $"0 {TimeUtility.SECOND_LABEL}";

				// Хвостовой пробел от последней части
				sb.Length--;

				return sb.ToString();
			}
		}
	}
}
