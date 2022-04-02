using Fusumity.Editor.Utilities;
using Fusumity.Attributes.Specific;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(HideIfAttribute))]
	public class HideIfDrawer : GenericPropertyDrawer
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
