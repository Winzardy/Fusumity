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
		/// <summary>
		/// Потолок длительностей расписания — сто лет. Дальше переполняются TimeSpan и DateTime,
		/// и на такой длительности падают и дровер, и календарь, а смысла в подобном окне нет
		/// </summary>
		public const long MAX_SECONDS = 100L * 365 * TimeUtility.SECS_IN_ONE_DAY;

		/// <summary>
		/// Прибавление с потолком: у верхней границы DateTime обычное сложение бросает, а данные
		/// сюда приходят из ассета — календарь и дровер не должны падать на чужой длительности
		/// </summary>
		public static DateTime AddSeconds(DateTime utc, long seconds)
		{
			if (seconds <= 0)
				return utc;

			var limit = (DateTime.MaxValue - utc).TotalSeconds;

			return seconds >= limit ? DateTime.MaxValue : utc.AddSeconds(seconds);
		}

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
