using Fusumity.MVVM.UI;
using JetBrains.Annotations;
using System;

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
			SetActive(true);
			UpdateFilling(true);

			if (_layout.label != null)
				_layout.label.Bind(viewModel.Label);

			viewModel.ProgressChanged += HandleProgressChanged;

			if (_layout.styleSwitcher != null)
			{
				UpdateStyle();
				if (viewModel is IStylizedProgressBarViewModel stylizedViewModel)
					stylizedViewModel.StyleChanged += UpdateStyle;
			}
		}

		protected override void OnClear(IProgressBarViewModel viewModel)
		{
			if (_layout.label != null)
				_layout.label.Unbind(viewModel.Label);

			viewModel.ProgressChanged -= HandleProgressChanged;

			if (_layout.styleSwitcher != null)
			{
				if (viewModel is IStylizedProgressBarViewModel stylizedViewModel)
					stylizedViewModel.StyleChanged -= UpdateStyle;
			}
		}

		protected override void OnNullViewModel()
		{
			SetActive(false);
		}

		private void UpdateFilling(bool immediate = false)
		{
			_widget.Show(ViewModel.Progress, immediate, false);
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
		}

		private void UpdateStyle()
		{
			var style = string.Empty;
			if (ViewModel is IStylizedProgressBarViewModel stylizedViewModel)
			{
				style = stylizedViewModel.Style;
			}

			_layout.styleSwitcher.Switch(style);
		}
	}

	public interface IProgressBarViewModel
	{
		/// <summary>
		/// 0...1
		/// </summary>
		float Progress { get; }

		[CanBeNull] ILabelViewModel Label { get; }

		event Action ProgressChanged;
	}

	public interface IStylizedProgressBarViewModel : IProgressBarViewModel
	{
		public string Style { get; }

		event Action StyleChanged;
	}
}
