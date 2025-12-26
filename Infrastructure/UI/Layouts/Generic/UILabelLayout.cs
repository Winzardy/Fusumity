using Fusumity.Utility;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using TMPro;

namespace UI
{
	[InfoBox(
		"Automatically disables GameObject when provided with null string.",
		InfoMessageType.Info)]
	public class UILabelLayout : UIBaseLayout
	{
		public TMP_Text label;

		public void SetLabel(string text)
		{
			if (label == null)
				return;

			if (text.IsNullOrEmpty())
			{
				rectTransform.SetActive(false);
			}
			else
			{
				rectTransform.SetActive(true);
				label.text = text;
			}
		}

		protected override void Reset()
		{
			base.Reset();
			label = GetComponentInChildren<TMP_Text>();
		}
	}
}
