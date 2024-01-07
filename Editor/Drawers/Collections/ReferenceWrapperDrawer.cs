using Fusumity.Collections;
using Fusumity.Editor.Drawers.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Collections
{
	[CustomPropertyDrawer(typeof(ReferenceWrapper<,>))]
	public class ReferenceWrapperDrawer : ReferenceSelectionAttributeDrawer
	{
		private const string VALUE_NAME = "_value";

		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var valueProperty = property.FindPropertyRelative(VALUE_NAME);

			currentPropertyData.bodyHeight = valueProperty.GetBodyHeight() - EditorGUIUtility.singleLineHeight;
		}

		public override void DrawSubBody(Rect position)
		{
			var property = currentPropertyData.property;
			var fieldType = fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType() : fieldInfo.FieldType;

			var targetType = fieldType!.GetGenericArguments()[1];
			var currentType = property.GetPropertyTypeByLocalPath(VALUE_NAME);

			var targetProperty = property.FindPropertyRelative(VALUE_NAME);
			targetProperty.DrawTypeSelector(position, targetType, currentType, SUB_BODY_GUI_CONTENT, true);
		}

		public override void DrawBody(Rect position)
		{
			var property = currentPropertyData.property;
			var valueProperty = property.FindPropertyRelative(VALUE_NAME);

			valueProperty.DrawBody(position);
		}
	}
}