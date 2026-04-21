using UnityEngine;

namespace UI
{
	public class OrderLayoutCanvasGroupAlphaReactor : OrderedLayoutElementReactor
	{
		[SerializeField]
		private CanvasGroup _target;

		[SerializeField]
		private float _multiplier = 1;

		public override void OnOrderChanged(int order)
		{
			_target.alpha = 1;

			for (int i = 0; i < order; i++)
			{
				_target.alpha *= _multiplier;
			}
		}
	}
}
