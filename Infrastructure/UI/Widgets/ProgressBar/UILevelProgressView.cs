using Fusumity.MVVM.UI;
using System;

namespace UI
{
	public class UILevelProgressView : UIView<ILevelProgressViewModel, UILevelProgressLayout>
	{
		private UIProgressBarView _progressBar;

		public UILevelProgressView(UILevelProgressLayout layout) : base(layout)
		{
			AddDisposable(_progressBar = new UIProgressBarView(layout.progressBar));
		}

		protected override void OnUpdate(ILevelProgressViewModel viewModel)
		{
			_progressBar.Update(viewModel.ProgressBar);
			viewModel.LevelChanged += HandleLevelChanged;
		}

		protected override void OnClear(ILevelProgressViewModel viewModel)
		{
			viewModel.LevelChanged -= HandleLevelChanged;
		}

		private void HandleLevelChanged()
		{
			_layout.level.text = ViewModel.Level.ToString();
		}
	}

	public interface ILevelProgressViewModel
	{
		int Level { get; }
		IProgressBarViewModel ProgressBar { get; }

		event Action LevelChanged;
	}
}
