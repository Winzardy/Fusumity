using TMPro;
using UnityEngine.UI;

namespace UI
{
	public class UILabeledIconWidgetLayout : UILocalizedBaseLayout
	{
		public bool useLayoutAnimations;
		public Image icon;
		public TMP_Text label;
		public TMP_Text additionLabel;

		// TODO: убрать как вернусь из отпуска
		public StateSwitcher<bool> stateSwitcher;

		public override TMP_Text Placeholder => label;
		public override bool UseLayoutAnimations => useLayoutAnimations;
	}
}
