using UnityEngine;

namespace UI
{
	[RequireComponent(typeof(Canvas))]
	public abstract class UICanvasLayout : UIBaseLayout
	{
		public Canvas canvas;

		protected override void Reset()
		{
			base.Reset();

			canvas = GetComponent<Canvas>();
		}
	}
}