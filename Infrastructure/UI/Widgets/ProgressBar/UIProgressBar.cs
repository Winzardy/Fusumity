namespace UI
{
	public class UIProgressBar : UIWidget<UIProgressBarLayout, float>
	{
		protected override void OnSetupDefaultAnimator() => SetAnimator<DefaultProgressBarAnimator>();
		protected override void OnShow(ref float _) => _animator.Play(WidgetAnimationType.PROGRESS_BAR, _immediate);
	}
}
