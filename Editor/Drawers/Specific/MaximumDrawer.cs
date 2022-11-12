using System;
using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(MaximumAttribute))]
	public class MaximumDrawer : FusumityPropertyDrawer
	{
		public override void ValidateBeforeDrawing()
		{
			base.ValidateBeforeDrawing();

			var property = propertyData.property;
			var maxAttribute = (MaximumAttribute)attribute;

			var intmax = maxAttribute.maxInt;
			var floatmax = maxAttribute.maxFloat;

			if (!string.IsNullOrEmpty(maxAttribute.maxPath))
			{
				var maxProperty = property.GetPropertyByLocalPath(maxAttribute.maxPath);

				switch (maxProperty?.propertyType)
				{
					case SerializedPropertyType.Integer:
						intmax = Math.Max(maxProperty.intValue, intmax);
						floatmax = Math.Max((float)maxProperty.intValue, floatmax);
						break;
					case SerializedPropertyType.Float:
					case SerializedPropertyType.Vector2:
						intmax = Math.Max((int)maxProperty.floatValue, intmax);
						floatmax = Math.Max(maxProperty.floatValue, floatmax);
						break;
				}
			}

			switch (propertyData.property.propertyType)
			{
				case SerializedPropertyType.Integer:
					if (propertyData.property.intValue >intmax)
					{
						propertyData.property.intValue = intmax;
					}
					break;
				case SerializedPropertyType.Float:
					if (propertyData.property.floatValue > floatmax)
					{
						propertyData.property.floatValue = floatmax;
					}
					break;
				case SerializedPropertyType.Vector2:
					var vector = propertyData.property.vector2Value;
					if (vector.x > floatmax)
					{
						vector.x = floatmax;
						propertyData.property.vector2Value = vector;
					}
					if (vector.y > floatmax)
					{
						vector.y = floatmax;
						propertyData.property.vector2Value = vector;
					}
					break;
			}
		}
	}
}