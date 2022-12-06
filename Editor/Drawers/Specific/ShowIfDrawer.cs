using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ShowIfAttribute))]
	public class ShowIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var showIfAttribute = (ShowIfAttribute)attribute;
			var boolProperty = property.GetPropertyByLocalPath(showIfAttribute.boolPath);

			currentPropertyData.drawProperty = boolProperty.boolValue;
		}
	}
}
