using Fusumity.Collections;
using Fusumity.Editor.Drawers.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Collections
{
	[CustomPropertyDrawer(typeof(ReferenceWrapper<,>))]
	public class ReferenceWrapperDrawer : SerializeReferenceSelectorAttributeDrawer
	{
		private const string _valueName = "_value";

		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var valueProperty = property.FindPropertyRelative(_valueName);

			currentPropertyData.bodyHeight = valueProperty.GetBodyHeight() - EditorGUIUtility.singleLineHeight;
		}

		public override void DrawSubBody(Rect position)
		{
			var property = currentPropertyData.property;
			var fieldType = fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType() : fieldInfo.FieldType;

			var valueType = fieldType.GetGenericArguments()[1];

			var targetType = valueType;
			var currentType = property.GetPropertyTypeByLocalPath(_valueName);

			SelectType(position, currentType, targetType, true);
		}

		protected override void SetValue(SerializedProperty property, object value)
		{
			base.SetValue(property.FindPropertyRelative(_valueName), value);
		}

		public override void DrawBody(Rect position)
		{
			var property = currentPropertyData.property;
			var valueProperty = property.FindPropertyRelative(_valueName);

			valueProperty.DrawBody(position);
		}
	}
}