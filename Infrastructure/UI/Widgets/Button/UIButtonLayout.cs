using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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

		public void Subscribe(UnityAction action) => button.onClick.AddListener(action);

		public void Unsubscribe(UnityAction action) => button.onClick.RemoveListener(action);

		protected override void Reset()
		{
			base.Reset();

			button = GetComponentInChildren<Button>();
			label = GetComponentInChildren<TMP_Text>();
		}
	}
}
