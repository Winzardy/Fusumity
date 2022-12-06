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

			var property = currentPropertyData.property;
			var disableIfAttribute = (DisableIfAttribute)attribute;
			var boolProperty = property.GetPropertyByLocalPath(disableIfAttribute.boolPath);

			currentPropertyData.isEnabled = !boolProperty.boolValue;
		}
	}
}
