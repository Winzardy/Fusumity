using UnityEngine;

namespace UI
{
	public class FaceTarget : ReactiveBehaviour
	{
		public enum FacingMode
		{
			UpToTarget,
			UpFromTarget,
			RightToTarget,
			RightFromTarget
		}

		[SerializeField]
		private Transform _target;

		[SerializeField]
		private FacingMode _mode;

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
			if (_target == null)
				return;
			var direction = (_target.position - transform.position).normalized;

			switch (_mode)
			{
				case FacingMode.UpToTarget:
					transform.up = direction;
					break;
				case FacingMode.UpFromTarget:
					transform.up = -direction;
					break;
				case FacingMode.RightToTarget:
					transform.right = direction;
					break;
				case FacingMode.RightFromTarget:
					transform.right = -direction;
					break;
			}
		}
	}
}
