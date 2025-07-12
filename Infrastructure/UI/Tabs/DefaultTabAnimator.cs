using DG.Tweening;
using UnityEngine;

namespace UI.Tabs
{
	public class DefaultTabAnimator : BaseWidgetAnimator<UIBaseTabLayout, ITab>
	{
		private const float OPENING_TIME = 0.4f;
		private const float CLOSING_TIME = 0.5f;

		private readonly Ease _easeIn = Ease.Linear;
		private readonly Ease _easeOut = Ease.OutSine;

		protected override void OnCreateOpeningSequence(ref Sequence sequence)
		{
			sequence ??= DOTween.Sequence();

			var tabIndex = _widget.IndexFromParent();
			var prevIndex = _widget.Group.PrevTabIndex;

			if (tabIndex > prevIndex)
			{
				_layout.canvasGroup.alpha = 1;

				sequence.Join(_layout.container
				   .DOAnchorPos(Vector2.zero, OPENING_TIME)
				   .From(new Vector2(_layout.container.rect.width, 0))
				   .SetEase(_easeIn));
			}
			else if (tabIndex < prevIndex)
			{
				_layout.canvasGroup.alpha = 1;

				sequence.Join(_layout.container
				   .DOAnchorPos(Vector2.zero, OPENING_TIME)
				   .From(new Vector2(-_layout.container.rect.width, 0))
				   .SetEase(_easeIn));
			}
			else if (tabIndex == prevIndex)
			{
				if (_layout.canvasGroup)
				{
					sequence.Join(_layout.canvasGroup
					   .DOFade(1f, OPENING_TIME * 0.7f)
					   .From(0f));
				}

				var alignment = _widget.Group.GetAlignmentGroupType();
				if (alignment != AlignmentGroupType.None)
				{
					sequence.Join(_layout.container
					   .DOAnchorPosY(0f, OPENING_TIME * 0.7f)
					   .From(GetPosByAlignment())
					   .SetEase(_easeIn));
				}

				Vector2 GetPosByAlignment()
				{
					var offset = 100;
					return alignment switch
					{
						AlignmentGroupType.Top => new Vector2(0, offset),
						AlignmentGroupType.Bottom => new Vector2(0, -offset),
						AlignmentGroupType.Left => new Vector2(offset, 0),
						AlignmentGroupType.Right => new Vector2(-offset, 0),
						_ => Vector2.zero
					};
				}
			}
		}

		protected override void OnCreateClosingSequence(ref Sequence sequence)
		{
			sequence ??= DOTween.Sequence();

			var tabIndex = _widget.IndexFromParent();
			var nextIndex = _widget.Group.NextTabIndex;

			if (tabIndex > nextIndex)
			{
				_layout.canvasGroup.alpha = 1;

				sequence.Join(_layout.container
				   .DOAnchorPos(new Vector2(_layout.container.rect.width, 0), CLOSING_TIME)
				   .From(Vector2.zero)
				   .SetEase(_easeOut));
			}
			else if (tabIndex < nextIndex)
			{
				_layout.canvasGroup.alpha = 1;

				sequence.Join(_layout.container
				   .DOAnchorPos(new Vector2(-_layout.container.rect.width, 0), CLOSING_TIME)
				   .From(Vector2.zero)
				   .SetEase(_easeOut));
			}
			else if (tabIndex == nextIndex)
			{
				if (_layout.canvasGroup)
				{
					sequence.Join(_layout.canvasGroup
					   .DOFade(0f, CLOSING_TIME * 0.49f)
					   .From(1f));
				}

				var alignment = _widget.Group.GetAlignmentGroupType();
				if (alignment != AlignmentGroupType.None)
				{
					sequence.Join(_layout.container
					   .DOAnchorPos(GetPosByAlignment(), CLOSING_TIME * .7f)
					   .From(Vector2.zero)
					   .SetEase(_easeOut));
				}

				Vector2 GetPosByAlignment()
				{
					var offset = 200;
					return alignment switch
					{
						AlignmentGroupType.Top => new Vector2(0, offset),
						AlignmentGroupType.Bottom => new Vector2(0, -offset),
						AlignmentGroupType.Left => new Vector2(offset, 0),
						AlignmentGroupType.Right => new Vector2(-offset, 0),
						_ => Vector2.zero
					};
				}
			}
		}
	}
}
