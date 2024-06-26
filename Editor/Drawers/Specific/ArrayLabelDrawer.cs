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

			var hasLabel = labelAttribute.hasName && (!labelAttribute.hideNameIfExpanded || !currentPropertyData.property.isExpanded);
			if (hasLabel)
			{
				currentPropertyData.label.text = currentPropertyData.label.text;
			}

			if (!string.IsNullOrEmpty(currentPropertyData.labelPrefix))
				currentPropertyData.labelPrefix += $"-{elementIndex}";
			else
				currentPropertyData.labelPrefix = $"{elementIndex}";

			currentPropertyData.labelPrefixWidth = currentPropertyData.labelPrefix.Length * 7 + 7;
			currentPropertyData.hasLabel = hasLabel;
			currentPropertyData.hasFoldout = labelAttribute.hasFoldout;
		}
	}
}