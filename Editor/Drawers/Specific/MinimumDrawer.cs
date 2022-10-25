using System;
using Fusumity.Editor.Utilities;
using Fusumity.Attributes.Specific;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(MinimumAttribute))]
	public class MinimumDrawer : FusumityPropertyDrawer
	{
		public override void ValidateBeforeDrawing()
		{
			base.ValidateBeforeDrawing();

			var property = propertyData.property;
			var minAttribute = (MinimumAttribute)attribute;

			var intMin = minAttribute.minInt;
			var floatMin = minAttribute.minFloat;

			if (!string.IsNullOrEmpty(minAttribute.minPath))
			{
				var minProperty = property.GetPropertyByLocalPath(minAttribute.minPath);

				switch (minProperty?.propertyType)
				{
					case SerializedPropertyType.Integer:
						intMin = Math.Max(minProperty.intValue, intMin);
						floatMin = Math.Max((float)minProperty.intValue, floatMin);
						break;
					case SerializedPropertyType.Float:
						intMin = Math.Max((int)minProperty.floatValue, intMin);
						floatMin = Math.Max(minProperty.floatValue, floatMin);
						break;
				}
			}

			switch (propertyData.property.propertyType)
			{
				case SerializedPropertyType.Integer:
					if (propertyData.property.intValue < intMin)
					{
						propertyData.property.intValue = intMin;
					}
					break;
				case SerializedPropertyType.Float:
					if (propertyData.property.floatValue < floatMin)
					{
						propertyData.property.floatValue = floatMin;
					}
					break;
			}
		}
	}
}