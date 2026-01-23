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

namespace UI.Popups.Editor
{
	public class UIDispatcherEditorPopupTabAttributeProcessor : OdinAttributeProcessor<UIDispatcherEditorPopupTab>
	{
		private static List<Type> _cachedExporterTypes;

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(UIDispatcherEditorPopupTab.popup):
					attributes.Add(new BoxGroupAttribute("Box", showLabel: false));
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal"));
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					attributes.Add(new HideLabelAttribute());
					break;

				case nameof(UIDispatcherEditorPopupTab.argsInspector):
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new DarkCardBoxAttribute("Box/Horizontal/left/color"));
					attributes.Add(new HideIfAttribute(nameof(UIDispatcherEditorPopupTab.argsInspector),
						UIWidgetArgsInspector.Empty));
					break;

				case nameof(UIDispatcherEditorPopupTab.Show):
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal", 180));
					var buttonAttribute = new ButtonAttribute(ButtonSizes.Large)
					{
						Style = Sirenix.OdinInspector.ButtonStyle.FoldoutButton
					};
					attributes.Add(buttonAttribute);
					attributes.Add(new EnableIfAttribute(nameof(UIDispatcherEditorPopupTab.popup), null));
					break;
			}
		}
	}
}
