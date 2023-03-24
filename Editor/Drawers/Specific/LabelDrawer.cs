using Fusumity.Attributes.Specific;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(LabelAttribute))]
	public class LabelDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var labelAttribute = (LabelAttribute)attribute;
			currentPropertyData.label.text = labelAttribute.label;
		}
	}
}