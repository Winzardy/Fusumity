using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Utility;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Content.ScriptableObjects.Editor
{
	public class ContentDatabaseExportAttributeProcessor : OdinAttributeProcessor<ContentDatabaseExport>
	{
		private static List<Type> _cachedExporterTypes;

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(ContentDatabaseExport.settings):
					attributes.Add(new PropertySpaceAttribute(0, 10));
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new PropertyOrderAttribute(-1));
					break;

				case nameof(ContentDatabaseExport.type):
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new BoxGroupAttribute("Box", showLabel: false));
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal"));
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					attributes.Add(new LabelTextAttribute("Export To"));

					var className = nameof(ContentDatabaseExportAttributeProcessor);
					attributes.Add(new ValueDropdownAttribute($"@{className}.{nameof(GetExporters)}()"));
					break;

				case ContentDatabaseExport.ARGS_FIELD_NAME:
					attributes.Add(new HideReferenceObjectPickerAttribute());
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					attributes.Add(new DarkCardBoxAttribute("Box/Horizontal/left/card"));
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new ShowIfAttribute(ContentDatabaseExport.ARGS_FIELD_NAME, null));

					break;

				case nameof(ContentDatabaseExport.Export):
					if (member is MethodInfo method)
					{
						if (method.GetParameters().Length > 0)
							break;
					}

					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal", 100));
					attributes.Add(new ButtonAttribute("Run", ButtonSizes.Large));
					break;
			}
		}

		private static IEnumerable GetExporters()
		{
			_cachedExporterTypes ??= ReflectionUtility.GetAllTypes<IContentDatabaseExporter>(editor: true);
			foreach (var type in _cachedExporterTypes)
			{
				var label = type.Name
					.Remove("ContentDatabase")
					.Remove("Exporter")
					.NicifyText();

				yield return new ValueDropdownItem(label, type);
			}
		}
	}
}
