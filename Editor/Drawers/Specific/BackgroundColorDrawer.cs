using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(BackgroundColorAttribute))]
	public class BackgroundColorDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var backgroundColorAttribute = (BackgroundColorAttribute)attribute;

			if (!string.IsNullOrEmpty(backgroundColorAttribute.conditionPath))
			{
				var property = propertyData.property;
				var condition = property.GetPropertyByLocalPath(backgroundColorAttribute.conditionPath).boolValue;
				if (!condition)
					return;
			}
			if (!string.IsNullOrEmpty(backgroundColorAttribute.invertConditionPath))
			{
				var property = propertyData.property;
				var invertCondition = property.GetPropertyByLocalPath(backgroundColorAttribute.invertConditionPath).boolValue;
				if (invertCondition)
					return;
			}
			propertyData.backgroundColor = backgroundColorAttribute.color;
		}
	}
}