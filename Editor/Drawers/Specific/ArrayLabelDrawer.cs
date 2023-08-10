using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ArrayLabelAttribute))]
	public class ArrayLabelDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var labelAttribute = (ArrayLabelAttribute)attribute;

			currentPropertyData.drawOffsetX = -10;

			var elementIndex = currentPropertyData.property.GetElementIndex();
			if (labelAttribute.indexOffset != 0)
				elementIndex += labelAttribute.indexOffset;
			if (labelAttribute.hasName)
			{
				currentPropertyData.labelPrefix = $"{elementIndex} {currentPropertyData.label.text}";
			}
			else
				currentPropertyData.labelPrefix = elementIndex.ToString();

			currentPropertyData.labelPrefixWidth = currentPropertyData.labelPrefix.Length * 7f + 15f;

			currentPropertyData.hasLabel = false;
			currentPropertyData.hasFoldout = labelAttribute.hasFoldout;
		}
	}
}