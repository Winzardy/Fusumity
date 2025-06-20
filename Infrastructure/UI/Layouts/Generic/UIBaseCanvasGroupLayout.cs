using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	[RequireComponent(typeof(CanvasGroup))]
	public abstract class UIBaseCanvasGroupLayout : UIBaseLayout
	{
		[PropertySpace(10)]
		public CanvasGroup canvasGroup;

		protected override void Reset()
		{
			base.Reset();

			canvasGroup = GetComponent<CanvasGroup>();
		}
	}
}
