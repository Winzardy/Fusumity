using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
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
				currentPropertyData.hasBeforeExtension = true;
				currentPropertyData.beforeExtensionHeight += EditorGUIUtility.singleLineHeight;
			}
			else
			{
				currentPropertyData.hasAfterExtension = true;
				currentPropertyData.afterExtensionHeight += EditorGUIUtility.singleLineHeight;
			}

			if (boolButtonAttribute.hidePropertyField)
			{
				currentPropertyData.hasLabel = false;
				currentPropertyData.hasBody = false;
				currentPropertyData.hasSubBody = false;
				currentPropertyData.hasFoldout = false;
			}
		}

		public override void DrawBeforeExtension(ref Rect position)
		{
			base.DrawBeforeExtension(ref position);

			var buttonAttribute = (ButtonAttribute)attribute;
			if (!buttonAttribute.drawBefore)
				return;

			DrawButton(ref position, buttonAttribute);
		}

		public override void DrawAfterExtension(ref Rect position)
		{
			base.DrawAfterExtension(ref position);

			var buttonAttribute = (ButtonAttribute)attribute;
			if (buttonAttribute.drawBefore)
				return;

			DrawButton(ref position, buttonAttribute);
		}

		private void DrawButton(ref Rect position, ButtonAttribute buttonAttribute)
		{
			var isEnabled = GUI.enabled;
			if (buttonAttribute.forceEnable)
				GUI.enabled = true;

			var drawPosition = EditorGUI.IndentedRect(position);
			drawPosition.height = EditorGUIUtility.singleLineHeight;
			position.yMin += EditorGUIUtility.singleLineHeight;

			var label = string.IsNullOrEmpty(buttonAttribute.buttonName)
				? (string.IsNullOrEmpty(buttonAttribute.methodPath)
					? currentPropertyData.label
					: new GUIContent(buttonAttribute.methodPath))
				: new GUIContent(buttonAttribute.buttonName);

			var isPressed = GUI.Button(drawPosition, label);
			if (!isPressed)
				return;

			if (string.IsNullOrEmpty(buttonAttribute.methodPath))
				return;

			Undo.RecordObject(currentPropertyData.property.serializedObject.targetObject, buttonAttribute.buttonName);
			currentPropertyData.property.InvokeMethodByLocalPath(buttonAttribute.methodPath);
			currentPropertyData.property.serializedObject.targetObject.SaveChanges();

			currentPropertyData.forceBreak = true;

			if (buttonAttribute.forceEnable)
				GUI.enabled = isEnabled;
		}
	}
}