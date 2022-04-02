using Fusumity.Editor.Utilities;
using Fusumity.Attributes.Specific;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ButtonAttribute))]
	public class ButtonDrawer : GenericPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			propertyData.hasBeforeExtension = true;
			propertyData.beforeExtensionHeight += EditorGUIUtility.singleLineHeight;

			var boolButtonAttribute = (ButtonAttribute)attribute;
			if (boolButtonAttribute.hideProperty)
			{
				propertyData.drawProperty = false;
			}
		}

		public override void DrawBeforeExtension(ref Rect position)
		{
			base.DrawBeforeExtension(ref position);

			var boolButtonAttribute = (ButtonAttribute)attribute;

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

			propertyData.property.InvokeMethodByLocalPath(boolButtonAttribute.methodPath);
		}
	}
}
