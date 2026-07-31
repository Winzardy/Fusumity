using UnityEngine;

namespace UI
{
	public class FaceWorldDown : ReactiveBehaviour
	{
		protected override void OnEnabled()
		{
			UpdateRotation();
		}

		protected override void OnUpdate()
		{
			UpdateRotation();
		}

		private void UpdateRotation()
		{
			transform.up = Vector3.up;
		}
	}
}
