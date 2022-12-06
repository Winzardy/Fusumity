using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(EnableIfAttribute))]
	public class EnableIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var enableIfAttribute = (EnableIfAttribute)attribute;
			var boolProperty = property.GetPropertyByLocalPath(enableIfAttribute.boolPath);

			currentPropertyData.isEnabled = boolProperty.boolValue;
		}
	}
}
