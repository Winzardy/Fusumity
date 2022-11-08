using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(DisableIfAttribute))]
	public class DisableIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = propertyData.property;
			var disableIfAttribute = (DisableIfAttribute)attribute;
			var boolProperty = property.GetPropertyByLocalPath(disableIfAttribute.boolPath);

			propertyData.isEnabled = !boolProperty.boolValue;
		}
	}
}
