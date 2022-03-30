using Fusumity.Editor.Utilities;
using Fusumity.Attributes.Specific;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(DisableIfAttribute))]
	public class DisableIfDrawer : GenericPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = propertyData.property;
			var disableIfAttribute = (DisableIfAttribute)attribute;
			var boolProperty = property.GetPropertyByPropertyLocalPath(disableIfAttribute.boolPath);

			propertyData.isEnabled = !boolProperty.boolValue;
		}
	}
}
