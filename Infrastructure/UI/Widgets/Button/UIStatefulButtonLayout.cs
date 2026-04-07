using System;
using ActionBusSystem;
using Content;
using Fusumity.Attributes.Odin;
using Fusumity.MVVM.UI;
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
		public UIAttentionIndicatorLayout indicator;

		[Space]
		[ContentReference(typeof(ActionBusElementConfig), dropdown: true)]
		public string uId;

		[ContentReference(typeof(ActionBusGroupConfig), dropdown: true)]
		public string groupId;
	}

	[HideLabel]
	[Serializable]
	public struct ActionBusButtonScheme
	{
		public Button button;

		[ShowIf(nameof(button), null)]
		[ConstDropdown(typeof(ActionBusElementType))]
		public string uId;

		[ShowIf(nameof(button), null)]
		[ConstDropdown(typeof(ActionBusGroupType))]
		public string groupId;
	}
}
