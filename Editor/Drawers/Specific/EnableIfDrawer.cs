using Fusumity.Editor.Utilities;
using Fusumity.Attributes.Specific;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(EnableIfAttribute))]
	public class EnableIfDrawer : GenericPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = propertyData.property;
			var enableIfAttribute = (EnableIfAttribute)attribute;
			var boolProperty = property.GetPropertyByPropertyLocalPath(enableIfAttribute.boolPath);

			propertyData.isEnabled = boolProperty.boolValue;
		}
	}
}
