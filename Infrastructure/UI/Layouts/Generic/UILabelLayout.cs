using Fusumity.Utility;
using Sapientia.Extensions;
using TMPro;

namespace UI
{
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
