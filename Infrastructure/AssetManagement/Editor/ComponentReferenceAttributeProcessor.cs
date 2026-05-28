using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Editor;
using Fusumity.Editor.Utility;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

			attributes.Add(new ComponentReferencePickerAttribute(entry.AssetType));
		}

		public static IEnumerable<ValueDropdownItem<GameObject>> Filter(InspectorProperty property)
		{
			if (property.Parent.ValueEntry.WeakSmartValue is not ComponentReference entry)
				yield break;

			foreach (var obj in AssetDatabaseUtility.EnumeratePrefabsOfType(entry.AssetType))
				yield return new ValueDropdownItem<GameObject>(obj.name, obj);
		}
	}

	public class ComponentReferencePickerAttribute : Attribute
	{
		public Type ComponentType { get; set; }

		public ComponentReferencePickerAttribute(Type type)
		{
			ComponentType = type;
		}
	}

	public class ComponentReferencePickerDrawer : OdinAttributeDrawer<ComponentReferencePickerAttribute>
	{
		private Rect? _rect;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Property.Parent.ValueEntry.WeakSmartValue is not IAssetReference reference)
			{
				CallNextDrawer(label);
				return;
			}

			using (new EditorGUI.DisabledScope(Attribute.ComponentType == null))
			{
				if (_rect.HasValue)
				{
					if (GUI.Button(_rect.Value, GUIContent.none))
					{
						var selector = new GenericSelector<Object>("Select",
							AssetDatabaseUtility.EnumeratePrefabsOfType(Attribute.ComponentType),
							false,
							x => x.name);
						selector.SetSelection(reference.EditorAsset);
						selector.EnableSingleClickToSelect();
						selector.SelectionConfirmed += selection =>
						{
							var prefab = selection.FirstOrDefault();
							if (!prefab)
								return;
							reference.SetEditorAsset(prefab);
						};
						var rect = Property.LastDrawnValueRect;
						rect.width -= EditorGUIUtility.labelWidth;
						rect.x     += EditorGUIUtility.labelWidth;
						selector.ShowInPopup(rect); // вот тут нужно чтобы он селектор открывал под полем object
					}
				}
			}

			CallNextDrawer(label);
			_rect = Property.LastDrawnValueRect.AlignRight(18);
		}
	}
}
