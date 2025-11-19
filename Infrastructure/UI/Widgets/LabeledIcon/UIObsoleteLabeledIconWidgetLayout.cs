using TMPro;
using UnityEngine.UI;

namespace UI
{
	public class UIObsoleteLabeledIconWidgetLayout : UILocalizedBaseLayout
	{
		public bool useLayoutAnimations;
		public Image icon;
		public TMP_Text label;

		public StateSwitcher<string> stateSwitcher;

		public override TMP_Text Label => label;
		public override bool UseLayoutAnimations => useLayoutAnimations;
	}
}
