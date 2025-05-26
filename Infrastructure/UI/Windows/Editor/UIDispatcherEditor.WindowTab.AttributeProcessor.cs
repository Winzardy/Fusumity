using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UI.Editor;

namespace UI.Windows.Editor
{
	public class UIDispatcherEditorWindowTabAttributeProcessor : OdinAttributeProcessor<UIDispatcherEditorWindowTab>
	{
		private static List<Type> _cachedExporterTypes;

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(UIDispatcherEditorWindowTab.type):
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new BoxGroupAttribute("Box", showLabel: false));
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal"));
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					attributes.Add(new HideLabelAttribute());

					var className = nameof(UIDispatcherEditorWindowTabAttributeProcessor);
					attributes.Add(new ValueDropdownAttribute($"@{className}.{nameof(GetAllTypes)}()"));
					break;

				case nameof(UIDispatcherEditorWindowTab.args):
					attributes.Add(new HideReferenceObjectPickerAttribute());
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					attributes.Add(new UIArgsDarkBoxAttribute());
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new ShowIfAttribute(nameof(UIDispatcherEditorWindowTab.args), null));

					break;

				case nameof(UIDispatcherEditorWindowTab.Show):
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal", 100));
					var buttonAttribute = new ButtonAttribute(ButtonSizes.Large)
					{
						Style = Sirenix.OdinInspector.ButtonStyle.FoldoutButton
					};
					attributes.Add(buttonAttribute);
					attributes.Add(new EnableIfAttribute(nameof(UIDispatcherEditorWindowTab.type), null));
					break;
			}
		}

		private static IEnumerable GetAllTypes()
		{
			var types = ReflectionUtility.GetAllTypes<IWindow>(false);
			foreach (var type in types)
			{
				var name = type.Name
				   .Replace("Window", string.Empty);

				yield return new ValueDropdownItem(name, type);
			}
		}
	}
}
