using UnityEngine.UI;

namespace UI
{
	public class UILayerLayout : UICanvasLayout
	{
		public GraphicRaycaster raycaster;

		protected override void Reset()
		{
			base.Reset();

			raycaster = GetComponent<GraphicRaycaster>();
		}
#if UNITY_EDITOR
		public override bool HideDebugAnimationInEditor => true;
#endif
	}
}