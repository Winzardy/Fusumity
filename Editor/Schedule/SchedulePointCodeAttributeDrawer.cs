using System;
using System.Globalization;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Extensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class SchedulePointCodeAttributeDrawer : OdinAttributeDrawer<SchedulePointCodeAttribute, long>
	{
		private const string TYPE_LABEL = "Type";
		private const string FROM_LABEL = "from";

		private const float SUFFIX_TEXT_OFFSET = 0;
		private const float RESET_WIDTH = 52f;

		private static readonly GUIContent RESET_LABEL =
			new("Reset", "Код точки не распознан — сбросить её к ежедневной и задать заново");
		private GUIStyle _suffixTextStyle;
		private GUIStyle _infoStyle;

		protected override void Initialize()
		{
			_suffixTextStyle = new GUIStyle(EditorStyles.label)
			{
				fontSize = EditorStyles.textField.fontSize - 3,
				normal =
				{
					textColor = Color.gray
				},
				hover =
				{
					textColor = Color.gray
				}
			};

			// Стиль справки под полями раньше создавался на каждый repaint
			_infoStyle = new GUIStyle(SirenixGUIStyles.MiniLabelCentered)
			{
				alignment = TextAnchor.UpperLeft,
				richText = true,
			};

			_infoStyle.fontSize -= 2;
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Property.Parent.ValueEntry.WeakSmartValue is not ISchedulePoint point)
				return;

			var enumLabel = label == null || label.text.IsNullOrEmpty()
				? new GUIContent(TYPE_LABEL, tooltip: label?.tooltip)
				: label;

			SchedulePointDecode decode;

			try
			{
				decode = ValueEntry.SmartValue;
			}
			catch
			{
				// Код не декодируется — миграция, скрипт, правка ассета руками. Рисуем один выбор
				// типа: иначе такую точку не починить ни здесь, ни в календаре
				DrawBrokenCode(enumLabel);
				return;
			}

			EditorGUI.BeginChangeCheck();
			var newKind = FusumityEditorGUILayout.EnumPopup(enumLabel, decode.kind);

			if (EditorGUI.EndChangeCheck() && newKind != decode.kind)
			{
				decode = SchedulePointDecode.GetDefault(newKind);
				ValueEntry.SmartValue = SchedulePointDecode.Encode(in decode);

				// Точка сбрасывается к дефолту нового типа — окно старого не переживает смену:
				// у Interval окон не бывает, а на коротком периоде чужая длительность длиннее
				// периода, и валидация тут же ругается на «ошибку», которую сделал сам редактор
				var schemeProperty = Property.Parent?.Parent?.Parent;

				if (schemeProperty?.ValueEntry?.WeakSmartValue is ScheduleScheme scheme &&
					scheme.GetWindowDuration(Property.Parent.Index) > 0)
					SchedulePointWindowDrawer.WriteDuration(schemeProperty, Property.Parent.Index, 0);

				Property.MarkSerializationRootDirty();
				return;
			}

			EditorGUI.BeginChangeCheck();
			var kind = decode.kind;
			if (kind is SchedulePointKind.Interval)
			{
				// Потолок общий с окнами: Encode умножает секунды на TYPE_OFFSET, и у самой границы
				// long код заворачивается в отрицательный — точка молча становится чужим правилом
				decode.sec = Math.Clamp(SirenixEditorFields.LongField(decode.sec), 1, ScheduleEditorFormat.MAX_SECONDS);
				var timeLabel = decode.sec.ToLabel();
				var suffix = decode.sec == 1
					? TimeUtility.SECOND_LABEL
					: decode.sec <= TimeUtility.SECS_IN_ONE_MINUTE - 1
						? $"{TimeUtility.SECOND_LABEL}s"
						: $"{TimeUtility.SECOND_LABEL}s, {timeLabel}";
				FusumityEditorGUILayout.SuffixValue(label, decode.sec, suffix, textStyle: _suffixTextStyle, textOffset: SUFFIX_TEXT_OFFSET + 1.75f);
			}
			else
			{
				SirenixEditorGUI.BeginHorizontalPropertyLayout(GUIContent.none);
				{
					var culture = CultureInfo.CurrentUICulture;
					var dateTimeFormat = culture.DateTimeFormat;

					if (kind is not SchedulePointKind.Daily
						and SchedulePointKind.Weekly
						or SchedulePointKind.MonthlyOnWeekday
						or SchedulePointKind.YearlyOnWeekday)
					{
						if (kind is SchedulePointKind.MonthlyOnWeekday or SchedulePointKind.YearlyOnWeekday)
						{
							if (kind is SchedulePointKind.YearlyOnWeekday)
								DrawMonth(ref decode);

							int displayWeekOfMonth = decode.weekOfMonth;
							displayWeekOfMonth += 1;

							displayWeekOfMonth = SirenixEditorFields.IntField(displayWeekOfMonth, GUILayout.MinWidth(58));
							FusumityEditorGUILayout.SuffixValue(label, displayWeekOfMonth, Suffix(displayWeekOfMonth)
								+ TimeUtility.WEEK_LABEL, textStyle: _suffixTextStyle, textOffset: SUFFIX_TEXT_OFFSET);

							decode.weekOfMonth = (byte) Math.Clamp(displayWeekOfMonth - 1, 0, 4);
							FusumityEditorGUILayout.SuffixLabel(FROM_LABEL);
							decode.sign = SirenixEditorFields.Dropdown(
								GUIContent.none,
								decode.sign ? 0 : 1,
								new[]
								{
									"start",
									"end"
								},
								GUILayout.MaxWidth(48)
							) == 0;
						}

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
							}, GUILayout.MinWidth(68)
						);
					}

					decode.hr = (byte) Math.Clamp(SirenixEditorFields.IntField(decode.hr), 0, 23);
					FusumityEditorGUILayout.SuffixValue(label, decode.hr, Suffix(decode.hr) + TimeUtility.HOUR_LABEL,
						textStyle: _suffixTextStyle, textOffset: SUFFIX_TEXT_OFFSET);
					decode.min = (byte) Math.Clamp(SirenixEditorFields.IntField(decode.min), 0, 59);
					FusumityEditorGUILayout.SuffixValue(label, decode.min, Suffix(decode.min) + TimeUtility.MINUTE_LABEL,
						textStyle: _suffixTextStyle, textOffset: SUFFIX_TEXT_OFFSET);
					decode.sec = Math.Clamp(SirenixEditorFields.LongField(decode.sec), 0, 59);
					FusumityEditorGUILayout.SuffixValue(label, decode.sec, Suffix(decode.sec) + TimeUtility.SECOND_LABEL,
						textStyle: _suffixTextStyle, textOffset: SUFFIX_TEXT_OFFSET);

					if (kind is not SchedulePointKind.Daily
						and not SchedulePointKind.Weekly
						and not SchedulePointKind.MonthlyOnWeekday
						and not SchedulePointKind.YearlyOnWeekday)
					{
						var displayDay = decode.day + 1;
						displayDay = SirenixEditorFields.LongField(displayDay);
						FusumityEditorGUILayout.SuffixValue(label, displayDay, Suffix(displayDay) + TimeUtility.DAY_LABEL,
							textStyle: _suffixTextStyle, textOffset: SUFFIX_TEXT_OFFSET);

						var dayMax = 30L;
						switch (kind)
						{
							case SchedulePointKind.Date:
								dayMax = DateTime.DaysInMonth((int) decode.yr, decode.mh + 1) - 1;
								break;

							case SchedulePointKind.Yearly:
							case SchedulePointKind.Monthly:
								FusumityEditorGUILayout.SuffixLabel(FROM_LABEL, textColor: _suffixTextStyle.normal.textColor);
								dayMax = DateTime.DaysInMonth(DateTime.Now.Year, decode.mh + 1) - 1;
								if (decode.mh == 1) // Исключение для Февраля
									dayMax = Math.Clamp(dayMax, 0, 28);

								decode.sign = SirenixEditorFields.Dropdown(
									GUIContent.none,
									decode.sign ? 0 : 1,
									new[]
									{
										"start",
										"end"
									},
									GUILayout.MaxWidth(48)
								) == 0;
								break;
						}

						decode.day = Math.Clamp(displayDay - 1, 0, dayMax);
					}

					if (kind is SchedulePointKind.Yearly
						or SchedulePointKind.Date)
					{
						DrawMonth(ref decode);
					}

					if (kind is SchedulePointKind.Date)
					{
						// Год вне границ DateTime роняет и Encode, и превью — а попасть туда
						// достаточно, стерев поле
						decode.yr = Math.Clamp(SirenixEditorFields.LongField(decode.yr), 1, 9999);
						FusumityEditorGUILayout.SuffixValue(label, decode.yr, Suffix(decode.yr) + TimeUtility.YEAR_LABEL,
							textStyle: _suffixTextStyle, textOffset: SUFFIX_TEXT_OFFSET);
						decode.sign = decode.yr > DateTime.UnixEpoch.Year;
					}

					void DrawMonth(ref SchedulePointDecode decode)
					{
						decode.mh = (byte) SirenixEditorFields.Dropdown(
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
							}, GUILayout.MinWidth(68)
						);
					}
				}

				SirenixEditorGUI.EndHorizontalPropertyLayout();
			}

			if (EditorGUI.EndChangeCheck())
			{
				// Месяц и год рисуются после поля дня, поэтому день мог остаться от прежнего месяца:
				// 31 марта → февраль иначе роняет Encode на несуществующем 31 февраля
				if (kind is SchedulePointKind.Date)
					decode.day = Math.Clamp(decode.day, 0,
						DateTime.DaysInMonth((int) decode.yr, decode.mh + 1) - 1);

				ValueEntry.SmartValue = SchedulePointDecode.Encode(in decode);
				Property.MarkSerializationRootDirty();
			}

			var style = _infoStyle;
			var now = DateTime.UtcNow;
			DateTime date;

			try
			{
				date = point.ToDateTime(now);
			}
			catch
			{
				// Дата не собирается — показываем хотя бы код, чтобы точку было видно
				GUILayout.Label($" code: {ValueEntry.SmartValue}", style);
				return;
			}

			var remaining = date - now;

			var remainingLabel = ", remaining:".ColorText(style.normal.textColor.WithAlpha(0.4f));
			var remainingText = $"{remainingLabel} {remaining.ToLabel(true, false)}";

			var dateLabel = "Date:".ColorText(style.normal.textColor.WithAlpha(0.4f));
			var dateText = $"{dateLabel} {date.ToString("U", CultureInfo.InvariantCulture)}";

			var nowLabel = "Now:".ColorText(style.normal.textColor.WithAlpha(0.4f));
			var nowText = $"{nowLabel} {now.ToString("U", CultureInfo.InvariantCulture)}";
			var code = $"code: {ValueEntry.SmartValue}".ColorText(style.normal.textColor.WithAlpha(0.2f));
			GUILayout.Label($" {dateText}" + $"{remainingText}\n" +
				$" {nowText}", style);
			var width = style.CalcWidth(code);
			var height = style.CalcHeight(code, width);
			GUI.Label(GUILayoutUtility.GetLastRect()
				.AlignRight(width, 2)
				.AlignBottom(height), code, style);
		}

		/// <summary>
		/// Сброс поверх нераспознанного кода — единственный способ оживить такую точку
		/// </summary>
		/// <remarks>
		/// Кнопкой, а не попапом типа: нераспознанный код не приводится к валидному значению
		/// enum, и рисовать по нему выпадающий список нечем. После сброса точка становится
		/// обычной Daily, и дальше работает штатный дровер
		/// </remarks>
		private void DrawBrokenCode(GUIContent label)
		{
			var raw = ValueEntry.SmartValue;

			SirenixEditorGUI.BeginHorizontalPropertyLayout(label);
			{
				GUILayout.Label($"broken code: {raw}", _infoStyle);

				if (GUILayout.Button(RESET_LABEL, EditorStyles.miniButton, GUILayout.Width(RESET_WIDTH)))
				{
					var decode = SchedulePointDecode.GetDefault(SchedulePointKind.Daily);
					ValueEntry.SmartValue = SchedulePointDecode.Encode(in decode);
					Property.MarkSerializationRootDirty();
				}
			}
			SirenixEditorGUI.EndHorizontalPropertyLayout();
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
