using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIAdBannerLayout : UIBaseLayout
	{
		public TMP_Text label;
		public Image icon;

		[Space]
		public StateSwitcher<string> switcher;
	}
}
