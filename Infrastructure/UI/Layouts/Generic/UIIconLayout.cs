using UnityEngine.UI;

namespace UI
{
	public class UIIconLayout : UIBaseLayout
	{
		public Image icon;

		protected override void Reset()
		{
			base.Reset();
			icon = GetComponentInChildren<Image>();
		}
	}
}
