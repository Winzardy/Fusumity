using Fusumity.Utility;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using TMPro;

namespace UI
{
	public class UILabelLayout : UIBaseLayout
	{
		[InfoBox("Automatically disables GameObject when provided with null string.", InfoMessageType.Info)]
		public TMP_Text label;

		[LabelText("Switcher (optional)")]
		public StateSwitcher<bool> switcher;

		public void SetLabel(string text)
		{
			if (label == null)
				return;

			if (text.IsNullOrEmpty())
			{
				ChangeState(false);
			}
			else
			{
				ChangeState(true);
				label.text = text;
			}
		}

		private void ChangeState(bool active)
		{
			rectTransform.SetActive(active);
			switcher?.Switch(active);
		}

		protected override void Reset()
		{
			base.Reset();
			label = GetComponentInChildren<TMP_Text>();
		}
	}
}
