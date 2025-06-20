using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.Pool;

namespace AssetManagement.Editor
{
	public class ComponentReferenceEntryAttributeProcessor : BaseAssetReferenceEntryAttributeProcessor<ComponentReferenceEntry>
	{
		protected override string FieldName => "_assetReference";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.Name != IAssetReferenceEntry.CUSTOM_EDITOR_NAME)
				return;

			if (parentProperty.ValueEntry?.WeakSmartValue is not ComponentReferenceEntry entry)
				return;

			if (entry.AssetType == null)
				return;

			var className = nameof(ComponentReferenceEntryAttributeProcessor);
			var dropdown = new ValueDropdownAttribute($"@{className}.{nameof(Filter)}($property)");
			dropdown.AppendNextDrawer = true;
			attributes.Add(dropdown);
		}

		public static IEnumerable<ValueDropdownItem<GameObject>> Filter(InspectorProperty property)
		{
			if (property.Parent.ValueEntry.WeakSmartValue is not ComponentReferenceEntry entry)
				return null;

			using (ListPool<GameObject>.Get(out var list))
			{
				foreach (var obj in AssetDatabaseUtility.LoadPrefabs())
				{
					if (obj.TryGetComponent(entry.AssetType, out _))
						list.Add(obj);
				}

				return list.ToArray().Select(x => new ValueDropdownItem<GameObject>(x.name, x));
			}
		}
	}
}
