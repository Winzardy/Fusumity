using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Content.ScriptableObjects.Editor
{
	public class ContentFilteringAttributeProcessor : OdinAttributeProcessor<ContentFiltering>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(ContentFiltering.skipDatabases):
					var className = nameof(ContentFilteringAttributeProcessor);
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
