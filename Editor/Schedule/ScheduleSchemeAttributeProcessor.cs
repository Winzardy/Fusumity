using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;

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
}
