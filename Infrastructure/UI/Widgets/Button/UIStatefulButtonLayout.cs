using ActionBusSystem;
using Fusumity.Attributes.Odin;
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
		[ConstDropdown(typeof(ActionBusElementType))]
		public string uId;
		[ConstDropdown(typeof(ActionBusGroupType))]
		public string groupId;
	}
}
