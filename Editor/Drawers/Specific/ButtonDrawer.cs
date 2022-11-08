using Fusumity.Attributes.Specific;
using Fusumity.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ButtonAttribute))]
	public class ButtonDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var boolButtonAttribute = (ButtonAttribute)attribute;

			if (boolButtonAttribute.drawBefore)
			{
				propertyData.hasBeforeExtension = true;
				propertyData.beforeExtensionHeight += EditorGUIUtility.singleLineHeight;
			}
			else
			{
				propertyData.hasAfterExtension = true;
				propertyData.afterExtensionHeight += EditorGUIUtility.singleLineHeight;
			}

			if (boolButtonAttribute.hidePropertyField)
			{
				propertyData.hasLabel = false;
				propertyData.hasBody = false;
				propertyData.hasSubBody = false;
				propertyData.hasFoldout = false;
			}
		}

		public override void DrawBeforeExtension(ref Rect position)
		{
			base.DrawBeforeExtension(ref position);

			var boolButtonAttribute = (ButtonAttribute)attribute;
			if (!boolButtonAttribute.drawBefore)
				return;

			DrawButton(ref position, boolButtonAttribute);
		}

		public override void DrawAfterExtension(ref Rect position)
		{
			base.DrawAfterExtension(ref position);

			var boolButtonAttribute = (ButtonAttribute)attribute;
			if (boolButtonAttribute.drawBefore)
				return;

			DrawButton(ref position, boolButtonAttribute);
		}

		private void DrawButton(ref Rect position, ButtonAttribute boolButtonAttribute)
		{
			var drawPosition = position;
			drawPosition.height = EditorGUIUtility.singleLineHeight;
			position.yMin += EditorGUIUtility.singleLineHeight;

			var label = string.IsNullOrEmpty(boolButtonAttribute.buttonName)
				? (string.IsNullOrEmpty(boolButtonAttribute.methodPath)
					? propertyData.label
					: new GUIContent(boolButtonAttribute.methodPath))
				: new GUIContent(boolButtonAttribute.buttonName);

			var isPressed = GUI.Button(drawPosition, label);
			if (!isPressed)
				return;

			if (string.IsNullOrEmpty(boolButtonAttribute.methodPath))
				return;

			Undo.RecordObject(propertyData.property.serializedObject.targetObject, boolButtonAttribute.buttonName);
			propertyData.property.InvokeMethodByLocalPath(boolButtonAttribute.methodPath);
		}
	}
}