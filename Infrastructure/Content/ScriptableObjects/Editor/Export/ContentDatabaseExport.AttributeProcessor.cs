using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Utility;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sapientia.Reflection;
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
				case nameof(ContentDatabaseExport.options):
					attributes.Add(new TitleAttribute("Options"));
					attributes.Add(new HideLabelAttribute());
					break;

				case nameof(ContentDatabaseExport.type):
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new BoxGroupAttribute("Box", showLabel: false));
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal"));
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					attributes.Add(new LabelTextAttribute("Type"));

					var className = nameof(ContentDatabaseExportAttributeProcessor);
					attributes.Add(new ValueDropdownAttribute($"@{className}.{nameof(GetExporters)}()"));
					break;

				case ContentDatabaseExport.ARGS_FIELD_NAME:
					attributes.Add(new HideReferenceObjectPickerAttribute());
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new VerticalGroupAttribute("Box/Horizontal/left"));
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new ShowIfAttribute(ContentDatabaseExport.ARGS_FIELD_NAME, null));

					break;

				case nameof(ContentDatabaseExport.Button):
					attributes.Add(new HorizontalGroupAttribute("Box/Horizontal", 100));
					attributes.Add(new ButtonAttribute("Export", ButtonSizes.Large));
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

	public class ContentDatabaseExportOptionsAttributeProcessor : OdinAttributeProcessor<ContentDatabaseExportOptions>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(ContentDatabaseExportOptions.skipDatabases):
					var className = nameof(ContentDatabaseExportOptionsAttributeProcessor);
					var dropdownAttribute = new ValueDropdownAttribute($"@{className}.{nameof(GetDatabases)}()")
					{
						IsUniqueList = true
					};
					attributes.Add(dropdownAttribute);
					break;
			}
		}

		private static IEnumerable GetDatabases()
		{
			foreach (var database in ContentDatabaseEditorUtility.Databases)
			{
				var name = database.name;
				yield return new ValueDropdownItem(database.ToLabel(), name);
			}
		}
	}
}
