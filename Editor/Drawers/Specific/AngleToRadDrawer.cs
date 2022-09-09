using Fusumity.Attributes.Specific;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(AngleToRadAttribute))]
	public class AngleToRadDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			propertyData.label.text = propertyData.label.text.Replace("Rad", "Angle");
			base.ModifyPropertyData();
		}

		public override void DrawSubBody(Rect position)
		{
			var rad = propertyData.property.floatValue;
			EditorGUI.BeginChangeCheck();
			rad = EditorGUI.FloatField(position, " ", rad * Mathf.Rad2Deg) * Mathf.Deg2Rad;
			if (EditorGUI.EndChangeCheck())
				propertyData.property.floatValue = rad;
		}
	}
}