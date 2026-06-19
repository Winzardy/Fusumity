using System;
using ActionBusSystem;
using Fusumity.Attributes;
using Fusumity.Attributes.Odin;
using Fusumity.MVVM.UI;
using Game.UI;
using JetBrains.Annotations;
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

		[Indent]
		public GameObject[] iconGroup;

		[Space]
		[OptionalSuffixLabel]
		public StateSwitcher<string> switcher;

		[CanBeNull]
		public UIAdBannerLayout adBanner;

		[CanBeNull]
		public UILabeledIconLayout labeledIcon;

		[CanBeNull]
		public UIAttentionIndicatorLayout indicator;

		[Space]
		[ConstDropdown(typeof(ActionBusElementType))]
		[CanBeNull]
		public string uId;

		[ConstDropdown(typeof(ActionBusGroupType))]
		[CanBeNull]
		public string groupId;
	}

	[FoldoutContainer]
	[Serializable]
	public struct ActionBusButtonScheme
	{
		public Button button;

		[ShowIf(nameof(button), null)]
		[ConstDropdown(typeof(ActionBusElementType))]
		[CanBeNull]
		public string uId;

		[ShowIf(nameof(button), null)]
		[ConstDropdown(typeof(ActionBusGroupType))]
		[CanBeNull]
		public string groupId;
	}
}
