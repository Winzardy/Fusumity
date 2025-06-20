using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	[RequireComponent(typeof(Image))]
	public class ImageSpriteButtonTransition : ButtonTransition
	{
		[SerializeField]
		private Image _target;

		[SerializeField]
		private SpriteState _state;

		public override void DoStateTransition(int state, bool instant)
		{
			if (!_target)
				return;

			var sprite = state switch
			{
				ButtonTransitionType.NORMAL => null,
				ButtonTransitionType.HIGHLIGHTED => _state.highlightedSprite,
				ButtonTransitionType.PRESSED => _state.pressedSprite,
				ButtonTransitionType.SELECTED => _state.selectedSprite,
				ButtonTransitionType.DISABLED => _state.disabledSprite,
				_ => null
			};

			_target.overrideSprite = sprite;
		}

		public void SetState(SpriteState state) => _state = state;

		private void Reset() => TryGetComponent(out _target);
	}
}
