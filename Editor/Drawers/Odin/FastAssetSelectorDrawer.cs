using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fusumity.Editor.Drawers
{
	[DrawerPriority(0, 9000, 0)]
	public class FastAssetSelectorDrawer<T> : OdinAttributeDrawer<FastAssetSelectorAttribute, T>
		where T : Object
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var targetType = Attribute.type ?? ValueEntry.TypeOfValue;
			ValueEntry.SmartValue.DrawAssetSelector(label, targetType, OnSelected);
		}

		private void OnSelected(Object target)
		{
			ValueEntry.SmartValue = (T)target;

			var root = Property.SerializationRoot.ValueEntry.WeakSmartValue as Object;
			EditorUtility.SetDirty(root);
			AssetDatabase.SaveAssetIfDirty(root);
		}
	}
}
