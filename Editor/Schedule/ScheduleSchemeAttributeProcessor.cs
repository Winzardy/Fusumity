using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class ScheduleSchemeAttributeProcessor : ValueWrapperOdinAttributeProcessor<ScheduleScheme>
	{
		private const string TYPE = nameof(ScheduleSchemeAttributeProcessor);

		protected override string ValueFieldName => nameof(ScheduleScheme.points);

		protected override string EmptyLabel { get => "Schedule"; }

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			// Длительности рисуются в паре со своими точками (SchedulePointWindowDrawer),
			// самому массиву в инспекторе делать нечего
			if (member.Name == nameof(ScheduleScheme.durations))
			{
				attributes.Add(new UnityEngine.HideInInspector());
				return;
			}

			if (member.Name != nameof(ScheduleScheme.points))
				return;

			attributes.Add(new ListDrawerSettingsAttribute
			{
				OnTitleBarGUI = $"@{TYPE}.{nameof(DrawCalendarBar)}($property)"
			});
		}

		public static void DrawCalendarBar(InspectorProperty property)
		{
			if (!SirenixEditorGUI.ToolbarButton(SdfIconType.CalendarWeek))
				return;

			// Окно поднимается вне отрисовки дерева Odin — иначе рвётся его стек свойств
			var opened = property;
			EditorApplication.delayCall += () => ScheduleCalendarWindow.Open(opened);
		}
	}

	/// <summary>
	/// Держит <see cref="ScheduleScheme.durations"/> параллельным точкам: список в инспекторе
	/// умеет удалять, вставлять и перетаскивать элементы, а длительности лежат отдельным
	/// массивом и сами за точками не едут — окно молча переезжало бы на чужое правило
	/// </summary>
	public sealed class ScheduleSchemeDurationsDrawer : OdinValueDrawer<ScheduleScheme>
	{
		/// <summary>
		/// Длительность точки, вынутой предыдущей правкой: перетаскивание элемента приходит
		/// парой «удалить + вставить», и без переноса точка теряла бы своё окно
		/// </summary>
		private long? _moved;

		private long _movedCode;

		protected override void Initialize()
		{
			if (Property.Children[nameof(ScheduleScheme.points)]?.ChildResolver is not ICollectionResolver resolver)
				return;

			// Сначала снимаем: дровер могли пересоздать поверх живого резолвера, а вторая
			// подписка сдвинула бы длительности дважды. Отписки в teardown нет и не нужно —
			// резолвер живёт ровно столько же, сколько свойство, и умирает вместе с ним
			resolver.OnBeforeChange -= Shift;
			resolver.OnBeforeChange += Shift;
		}

		protected override void DrawPropertyLayout(GUIContent label) => CallNextDrawer(label);

		/// <summary>
		/// Повторяет правку списка на массиве длительностей — до того, как индексы уедут
		/// </summary>
		private void Shift(CollectionChangeInfo info)
		{
			var scheme = ValueEntry.SmartValue;

			if (scheme.durations.IsNullOrEmpty())
				return;

			var index = info.Index;

			switch (info.ChangeType)
			{
				case CollectionChangeType.RemoveIndex:
					break;

				case CollectionChangeType.RemoveValue:
					index = IndexOf(scheme.points, info.Value);
					break;

				case CollectionChangeType.Insert:
					scheme.durations = Insert(scheme.durations, index, Restore(info.Value));
					ValueEntry.SmartValue = scheme;

					return;

				case CollectionChangeType.Clear:
					scheme.durations = null;
					_moved = null;
					ValueEntry.SmartValue = scheme;

					return;

				// Add дописывает в конец: индекса за длиной массива всё равно нет, окно нулевое
				default:
					return;
			}

			if (index < 0 || index >= scheme.durations.Length)
				return;

			// Запоминаем окно вынутой точки: следующей правкой может прийти её же вставка
			_moved = scheme.durations[index];
			_movedCode = index < scheme.points.Length ? scheme.points[index].code : 0;

			scheme.durations = RemoveAt(scheme.durations, index);
			ValueEntry.SmartValue = scheme;
		}

		/// <summary>Окно возвращается вставленной точке, только если вынимали ровно её</summary>
		private long Restore(object value)
		{
			var duration = _moved.HasValue && value is SchedulePoint point && point.code == _movedCode
				? _moved.Value
				: 0;

			_moved = null;

			return duration;
		}

		private static int IndexOf(SchedulePoint[] points, object value)
		{
			if (points == null || value is not SchedulePoint point)
				return -1;

			for (var i = 0; i < points.Length; i++)
			{
				if (points[i].code == point.code)
					return i;
			}

			return -1;
		}

		private static long[] RemoveAt(long[] durations, int index)
		{
			var updated = new long[durations.Length - 1];

			Array.Copy(durations, updated, index);
			Array.Copy(durations, index + 1, updated, index, durations.Length - index - 1);

			// Не схлопываем в null даже из одних нулей: следом может прийти вставка той же
			// точки (перетаскивание), и ей некуда будет вернуть окно
			return updated;
		}

		private static long[] Insert(long[] durations, int index, long duration)
		{
			if (index < 0 || index > durations.Length)
				return durations;

			var updated = new long[durations.Length + 1];

			Array.Copy(durations, updated, index);
			Array.Copy(durations, index, updated, index + 1, durations.Length - index);
			updated[index] = duration;

			return updated;
		}
	}
}
