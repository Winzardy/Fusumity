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

			var property = currentPropertyData.property;
			var maxAttribute = (MaximumAttribute)attribute;

			var maxInt = maxAttribute.maxInt;
			var maxFloat = maxAttribute.maxFloat;

			if (!string.IsNullOrEmpty(maxAttribute.maxPath))
			{
				var maxProperty = property.GetPropertyByLocalPath(maxAttribute.maxPath);

				switch (maxProperty?.propertyType)
				{
					case SerializedPropertyType.Integer:
						maxInt = Math.Max(maxProperty.intValue, maxInt);
						maxFloat = Math.Max((float)maxProperty.intValue, maxFloat);
						break;
					case SerializedPropertyType.Float:
					case SerializedPropertyType.Vector2:
						maxInt = Math.Max((int)maxProperty.floatValue, maxInt);
						maxFloat = Math.Max(maxProperty.floatValue, maxFloat);
						break;
				}
			}

			switch (currentPropertyData.property.propertyType)
			{
				case SerializedPropertyType.Integer:
					if (currentPropertyData.property.intValue >maxInt)
					{
						currentPropertyData.property.intValue = maxInt;
					}
					break;
				case SerializedPropertyType.Float:
					if (currentPropertyData.property.floatValue > maxFloat)
					{
						currentPropertyData.property.floatValue = maxFloat;
					}
					break;
				case SerializedPropertyType.Vector2:
					var vector = currentPropertyData.property.vector2Value;
					if (vector.x > maxFloat)
					{
						vector.x = maxFloat;
						currentPropertyData.property.vector2Value = vector;
					}
					if (vector.y > maxFloat)
					{
						vector.y = maxFloat;
						currentPropertyData.property.vector2Value = vector;
					}
					break;
			}
		}
	}
}