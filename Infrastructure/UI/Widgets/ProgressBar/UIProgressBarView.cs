using Fusumity.MVVM.UI;
using System;
using System.Diagnostics.CodeAnalysis;

namespace UI
{
	public class UIProgressBarView : UIView<IProgressBarViewModel, UIProgressBarLayout>
	{
		private UIProgressBar _widget;

		public UIProgressBarView(UIProgressBarLayout layout) : base(layout)
		{
			AddDisposable(_widget = new UIProgressBar());
			_widget.Initialize();

			_widget.SetupLayout(layout);
			_widget.SetActive(true);

			Reset();
		}

		protected override void OnUpdate(IProgressBarViewModel viewModel)
		{
			UpdateFilling();
			UpdateLabel();

			viewModel.ProgressChanged += HandleProgressChanged;
		}

		protected override void OnClear(IProgressBarViewModel viewModel)
		{
			viewModel.ProgressChanged -= HandleProgressChanged;
		}

		private void UpdateFilling()
		{
			_widget.Show(ViewModel.Progress);
		}

		private void UpdateLabel()
		{
			if (_layout.label != null)
			{
				_layout.label.text = ViewModel.Label;
			}
		}

		public override void Reset()
		{
			ClearViewModel();

			switch (_layout.type)
			{
				case UIProgressBarLayout.Type.Image:
					_layout.image.fillAmount = 1;
					break;

				case UIProgressBarLayout.Type.ScrollBar:
					_layout.scrollBar.size = 1;
					break;
			}

			if (_layout.label != null)
			{
				_layout.label.text = null;
			}
		}

		private void HandleProgressChanged()
		{
			UpdateFilling();
			UpdateLabel();
		}
	}

	public interface IProgressBarViewModel
	{
		/// <summary>
		/// 0...1
		/// </summary>
		float Progress { get; }

		[MaybeNull]
		string Label { get; }

		event Action ProgressChanged;
	}
}
