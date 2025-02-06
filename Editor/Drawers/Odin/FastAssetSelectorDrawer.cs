using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fusumity.Editor.Drawers
{
	[DrawerPriority(0, 9000, 0)]
	public class FastAssetSelectorDrawer : OdinAttributeDrawer<FastAssetSelectorAttribute>
	{
		protected override bool CanDrawAttributeProperty(InspectorProperty property)
		{
			return base.CanDrawAttributeProperty(property) && property.ValueEntry.TypeOfValue.InheritsFrom(typeof(Object));
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var propertyType = Property.ValueEntry.TypeOfValue;
			var targetType = Attribute.type ?? propertyType;
			((Object)Property.ValueEntry.WeakSmartValue).DrawAssetSelector(label, propertyType, targetType, OnSelected);
		}

		private void OnSelected(Object target)
		{
			Property.ValueEntry.WeakSmartValue = target;

			var root = Property.SerializationRoot.ValueEntry.WeakSmartValue as Object;
			EditorUtility.SetDirty(root);
			AssetDatabase.SaveAssetIfDirty(root);
		}
	}
}
