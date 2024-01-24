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

			string prefix;
			if (labelAttribute.hasName && (!labelAttribute.hideNameIfExpanded || !currentPropertyData.property.isExpanded))
			{
				prefix = $"{elementIndex} {currentPropertyData.label.text}";
			}
			else
				prefix = elementIndex.ToString();

			currentPropertyData.labelPrefixWidth += prefix.Length * 7f + 15f;
			currentPropertyData.labelPrefix = string.IsNullOrEmpty(currentPropertyData.labelPrefix) ? prefix : $"{prefix}|{currentPropertyData.labelPrefix}";

			currentPropertyData.hasLabel = false;
			currentPropertyData.hasFoldout = labelAttribute.hasFoldout;
		}
	}
}