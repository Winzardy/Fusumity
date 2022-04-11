using Fusumity.Editor.Utilities;
using Fusumity.Attributes.Specific;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ShowIfAttribute))]
	public class ShowIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = propertyData.property;
			var showIfAttribute = (ShowIfAttribute)attribute;
			var boolProperty = property.GetPropertyByLocalPath(showIfAttribute.boolPath);

			propertyData.drawProperty = boolProperty.boolValue;
		}
	}
}
