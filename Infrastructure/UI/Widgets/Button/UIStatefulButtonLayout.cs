using ActionBusSystem;
using Fusumity.Attributes.Odin;
using Game.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIStatefulButtonLayout : UIBaseLayout
	{
		public Button button;

		[Space]
		[OptionalSuffixLabel]
		public TMP_Text label;

		[Indent]
		public GameObject[] labelGroup;

		public Image icon;

		[Space]
		[OptionalSuffixLabel]
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
