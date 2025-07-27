using System;
using System.Globalization;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Extensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class SchedulePointCodeAttributeDrawer : OdinAttributeDrawer<SchedulePointCodeAttribute, long>
	{
		private const string TYPE_LABEL = "Type";

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Property.Parent.ValueEntry.WeakSmartValue is not ISchedulePoint point)
				return;

			SchedulePointDecode decode = ValueEntry.SmartValue;
			var enumLabel = label == null || label.text.IsNullOrEmpty()
				? new GUIContent(TYPE_LABEL, tooltip: label?.tooltip)
				: label;

			var newKind = FusumityEditorGUILayout.EnumPopup(enumLabel, decode.kind);
			if (newKind != decode.kind)
				decode = SchedulePointDecode.GetDefault(newKind);

			var kind = decode.kind;
			if (kind is SchedulePointKind.Interval)
			{
				decode.sec = Math.Clamp(SirenixEditorFields.LongField(decode.sec), 1, long.MaxValue - ISchedulePoint.TYPE_OFFSET);
				var timeLabel = decode.sec.ToLabelFromLong();
				var suffix = decode.sec == 1
					? TimeUtility.SECOND_LABEL
					: decode.sec <= TimeUtility.SECS_IN_ONE_MINUTE - 1
						? $"{TimeUtility.SECOND_LABEL}s"
						: $"{TimeUtility.SECOND_LABEL}s, {timeLabel}";
				FusumityEditorGUILayout.SuffixValue(label, decode.sec, suffix);
				ValueEntry.SmartValue = SchedulePointDecode.Encode(in decode);
			}
			else
			{
				SirenixEditorGUI.BeginHorizontalPropertyLayout(GUIContent.none);
				{
					decode.hr = Math.Clamp(SirenixEditorFields.IntField(decode.hr), 0, 23);
					FusumityEditorGUILayout.SuffixValue(label, decode.hr, Suffix(decode.hr) + TimeUtility.HOUR_LABEL);
					decode.min = Math.Clamp(SirenixEditorFields.IntField(decode.min), 0, 59);
					FusumityEditorGUILayout.SuffixValue(label, decode.min, Suffix(decode.min) + TimeUtility.MINUTE_LABEL);
					decode.sec = Math.Clamp(SirenixEditorFields.LongField(decode.sec), 0, 59);
					FusumityEditorGUILayout.SuffixValue(label, decode.sec, Suffix(decode.sec) + TimeUtility.SECOND_LABEL);

					var culture = CultureInfo.CurrentUICulture;
					var dateTimeFormat = culture.DateTimeFormat;

					if (kind is not SchedulePointKind.Daily)
					{
						if (kind == SchedulePointKind.Weekly)
						{
							decode.day = SirenixEditorFields.Dropdown(
								(int) decode.day,
								new[]
								{
									dateTimeFormat.GetDayName(DayOfWeek.Monday),
									dateTimeFormat.GetDayName(DayOfWeek.Tuesday),
									dateTimeFormat.GetDayName(DayOfWeek.Wednesday),
									dateTimeFormat.GetDayName(DayOfWeek.Thursday),
									dateTimeFormat.GetDayName(DayOfWeek.Friday),
									dateTimeFormat.GetDayName(DayOfWeek.Saturday),
									dateTimeFormat.GetDayName(DayOfWeek.Sunday)
								}
							);
						}
						else
						{
							var displayDay = decode.day + 1;
							displayDay = SirenixEditorFields.LongField(displayDay);
							FusumityEditorGUILayout.SuffixValue(label, displayDay, Suffix(displayDay) + TimeUtility.DAY_LABEL);

							var max = 30L;
							switch (kind)
							{
								case SchedulePointKind.Date:
									max = DateTime.DaysInMonth((int) decode.yr, decode.mh + 1) - 1;
									break;

								case SchedulePointKind.Yearly:
								case SchedulePointKind.Monthly:
									FusumityEditorGUILayout.SuffixLabel("from");
									max = DateTime.DaysInMonth(DateTime.Now.Year, decode.mh + 1) - 1;
									if (decode.mh == 1) // Исключение для Февраля
										max = Math.Clamp(max, 0, 28);

									decode.sign = SirenixEditorFields.Dropdown(
										GUIContent.none,
										decode.sign ? 0 : 1,
										new[]
										{
											"start",
											"end"
										},
										GUILayout.MaxWidth(55)
									) == 0;
									break;
							}

							decode.day = Math.Clamp(displayDay - 1, 0, max);
						}
					}

					if (kind is SchedulePointKind.Yearly or SchedulePointKind.Date)
					{
						decode.mh = SirenixEditorFields.Dropdown(
							decode.mh,
							new[]
							{
								dateTimeFormat.GetMonthName(1),
								dateTimeFormat.GetMonthName(2),
								dateTimeFormat.GetMonthName(3),
								dateTimeFormat.GetMonthName(4),
								dateTimeFormat.GetMonthName(5),
								dateTimeFormat.GetMonthName(6),
								dateTimeFormat.GetMonthName(7),
								dateTimeFormat.GetMonthName(8),
								dateTimeFormat.GetMonthName(9),
								dateTimeFormat.GetMonthName(10),
								dateTimeFormat.GetMonthName(11),
								dateTimeFormat.GetMonthName(12)
							}
						);
						;
					}

					if (kind is SchedulePointKind.Date)
					{
						decode.yr = SirenixEditorFields.LongField(decode.yr);
						FusumityEditorGUILayout.SuffixValue(label, decode.yr, Suffix(decode.yr) + TimeUtility.YEAR_LABEL);
					}

					ValueEntry.SmartValue = SchedulePointDecode.Encode(in decode);
				}
				SirenixEditorGUI.EndHorizontalPropertyLayout();
			}

			var style = new GUIStyle(SirenixGUIStyles.MiniLabelCentered)
			{
				alignment = TextAnchor.UpperLeft,
				richText = true,

			};
			GUILayout.Space(1);
			style.fontSize -= 2;
			var now = DateTime.UtcNow;
			var date = point.GetNextDateTime(now);

			var remaining = date - now;

			var remainingLabel = ", remaining:".ColorText(style.normal.textColor.WithAlpha(0.4f));
			var remainingText = $"{remainingLabel} {remaining.ToLabel(true, false)}";

			var dateLabel = "Date:".ColorText(style.normal.textColor.WithAlpha(0.4f));
			var dateText = $"{dateLabel} {date.ToString("U", CultureInfo.InvariantCulture)}";

			var nowLabel = "Now:".ColorText(style.normal.textColor.WithAlpha(0.4f));
			var nowText = $"{nowLabel} {now.ToString("U", CultureInfo.InvariantCulture)}";
			GUILayout.Label($" {dateText}" + $"{remainingText}\n" +
				$" {nowText}", style);
		}

		private static string Suffix(long number)
		{
			if (number <= 0)
				return string.Empty;

			var rem100 = number % 100;
			var rem10 = number % 10;

			var suffix = rem100 is 11 or 12 or 13
				? "th"
				: rem10 switch
				{
					1 => "st",
					2 => "nd",
					3 => "rd",
					_ => "th"
				};

			return $"{suffix} ";
		}
	}
}
