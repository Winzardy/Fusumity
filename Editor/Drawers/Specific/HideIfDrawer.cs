using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(HideIfAttribute))]
	public class HideIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = propertyData.property;
			var hideIfAttribute = (HideIfAttribute)attribute;
			var boolProperty = property.GetPropertyByLocalPath(hideIfAttribute.boolPath);

			propertyData.drawProperty = !boolProperty.boolValue;
		}
	}
}
