using System;
using Fusumity.Attributes.Specific;
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

			var elementIndex = currentPropertyData.label.text.Replace("Element ", string.Empty);
			if (labelAttribute.indexOffset != 0)
				elementIndex = (int.Parse(elementIndex) + labelAttribute.indexOffset).ToString();
			currentPropertyData.labelPrefix = elementIndex;

			var charCount = Math.Max(currentPropertyData.labelPrefix.Length + 1, 3);
			currentPropertyData.labelPrefixWidth = charCount * 7f;

			currentPropertyData.hasLabel = false;
			currentPropertyData.hasFoldout = labelAttribute.hasFoldout;
		}
	}
}