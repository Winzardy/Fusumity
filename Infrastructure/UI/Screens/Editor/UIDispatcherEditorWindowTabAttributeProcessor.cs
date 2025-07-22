using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace UI.Screens.Editor
{
	public class UIDispatcherEditorWindowTabAttributeProcessor : OdinAttributeProcessor<UIDispatcherEditorScreenTab>
	{
		private static List<Type> _cachedExporterTypes;

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(UIDispatcherEditorScreenTab.type):
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new BoxGroupAttribute("Box", showLabel: false));
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal"));
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					attributes.Add(new HideLabelAttribute());

					var className = nameof(UIDispatcherEditorWindowTabAttributeProcessor);
					attributes.Add(new ValueDropdownAttribute($"@{className}.{nameof(GetAllTypes)}()"));
					break;

				case nameof(UIDispatcherEditorScreenTab.Show):
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal", 80));
					var buttonAttribute = new ButtonAttribute(ButtonSizes.Large)
					{
						Style = Sirenix.OdinInspector.ButtonStyle.FoldoutButton
					};
					attributes.Add(buttonAttribute);
					attributes.Add(new EnableIfAttribute(nameof(UIDispatcherEditorScreenTab.type), null));
					break;
			}
		}

		private static IEnumerable GetAllTypes()
		{
			var types = ReflectionUtility.GetAllTypes<IScreen>(false);
			foreach (var type in types)
			{
				var name = type.Name
				   .Replace("Screen", string.Empty);

				yield return new ValueDropdownItem(name, type);
			}
		}
	}
}
