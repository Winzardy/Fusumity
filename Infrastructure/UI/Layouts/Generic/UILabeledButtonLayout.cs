using System;
using ActionBusSystem;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenoTween;

namespace UI
{
	[Obsolete("Будет переделан после того как уберем UIButtonWidget")]
	public class UILabeledButtonLayout : UILocalizedBaseLayout
	{
		public const string ANIMATION_KEY_PREFIX = "style/";

		public Button button;

		[Space]
		public StateSwitcher<string> styleSwitcher;

		[Space]
		public TMP_Text label;

		[Indent, LabelText("Disable Force Rebuild")]
		public bool disableLabelForceRebuild;

		[Indent]
		public bool disableDefaultLabel;

		[Indent]
		public GameObject[] labelGroup;

		public Image icon;

		[Indent]
		public bool disableDefaultIcon;

		[Indent]
		public GameObject[] iconGroup;

		//TODO: убрать, использовать StateSwitcher<string>
		[Obsolete("убрать, использовать StateSwitcher<string>")]
		[ReadOnly]
		[AnimationTweenKey(ANIMATION_KEY_PREFIX)]
		public string defaultStyle = ANIMATION_KEY_PREFIX + UIButtonStyle.DEFAULT;

		[Space]
		[ConstDropdown(typeof(ActionBusElementType))]
		public string uId;

		[ConstDropdown(typeof(ActionBusGroupType))]
		public string groupId;

		public override TMP_Text Label => label;

		protected override void Reset()
		{
			base.Reset();

			button = GetComponentInChildren<Button>();
			label  = GetComponentInChildren<TMP_Text>();
		}
	}
}
