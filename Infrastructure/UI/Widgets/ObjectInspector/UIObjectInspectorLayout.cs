using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIObjectInspectorLayout : UIBaseLayout
	{
		public RawImage image;
		public UISpinnerLayout spinner;

		[Space]
		public RectTransform interactionRect;

		protected override void Reset()
		{
			base.Reset();

			if (interactionRect == null)
				interactionRect = rectTransform;
		}
	}
}
