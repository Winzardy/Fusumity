using UnityEngine.UI;

namespace UI
{
	public static class UILayoutGroupUtility
	{
		public static bool IsReverse(this LayoutGroup layoutGroup)
		{
			if (layoutGroup is HorizontalOrVerticalLayoutGroup horizontalOrVerticalLayoutGroup)
				return horizontalOrVerticalLayoutGroup.reverseArrangement;

			if (layoutGroup is RadialLayoutGroup radialLayoutGroup)
				return radialLayoutGroup.clockwise;

			return false;
		}
	}
}
