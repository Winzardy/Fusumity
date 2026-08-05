using System;
using System.Globalization;
using Sapientia;
using Sapientia.Extensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Fusumity.Utility;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	/// <summary>
	/// Длительность окна точки — рисуется в паре с точкой, хотя лежит не в ней, а в
	/// <see cref="ScheduleScheme.durations"/> по тому же индексу. Появляется только когда
	/// на поле схемы висит <see cref="ScheduleWindowAttribute"/>: большинству расписаний
	/// окна не нужны. Вводить можно длиной или через конец окна — «диапазон пн–пт»
	/// дизайнер мыслит концом, а не длиной; хранится всегда длина
	/// </summary>
	public sealed class SchedulePointWindowDrawer : OdinValueDrawer<SchedulePoint>
	{
		private const string MOMENT_LABEL = "moment, no window";
		private const float NUMBER_WIDTH = 58f;

		private static readonly GUIContent DURATION_LABEL =
			new("Duration", "Длительность окна. Хранится в durations схемы, параллельно точкам");

		private static readonly GUIContent UNTIL_LABEL =
			new("Until", "Конец окна: до какого дня и времени оно идёт. Хранится всё равно длительность");

		private bool _enabled;

		/// <summary>
		/// Тугл окна: выключен — поля не рисуются, длительность обнулена. Инициализируется
		/// от данных при первом показе, дальше живёт как состояние редактора
		/// </summary>
		private bool? _active;

		private GUIStyle _suffixTextStyle;
		private GUIStyle _previewStyle;

		protected override void Initialize()
		{
			_enabled = HasWindowSupport(Property);

			if (!_enabled)
				return;

			_suffixTextStyle = new GUIStyle(EditorStyles.label)
			{
				fontSize = EditorStyles.textField.fontSize - 3,
				normal = {textColor = Color.gray},
				hover = {textColor = Color.gray}
			};

			// Серым, как suffix'ы полей: обычным цветом превью сливается со значениями
			_previewStyle = new GUIStyle(SirenixGUIStyles.MiniLabelCentered)
			{
				alignment = TextAnchor.UpperLeft,
				richText = true,
				normal = {textColor = Color.gray},
				hover = {textColor = Color.gray}
			};

			_previewStyle.fontSize -= 2;
		}

		/// <summary>Ищет [ScheduleWindow] вверх по дереву — атрибут висит на поле со схемой</summary>
		private static bool HasWindowSupport(InspectorProperty property)
		{
			for (var current = property; current != null; current = current.Parent)
			{
				if (current.GetAttribute<ScheduleWindowAttribute>() != null)
					return true;
			}

			return false;
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			CallNextDrawer(label);

			if (!_enabled)
				return;

			// Пара «точка + длительность» существует только внутри схемы
			var schemeProperty = Property.Parent?.Parent;

			if (schemeProperty?.ValueEntry?.WeakSmartValue is not ScheduleScheme scheme)
				return;

			SchedulePointDecode decode;

			try
			{
				decode = ValueEntry.SmartValue.code;
			}
			catch
			{
				return;
			}

			var kind = decode.kind;

			// У Interval окна нет. Остаток длительности от прежнего типа обнуляем сами:
			// дизайнер про скрытое окно не знает, и ошибка про него только путает
			if (kind == SchedulePointKind.Interval)
			{
				if (scheme.GetWindowDuration(Property.Index) > 0)
					SetDuration(schemeProperty, Property.Index, 0);

				return;
			}

			var index = Property.Index;
			var duration = scheme.GetWindowDuration(index);
			var active = _active ??= duration > 0;

			// Режим ввода зависит от типа точки: там, где конец окна однозначен, дизайнер
			// мыслит концом («до пятницы», «до 23:00») — там и рисуем конец. У остальных
			// правил период плавает, ввод только длительностью
			SirenixEditorGUI.BeginHorizontalPropertyLayout(
				CanPickEnd(kind) ? UNTIL_LABEL : DURATION_LABEL);
			{
				var toggled = EditorGUILayout.Toggle(active, GUILayout.Width(16));

				// Выключение — это и есть «окна нет»: поля прячем, длительность в ноль
				if (toggled != active)
				{
					_active = active = toggled;

					if (!toggled)
						SetDuration(schemeProperty, index, 0);
				}

				if (active)
				{
					EditorGUI.BeginChangeCheck();

					duration = CanPickEnd(kind)
						? DrawUntil(in decode, kind, duration)
						: DrawLength(kind, duration);

					if (EditorGUI.EndChangeCheck())
						SetDuration(schemeProperty, index, Math.Max(0, duration));
				}
			}
			SirenixEditorGUI.EndHorizontalPropertyLayout();

			if (active)
				DrawPreview(ValueEntry.SmartValue, duration);
		}

		private void SetDuration(InspectorProperty schemeProperty, int index, long value)
			=> WriteDuration(schemeProperty, index, value);

		/// <summary>
		/// Пишет в durations схемы, растянув его до нужного индекса. Все нули — массив
		/// зануляется: у расписаний без окон данные остаются пустыми
		/// </summary>
		internal static void WriteDuration(InspectorProperty schemeProperty, int index, long value)
		{
			var scheme = (ScheduleScheme) schemeProperty.ValueEntry.WeakSmartValue;
			var length = Math.Max(scheme.points?.Length ?? 0, index + 1);

			var updated = new long[length];

			if (scheme.durations != null)
				Array.Copy(scheme.durations, updated, Math.Min(scheme.durations.Length, length));

			updated[index] = value;

			var any = false;

			for (var i = 0; i < updated.Length; i++)
			{
				if (updated[i] <= 0)
					continue;

				any = true;
				break;
			}

			var durationsProperty = schemeProperty.Children[nameof(ScheduleScheme.durations)];

			// ApplyChanges сразу: без него запись висит в отложенных, и следующий кадр
			// перерисовывает поля из старой длительности — ввод терялся через раз
			if (durationsProperty?.ValueEntry != null)
			{
				durationsProperty.ValueEntry.WeakSmartValue = any ? updated : null;
				durationsProperty.ValueEntry.ApplyChanges();
			}
			else
			{
				// Скрытое поле могло не попасть в дерево — тогда пишем схему целиком
				scheme.durations = any ? updated : null;
				schemeProperty.ValueEntry.WeakSmartValue = scheme;
				schemeProperty.ValueEntry.ApplyChanges();
			}

			schemeProperty.MarkSerializationRootDirty();
			GUIHelper.RequestRepaint();
		}

		/// <summary>
		/// Конец однозначен только там, где период очевиден: у месячных и годовых правил
		/// «до 30-го числа» упирается в разную длину месяцев — там ввод длительностью
		/// </summary>
		private static bool CanPickEnd(SchedulePointKind kind)
			=> kind is SchedulePointKind.Daily or SchedulePointKind.Weekly or SchedulePointKind.Date;

		#region Length

		private long DrawLength(SchedulePointKind kind, long duration)
		{
			var span = TimeSpan.FromSeconds(Math.Max(0, duration));
			var maxDays = MaxDays(kind);

			// У Daily окно не длиннее суток — поле дней только мешает. Но если дни в данных
			// уже есть (сменили тип точки), поле остаётся, чтобы значение не скрылось молча
			var days = maxDays > 0 || span.Days > 0
				? Math.Clamp(Field(span.Days, TimeUtility.DAY_LABEL), 0, Math.Max(maxDays, span.Days))
				: 0;

			var hours = Math.Clamp(Field(span.Hours, TimeUtility.HOUR_LABEL), 0, 23);
			var minutes = Math.Clamp(Field(span.Minutes, TimeUtility.MINUTE_LABEL), 0, 59);
			var seconds = Math.Clamp(Field(span.Seconds, TimeUtility.SECOND_LABEL), 0, 59);

			return days * (long) TimeUtility.SECS_IN_ONE_DAY
				+ hours * TimeUtility.SECS_IN_ONE_HOUR
				+ minutes * TimeUtility.SECS_IN_ONE_MINUTE
				+ seconds;
		}

		/// <summary>
		/// Потолок дней — период повторения правила: длиннее окно всё равно бессмысленно,
		/// о наложении предупреждает InfoBox точки
		/// </summary>
		private static int MaxDays(SchedulePointKind kind)
			=> kind switch
			{
				SchedulePointKind.Daily => 0,
				SchedulePointKind.Weekly => 6,
				SchedulePointKind.Monthly or SchedulePointKind.MonthlyOnWeekday => 27,
				SchedulePointKind.Yearly or SchedulePointKind.YearlyOnWeekday => 364,
				_ => int.MaxValue
			};

		private int Field(int value, string suffix)
		{
			var result = SirenixEditorFields.IntField(value, GUILayout.MinWidth(NUMBER_WIDTH));
			FusumityEditorGUILayout.SuffixValue(null, result, suffix, textStyle: _suffixTextStyle);

			return result;
		}

		#endregion

		#region Until

		/// <summary>
		/// Конец окна на «естественном» периоде правила. Считаем в секундах от начала периода,
		/// чтобы переход через полночь и через конец недели был обычным переполнением
		/// </summary>
		private long DrawUntil(in SchedulePointDecode decode, SchedulePointKind kind, long duration)
		{
			var startTime = decode.hr * TimeUtility.SECS_IN_ONE_HOUR
				+ decode.min * TimeUtility.SECS_IN_ONE_MINUTE
				+ decode.sec;

			if (kind == SchedulePointKind.Date)
				return DrawUntilDate(in decode, duration);

			var period = kind == SchedulePointKind.Weekly
				? TimeUtility.SECS_IN_ONE_DAY * 7L
				: TimeUtility.SECS_IN_ONE_DAY;

			var start = kind == SchedulePointKind.Weekly
				? decode.day * TimeUtility.SECS_IN_ONE_DAY + startTime
				: startTime;

			var end = start + duration;

			if (kind == SchedulePointKind.Weekly)
			{
				var endDay = (int) (end / TimeUtility.SECS_IN_ONE_DAY % 7);
				endDay = SirenixEditorFields.Dropdown(endDay, WeekDayNames(), GUILayout.MinWidth(78));
				end = endDay * TimeUtility.SECS_IN_ONE_DAY + DrawTime(end % TimeUtility.SECS_IN_ONE_DAY);
			}
			else
			{
				end = DrawTime(end % TimeUtility.SECS_IN_ONE_DAY);
			}

			var result = end - start;

			// Конец ровно в начале — окна нет: единственный способ обнулить окно, раз ввод
			// теперь только концом. Конец раньше начала — окно уходит в следующий период
			if (result < 0)
				result += period;

			return result;
		}

		/// <summary>У Date период не повторяется — конец задаётся полной датой</summary>
		private long DrawUntilDate(in SchedulePointDecode decode, long duration)
		{
			DateTime start;

			try
			{
				start = new DateTime((int) decode.yr, decode.mh + 1, (int) decode.day + 1,
					decode.hr, decode.min, (int) decode.sec, DateTimeKind.Utc);
			}
			catch
			{
				return duration;
			}

			var end = start.AddSeconds(Math.Max(0, duration));

			var day = Math.Clamp(SirenixEditorFields.IntField(end.Day, GUILayout.MinWidth(NUMBER_WIDTH)), 1, 31);
			FusumityEditorGUILayout.SuffixValue(null, day, TimeUtility.SHORT_DAY_LABEL, textStyle: _suffixTextStyle);

			var month = SirenixEditorFields.Dropdown(end.Month - 1, MonthNames(), GUILayout.MinWidth(78)) + 1;

			var year = Math.Clamp((int) SirenixEditorFields.LongField(end.Year, GUILayout.MinWidth(NUMBER_WIDTH)),
				1, 9999);

			var time = DrawTime((long) end.TimeOfDay.TotalSeconds);

			// День зажимаем в границы месяца: «31-е» в феврале иначе бросит исключение
			day = Math.Min(day, DateTime.DaysInMonth(year, month));

			var result = (long) (new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)
				.AddSeconds(time) - start).TotalSeconds;

			return Math.Max(0, result);
		}

		private long DrawTime(long daySeconds)
		{
			var hours = Math.Clamp((int) (daySeconds / TimeUtility.SECS_IN_ONE_HOUR), 0, 23);
			var minutes = Math.Clamp((int) (daySeconds / TimeUtility.SECS_IN_ONE_MINUTE % 60), 0, 59);
			var seconds = Math.Clamp((int) (daySeconds % 60), 0, 59);

			hours = Math.Clamp(Field(hours, TimeUtility.HOUR_LABEL), 0, 23);
			minutes = Math.Clamp(Field(minutes, TimeUtility.MINUTE_LABEL), 0, 59);
			seconds = Math.Clamp(Field(seconds, TimeUtility.SECOND_LABEL), 0, 59);

			return hours * TimeUtility.SECS_IN_ONE_HOUR + minutes * TimeUtility.SECS_IN_ONE_MINUTE + seconds;
		}

		private static string[] _weekDayNames;
		private static string[] _monthNames;

		/// <summary>
		/// Кэш: дроверы зовут это каждый кадр IMGUI, а локаль редактора между кадрами
		/// не меняется. Имена — из локали, как в дровере кода точки
		/// </summary>
		private static string[] WeekDayNames()
		{
			if (_weekDayNames != null)
				return _weekDayNames;

			var format = CultureInfo.CurrentUICulture.DateTimeFormat;

			return _weekDayNames = new[]
			{
				format.GetDayName(DayOfWeek.Monday),
				format.GetDayName(DayOfWeek.Tuesday),
				format.GetDayName(DayOfWeek.Wednesday),
				format.GetDayName(DayOfWeek.Thursday),
				format.GetDayName(DayOfWeek.Friday),
				format.GetDayName(DayOfWeek.Saturday),
				format.GetDayName(DayOfWeek.Sunday)
			};
		}

		/// <inheritdoc cref="WeekDayNames"/>
		private static string[] MonthNames()
		{
			if (_monthNames != null)
				return _monthNames;

			var names = new string[12];

			for (var i = 0; i < names.Length; i++)
				names[i] = CultureInfo.CurrentUICulture.DateTimeFormat.GetMonthName(i + 1);

			return _monthNames = names;
		}

		#endregion

		/// <summary>
		/// Ближайшее окно целиком — иначе по длительности не видно, куда оно реально ляжет
		/// </summary>
		private void DrawPreview(ISchedulePoint point, long duration)
		{
			string text;

			if (duration <= 0)
			{
				text = MOMENT_LABEL.ColorText(_previewStyle.normal.textColor.WithAlpha(0.5f));
			}
			else
			{
				try
				{
					var start = point.ToDateTime(DateTime.UtcNow);
					var end = start.AddSeconds(duration);

					text = $" start — {start.ToString("ddd dd MMM, HH:mm", CultureInfo.InvariantCulture)}" +
						$"   ·   end — {end.ToString("ddd dd MMM, HH:mm", CultureInfo.InvariantCulture)}" +
						$"   ·   {CompactSpan(duration)}";
				}
				catch
				{
					text = CompactSpan(duration);
				}
			}

			GUILayout.Label(text, _previewStyle);
		}

		private static string CompactSpan(long seconds)
			=> ScheduleEditorFormat.CompactSpan(seconds);
	}
}
