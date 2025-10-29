using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public class UIMarkerLayout : UIBaseLayout
	{
		[Space]
		public CanvasGroup canvasGroup;

		[Space]
		public RectTransform arrow;
		public RectTransform pivot;

		[Space]
		public StateSwitcher<bool> offscreenStateSwitcher;

		[Space]
		public UIBaseLayout nested;

		[TitleGroup("Animation"), ShowIf(nameof(canvasGroup), null)]
		public float showingDuration = 0.2f;

		[ShowIf(nameof(canvasGroup), null)]
		public float hidingDuration = 0.15f;
	}
}
