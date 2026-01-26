using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIStatefulButtonLayout : UIBaseLayout
	{
		public Button button;

		[Space]
		public TMP_Text label;
		public Image icon;

		[Space]
		public StateSwitcher<string> switcher;
		public UIAdBannerLayout adBanner;
		public UILabeledIconLayout labeledIcon;

		[Space]
		public string uId;
		public string groupId;
	}
}
