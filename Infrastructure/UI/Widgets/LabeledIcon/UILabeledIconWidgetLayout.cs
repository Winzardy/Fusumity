using TMPro;
using UnityEngine.UI;

namespace UI
{
	public class UILabeledIconWidgetLayout : UILocalizedBaseLayout
	{
		public bool useLayoutAnimations;
		public Image icon;
		public TMP_Text label;

		public StateSwitcher<string> stateSwitcher;

		public override TMP_Text Placeholder => label;
		public override bool UseLayoutAnimations => useLayoutAnimations;
	}
}
