using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	[RequireComponent(typeof(Graphic))]
	public class GraphicColorButtonTransition : ButtonTransition
	{
		[SerializeField]
		private Graphic _target;

		[SerializeField]
		private ColorBlock _block;

		public override void DoStateTransition(int state, bool instant)
		{
			if (!_target)
				return;

			var tintColor = state switch
			{
				ButtonTransitionType.NORMAL => _block.normalColor,
				ButtonTransitionType.HIGHLIGHTED => _block.highlightedColor,
				ButtonTransitionType.PRESSED => _block.pressedColor,
				ButtonTransitionType.SELECTED => _block.selectedColor,
				ButtonTransitionType.DISABLED => _block.disabledColor,
				_ => Color.black
			};

			var targetColor = tintColor * _block.colorMultiplier;

			_target.CrossFadeColor(targetColor, instant ? 0f : _block.fadeDuration, true, true);
		}

		public void SetBlock(in ColorBlock block) => _block = block;

		[ContextMenu("Set Default Colors")]
		private void SetDefaultColorBlockForTintTransitions() => _block = ColorBlock.defaultColorBlock;

		private void Reset()
		{
			TryGetComponent(out _target);
			SetDefaultColorBlockForTintTransitions();
		}
	}
}
