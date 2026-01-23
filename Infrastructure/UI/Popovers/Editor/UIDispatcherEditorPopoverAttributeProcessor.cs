using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UI.Editor;

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
				case nameof(UIDispatcherEditorPopoverTab.popover):
					attributes.Add(new BoxGroupAttribute("Box", showLabel: false));
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal"));
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					attributes.Add(new HideLabelAttribute());
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
					attributes.Add(new EnableIfAttribute(nameof(UIDispatcherEditorPopoverTab.popover), null));
					break;
			}

			void AddToGroup() => attributes.Add(new DarkCardBoxAttribute("Box/Horizontal/left/color"));
		}
	}
}
