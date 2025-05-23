using TMPro;

namespace UI
{
	public class UILocalizedTextLayout : UILocalizedBaseLayout
	{
		public TMP_Text placeholder;

		public override TMP_Text Placeholder => placeholder;

		protected override void Reset()
		{
			base.Reset();
			placeholder = GetComponent<TMP_Text>();
		}
	}
}
