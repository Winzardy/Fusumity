using System;
using Fusumity.MVVM;

namespace UI.Scroll
{
	public abstract class UIViewBoundScrollListItem<TView, TLayout, TViewModel> : UIScrollListItemC<TLayout, TViewModel>
		where TView : IView<TViewModel>
		where TLayout : UIScrollItemLayout
		where TViewModel : class

	{
		private TView _view;

		protected override void OnLayoutInstalled()
		{
			_view = CreateView();
		}

		protected override void OnLayoutCleared()
		{
			if (_view is IDisposable view)
				view.Dispose();
		}

		protected override void OnShow(TViewModel viewModel)
		{
			_view.Update(viewModel);
		}

		protected override void OnReset(bool deactivate)
		{
			_view.Reset();
		}

		protected abstract TView CreateView();
	}
}
