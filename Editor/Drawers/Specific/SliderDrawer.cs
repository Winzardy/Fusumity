using Fusumity.Attributes.Specific;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(SliderAttribute))]
	public class SliderDrawer : FusumityPropertyDrawer
	{
		public override void DrawSubBody(Rect position)
		{
			var property = currentPropertyData.property;
			var sliderAttribute = (SliderAttribute)attribute;

			var min = sliderAttribute.min;
			var max = sliderAttribute.max;
			if (min > max)
				(min, max) = (max, min);

			if (property.propertyType == SerializedPropertyType.Float)
			{
				property.floatValue = EditorGUI.Slider(position, new GUIContent(" "), property.floatValue, min, max);
			}
			if (property.propertyType == SerializedPropertyType.Integer)
			{
				property.intValue = EditorGUI.IntSlider(position, new GUIContent(" "), property.intValue, (int)min, (int)max);
			}
		}
	}
}