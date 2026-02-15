using ActionBusSystem;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIButtonLayout : UILocalizedBaseLayout
	{
		public Button button;

		[Space]
		public TMP_Text label;

		[Indent]
		public bool disableDefaultLabel;

		[Indent]
		public GameObject[] labelGroup;

		public Image icon;

		[Indent]
		public bool disableDefaultIcon;

		[Indent]
		public GameObject[] iconGroup;

		[Space]
		public StateSwitcher<string> styleSwitcher;

		public override TMP_Text Label => label;

		[Space]
		[ConstDropdown(typeof(ActionBusElementType))]
		public string uId;
		[ConstDropdown(typeof(ActionBusGroupType))]
		public string groupId;

		protected override void Reset()
		{
			base.Reset();

			button = GetComponentInChildren<Button>();
			label = GetComponentInChildren<TMP_Text>();
		}
	}
}
