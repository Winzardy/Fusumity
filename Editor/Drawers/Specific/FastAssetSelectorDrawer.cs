using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(FastAssetSelectorAttribute), true)]
	public class FastAssetSelectorDrawer : FusumityPropertyDrawer
	{
		public override bool OverrideMethods => (currentPropertyData.property == null || currentPropertyData.property.propertyType == SerializedPropertyType.ObjectReference);

		public override void DrawSubBody(Rect position)
		{
			var property = currentPropertyData.property;
			if (property.propertyType != SerializedPropertyType.ObjectReference)
			{
				base.DrawSubBody(position);
				return;
			}

			var fieldType = fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType() : fieldInfo.FieldType;

			var attr = (FastAssetSelectorAttribute)attribute;
			var targetType = attr.type ?? fieldType;

			currentPropertyData.property.objectReferenceValue.DrawAssetSelector(position, SUB_BODY_GUI_CONTENT, targetType, OnSelected);
		}

		private void OnSelected(Object target)
		{
			currentPropertyData.property.objectReferenceValue = target;
			currentPropertyData.property.serializedObject.ApplyModifiedProperties();
			currentPropertyData.property.serializedObject.Update();
		}
	}
}