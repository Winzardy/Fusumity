using Fusumity.Attributes.Specific;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(RemoveFoldoutAttribute))]
	public class RemoveFoldoutDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			propertyData.property.isExpanded = true;
			propertyData.hasFoldout = false;
		}
	}
}
