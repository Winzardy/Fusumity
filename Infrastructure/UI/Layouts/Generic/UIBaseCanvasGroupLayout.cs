using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	[RequireComponent(typeof(CanvasGroup))]
	public abstract class UIBaseCanvasGroupLayout : UIBaseLayout
	{
		[PropertySpace(10)]
		public CanvasGroup canvasGroup;

		[BoxGroup("Close", showLabel: false)]
		[PropertyOrder(1)]
		[Tooltip("Optional: holding this button closes all windows and popups")]
		[CanBeNull]
		public UIHoldButtonLayout closeAllHold;

		protected override void Reset()
		{
			base.Reset();

			TryGetComponent(out canvasGroup);
		}
	}
}
