using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	[RequireComponent(typeof(CanvasRenderer))]
	public class EmptyGraphic : Graphic
	{
		public override void SetMaterialDirty() { }

		public override void SetVerticesDirty() { }
	}
}
