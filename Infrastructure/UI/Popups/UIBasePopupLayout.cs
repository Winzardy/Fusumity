using UnityEngine.UI;

namespace UI.Popups
{
	public class UIBasePopupLayout : UIBaseContainerLayout
	{
		public override bool UseLayoutAnimations => useAnimations;
		public bool useAnimations = true;

		public Button close;
	}
}
