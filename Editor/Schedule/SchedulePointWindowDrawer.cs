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

		/// <summary>Режим ввода общий на редактор: дизайнер переключает его один раз, а не на каждой точке</summary>
		private const string PIN_END_PREF = "ScheduleWindow.PinEnd";

		private const float MODE_WIDTH = 78f;
		private const int MAX_DAYS = (int) (ScheduleEditorFormat.MAX_SECONDS / TimeUtility.SECS_IN_ONE_DAY);
		private const float LABEL_PADDING = 4f;

		private static readonly GUIContent RANGE_LABEL =
			new("Range", "Точка становится отрезком [дата, дата + длительность) — в данных это «окно», " +
				"сам отрезок хранится длительностью, в durations схемы параллельно точкам\n\n" +
				"Until — вводится конец, и при сдвиге точки он остаётся на месте\n" +
				"Duration — вводится длина, и конец едет вместе с точкой");

		/// <summary>Что дизайнер вводит и что остаётся на месте при сдвиге точки</summary>
		private static readonly string[] MODE_NAMES = {"Until", "Duration"};

		private bool _enabled;

		/// <summary>
		/// Тугл окна: выключен — поля не рисуются, длительность обнулена. Инициализируется
		/// от данных при первом показе, дальше живёт как состояние редактора
		/// </summary>
		private bool? _active;

		/// <summary>
		/// Что остаётся на месте при сдвиге точки: конец окна или его длина. Хранится в схеме
		/// всё равно длительность — режим только про ввод
		/// </summary>
		private bool? _pinEnd;

		/// <summary>Начало окна прошлого кадра — по нему видно, на сколько уехала точка</summary>
		private long? _start;

		/// <summary>Код точки прошлого кадра — по нему видно подмену точки под этим индексом</summary>
		private long? _code;

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

			// Без отступов подпись прилипает к полям и к краю карточки и читается как строка
			// следующего свойства, а не как пояснение к этому
			_previewStyle.margin = new RectOffset(6, 0, 4, 3);
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
			// Правку самой точки надо отличать от правки со стороны: undo и календарь меняют код
			// мимо этих полей, и подгонять под них длительность нельзя
			bool moved;

			EditorGUI.BeginChangeCheck();

			try
			{
				CallNextDrawer(label);
			}
			finally
			{
				// finally: на кривом коде точки дровер бросает, и проверка осталась бы незакрытой
				moved = EditorGUI.EndChangeCheck();
			}

			if (!_enabled)
				return;

			// Пара «точка + длительность» существует только внутри схемы
			var schemeProperty = Property.Parent?.Parent;

			if (schemeProperty?.ValueEntry?.WeakSmartValue is not ScheduleScheme scheme)
				return;

			long code;
			SchedulePointDecode decode;

			try
			{
				code = GetCode();
				decode = code;
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

				_start = null;
				return;
			}

			var index = Property.Index;
			var duration = scheme.GetWindowDuration(index);

			// Дровер живёт на индексе элемента, а не на точке: после удаления или перестановки под
			// ним оказывается другая точка. Своя правка меняет код в этом же кадре (moved), всё
			// остальное — признак подмены, и состояние дровера надо перечитать от данных
			if (!moved && _code.HasValue && _code.Value != code)
			{
				_active = null;
				_start = null;
			}

			_code = code;

			var active = _active ??= duration > 0;

			// Ввод концом только там, где конец однозначен: у месячных и годовых правил период
			// плавает, и «до 30-го числа» упирается в разную длину месяцев
			var canPickEnd = CanPickEnd(kind);
			var pinEnd = canPickEnd && (_pinEnd ??= EditorPrefs.GetBool(PIN_END_PREF, true));
			var start = GetStart(in decode, kind);

			// Сдвиг точки не должен таскать конец за собой: раз дизайнер задал именно конец,
			// на месте остаётся он, а пересчитывается длительность
			if (moved && pinEnd && duration > 0 && _start.HasValue && _start.Value != start)
			{
				duration = Reanchor(duration, start - _start.Value, kind);
				SetDuration(schemeProperty, index, duration);
			}

			_start = start;

			// Колонка подписи по ширине слова: стандартная тянется на треть строки, и между
			// подписью и туглом остаётся пустое место
			GUIHelper.PushLabelWidth(EditorStyles.label.CalcSize(RANGE_LABEL).x + LABEL_PADDING);
			SirenixEditorGUI.BeginHorizontalPropertyLayout(RANGE_LABEL);
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
					// Перед полями: так подпись режима читается как заголовок к тому, что в них вводят.
					// У месячных и годовых правил конец не привязать к дню — там режим один и показан
					// выключенным, иначе непонятно, почему выбора нет
					GUIHelper.PushGUIEnabled(canPickEnd);
					DrawMode(pinEnd);
					GUIHelper.PopGUIEnabled();

					EditorGUI.BeginChangeCheck();

					duration = pinEnd
						? DrawUntil(in decode, kind, duration)
						: DrawLength(kind, duration);

					if (EditorGUI.EndChangeCheck())
						SetDuration(schemeProperty, index, Math.Max(0, duration));
				}
			}
			SirenixEditorGUI.EndHorizontalPropertyLayout();
			GUIHelper.PopLabelWidth();

			if (active)
				DrawPreview(new SchedulePoint {code = code}, duration);
		}

		/// <summary>
		/// Код точки этого кадра: правка уходит в дочернее свойство, а в саму точку попадает
		/// только на ApplyChanges — до него окно считало бы конец от прежнего начала
		/// </summary>
		private long GetCode()
		{
			var codeProperty = Property.Children[nameof(SchedulePoint.code)];

			return codeProperty?.ValueEntry?.WeakSmartValue is long code
				? code
				: ValueEntry.SmartValue.code;
		}

		/// <summary>
		/// Переключатель ввода. Хранится всё равно длительность — режим про то, что дизайнер
		/// держит в голове и что остаётся на месте при сдвиге точки
		/// </summary>
		private void DrawMode(bool pinEnd)
		{
			var selected = SirenixEditorFields.Dropdown(pinEnd ? 0 : 1, MODE_NAMES,
				GUILayout.Width(MODE_WIDTH)) == 0;

			if (selected == pinEnd)
				return;

			// Со следующего кадра: смена набора полей посреди отрисовки путает id контролов
			_pinEnd = selected;
			EditorPrefs.SetBool(PIN_END_PREF, selected);
		}

		private void SetDuration(InspectorProperty schemeProperty, int index, long value)
			=> WriteDuration(schemeProperty, index, value);

		/// <summary>
		/// Пишет в durations схемы, растянув его до нужного индекса. Все нули — массив
		/// зануляется: у расписаний без окон данные остаются пустыми
		/// </summary>
		internal static void WriteDuration(InspectorProperty schemeProperty, int index, long value)
		{
			// Единственная точка записи: за потолком переполняются TimeSpan и DateTime, и починить
			// такую длительность из инспектора уже нечем — поля перестают рисоваться
			value = Math.Clamp(value, 0, ScheduleEditorFormat.MAX_SECONDS);

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

		/// <summary>
		/// Начало окна в тех же единицах, в каких живёт длительность: секунды от начала периода
		/// правила, а у разовой даты — от эпохи
		/// </summary>
		private static long GetStart(in SchedulePointDecode decode, SchedulePointKind kind)
		{
			var time = decode.hr * TimeUtility.SECS_IN_ONE_HOUR
				+ decode.min * TimeUtility.SECS_IN_ONE_MINUTE
				+ decode.sec;

			switch (kind)
			{
				case SchedulePointKind.Weekly:
					return decode.day * TimeUtility.SECS_IN_ONE_DAY + time;

				case SchedulePointKind.Date:
					try
					{
						var date = new DateTime((int) decode.yr, decode.mh + 1, (int) decode.day + 1,
							decode.hr, decode.min, (int) decode.sec, DateTimeKind.Utc);

						return (long) (date - DateTime.UnixEpoch).TotalSeconds;
					}
					catch
					{
						return time;
					}

				default:
					return time;
			}
		}

		/// <summary>Пересчитывает длительность так, чтобы конец окна остался на месте</summary>
		/// <remarks>
		/// У повторяющихся правил начало может перешагнуть конец — тогда окно уезжает в следующий
		/// период, ровно как при ручном вводе конца раньше начала. У разовой даты периода нет,
		/// и такое окно схлопывается
		/// </remarks>
		private static long Reanchor(long duration, long shift, SchedulePointKind kind)
		{
			var result = duration - shift;
			var period = PeriodSeconds(kind);

			// У разовой даты периода нет: конца «в следующем периоде» не существует. Начало
			// перешагнуло конец — оставляем длину как есть, иначе окно молча стёрлось бы
			if (period <= 0)
				return result > 0 ? result : duration;

			if (result >= 0 && result <= period)
				return result;

			// Конец вышел за период — заворачиваем, как и при ручном вводе конца раньше начала;
			// без этого сдвиг назад раздувает окно длиннее периода повторения
			result %= period;

			return result < 0 ? result + period : result;
		}

		/// <summary>Период повторения правила — в нём заворачивается конец окна</summary>
		private static long PeriodSeconds(SchedulePointKind kind)
			=> kind switch
			{
				SchedulePointKind.Daily => TimeUtility.SECS_IN_ONE_DAY,
				SchedulePointKind.Weekly => TimeUtility.SECS_IN_ONE_DAY * 7L,
				_ => 0
			};

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

				// У разовой даты периода нет, но потолок всё равно нужен: без него в поле уезжает
				// длительность, на которой переполняются TimeSpan и DateTime
				_ => MAX_DAYS
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

			// С потолком: у даты близко к границе DateTime сложение бросает, а try выше
			// покрывает только сборку начала
			var end = ScheduleEditorFormat.AddSeconds(start, duration);

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

					text = $"start — {start.ToString("ddd dd MMM, HH:mm", CultureInfo.InvariantCulture)}" +
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
