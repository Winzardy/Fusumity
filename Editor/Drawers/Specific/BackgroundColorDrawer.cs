using Fusumity.Attributes.Specific;
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

			propertyData.backgroundColor = backgroundColorAttribute.color;
		}
	}
}