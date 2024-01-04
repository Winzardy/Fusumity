using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ReferenceSelectionAttribute))]
	public class ReferenceSelectionAttributeDrawer : FusumityPropertyDrawer
	{
		public override bool OverrideMethods => (currentPropertyData.property == null || currentPropertyData.property.propertyType == SerializedPropertyType.ManagedReference);

		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			if (!OverrideMethods || property.propertyType != SerializedPropertyType.ManagedReference)
				return;

			currentPropertyData.labelIntersectSubBody = false;
			currentPropertyData.hasFoldout = property.managedReferenceValue != null;
			currentPropertyData.hasSubBody = true;
			currentPropertyData.hasBody = property.GetManagedReferenceType() != null;
		}

		public override void DrawSubBody(Rect position)
		{
			var property = currentPropertyData.property;
			if (property.propertyType != SerializedPropertyType.ManagedReference)
			{
				base.DrawSubBody(position);
				return;
			}

			var fieldType = fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType() :
				(fieldInfo.FieldType.IsList() ? fieldInfo.FieldType.GetGenericArguments()[0] : fieldInfo.FieldType);

			var attr = (ReferenceSelectionAttribute)attribute;
			var targetType = attr.type ?? fieldType;
			var currentType = property.GetManagedReferenceType();

			property.DrawTypeSelector(position, targetType, currentType, SUB_BODY_GUI_CONTENT, attr.insertNull);
		}
	}

}