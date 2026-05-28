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
	public class ComponentReferenceAttributeProcessor : BaseAssetReferenceAttributeProcessor<ComponentReference>
	{
		protected override string FieldName => "_assetReference";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.Name != IAssetReference.CUSTOM_EDITOR_NAME)
				return;

			if (parentProperty.ValueEntry?.WeakSmartValue is not ComponentReference entry)
				return;

			if (entry.AssetType == null)
				return;

			var className = nameof(ComponentReferenceAttributeProcessor);
			var dropdown = new ValueDropdownAttribute($"@{className}.{nameof(Filter)}($property)");
			dropdown.AppendNextDrawer = true;
			attributes.Add(dropdown);
		}

		public static IEnumerable<GameObject> Filter(InspectorProperty property)
		{
			if (property.Parent.ValueEntry.WeakSmartValue is not ComponentReference entry)
				yield break;

			foreach (var obj in AssetDatabaseUtility.GetPrefabsOfType(entry.AssetType.Name))
				yield return obj;
		}
	}
}
