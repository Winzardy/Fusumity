using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Utility;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UI.Editor;
using UI.Popovers;
using UnityEngine;

namespace UI.Popovers.Editor
{
	public class UIDispatcherEditorPopoverAttributeProcessor : OdinAttributeProcessor<UIDispatcherEditorPopoverTab>
	{
		private static List<Type> _cachedExporterTypes;

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			var className = nameof(UIDispatcherEditorPopoverAttributeProcessor);
			switch (member.Name)
			{
				case nameof(UIDispatcherEditorPopoverTab.type):
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new BoxGroupAttribute("Box", showLabel: false));
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal"));
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					attributes.Add(new HideLabelAttribute());

					var typeExp = $"@{className}.{nameof(GetAllTypes)}()";
					attributes.Add(new ValueDropdownAttribute(typeExp));
					break;

				case nameof(UIDispatcherEditorPopoverTab.argsInspector):
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new HideIfAttribute(nameof(UIDispatcherEditorPopoverTab.argsInspector),
						UIWidgetArgsInspector.Empty));

					AddToGroup();
					break;

				case nameof(UIDispatcherEditorPopoverTab.host):
					attributes.Add(new LabelTextAttribute("Host"));
					AddToGroup();
					break;
				case nameof(UIDispatcherEditorPopoverTab.customAnchor):
					AddToGroup();
					break;

				case nameof(UIDispatcherEditorPopoverTab.Show):
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal", 100));
					var buttonAttribute = new ButtonAttribute(ButtonSizes.Large)
					{
						Style = Sirenix.OdinInspector.ButtonStyle.FoldoutButton
					};
					attributes.Add(buttonAttribute);
					attributes.Add(new EnableIfAttribute(nameof(UIDispatcherEditorPopoverTab.type), null));
					break;
			}

			void AddToGroup() => attributes.Add(new DarkCardBoxAttribute("Box/Horizontal/left/color"));
		}

		private static IEnumerable GetAllTypes()
		{
			var types = ReflectionUtility.GetAllTypes<IPopover>(false);
			foreach (var type in types)
			{
				var name = type.Name
					.Remove("Popover");

				yield return new ValueDropdownItem(name, type);
			}
		}
	}
}
