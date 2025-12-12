using System;
using System.Collections.Generic;
using System.Reflection;
using Game;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace UI.Editor.Signal
{
	public class UIDispatcherEditorSignalAttributeProcessor : OdinAttributeProcessor<UIDispatcherEditorSignalTab>
	{
		private static List<Type> _cachedExporterTypes;

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(UIDispatcherEditorSignalTab.inspector):
					attributes.Add(new BoxGroupAttribute("Box", showLabel: false));
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal"));
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					attributes.Add(new LabelTextAttribute("Target"));
					attributes.Add(new InfoBoxAttribute(
						"При пустом Target сигнал перехватывает системная логика; " +
						"если выбран виджет — он становится получателем"));
					break;

				case nameof(UIDispatcherEditorSignalTab.signalName):
					attributes.Add(
						new SignalDropdownAttribute(
							$"@{nameof(UIDispatcherEditorSignalAttributeProcessor)}.{nameof(GetAllSignals)}($property)"));
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					break;

				case nameof(UIDispatcherEditorSignalTab.Send):
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal", 100));
					var buttonAttribute = new ButtonAttribute(ButtonSizes.Large)
					{
						Style = Sirenix.OdinInspector.ButtonStyle.FoldoutButton,
						ButtonHeight = 40
					};
					attributes.Add(buttonAttribute);
					attributes.Add(new EnableIfAttribute(nameof(UIDispatcherEditorSignalTab.CanSend)));
					break;
			}
		}

		private static IEnumerable<string> GetAllSignals(InspectorProperty property)
		{
			if (property.ParentValueProperty.ValueEntry.WeakSmartValue is UIDispatcherEditorSignalTab tab)
			{
				if (tab.inspector.IsEmpty)
				{
					foreach (var p in EnumerateDefaultSignals())
						yield return p;
					yield break;
				}

				var widgetType = tab.inspector.widget.GetType();

				foreach (var s in EnumerateSignalConstants(widgetType))
					yield return s;
			}
		}

		private static IEnumerable<string> EnumerateSignalConstants(Type type)
		{
			if (type == null)
				yield break;

			var seen = new HashSet<string>(StringComparer.Ordinal);

			for (var cur = type; cur != null; cur = cur.BaseType)
			{
				var signals = cur.GetNestedType("Signals",
					BindingFlags.Public | BindingFlags.NonPublic);

				if (signals == null)
					continue;

				foreach (var f in signals.GetFields(
					BindingFlags.Public | BindingFlags.NonPublic |
					BindingFlags.Static | BindingFlags.FlattenHierarchy))
				{
					if (f.FieldType != typeof(string)) continue;

					string? value = null;

					if (f.IsLiteral && !f.IsInitOnly)
						value = (string) f.GetRawConstantValue();
					else if (f.IsStatic)
						value = (string?) f.GetValue(null);

					if (!string.IsNullOrEmpty(value) && seen.Add(value))
						yield return value!;
				}
			}
		}

		private static IEnumerable<string> EnumerateDefaultSignals()
		{
			var seen = new HashSet<string>(StringComparer.Ordinal);

			foreach (var f in typeof(Signals).GetFields(
				BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Static | BindingFlags.FlattenHierarchy))
			{
				if (f.FieldType != typeof(string)) continue;

				string value = null;

				if (f.IsLiteral && !f.IsInitOnly)
					value = (string) f.GetRawConstantValue();
				else if (f.IsStatic)
					value = (string) f.GetValue(null);

				if (!string.IsNullOrEmpty(value) && seen.Add(value))
					yield return value!;
			}
		}
	}
}
