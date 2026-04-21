using UnityEngine;

namespace UI
{
	public class OrderedLayoutScaleReactor : OrderedLayoutElementReactor
	{
		[SerializeField]
		private Transform _target;

		[SerializeField]
		private Vector3 _scaleMultiplier = Vector3.one;

		public override void OnOrderChanged(int order)
		{
			if (_target == null)
				_target = transform;

			_target.localScale = Vector3.one;
			for (int i = 0; i < order; i++)
				_target.localScale = Vector3.Scale(_target.localScale, _scaleMultiplier);
		}

#if UNITY_EDITOR
		private void Reset()
		{
			_target = transform;
		}
#endif
	}
}
