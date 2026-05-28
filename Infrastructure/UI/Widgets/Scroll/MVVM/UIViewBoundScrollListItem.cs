using System;
using Fusumity.MVVM;

namespace UI.Scroll
{
	public abstract class UIViewBoundScrollListItem<TView, TLayout, TViewModel> : UIScrollListItemC<TLayout, TViewModel>
		where TView : IView<TViewModel>
		where TLayout : UIScrollItemLayout
		where TViewModel : class
	{
		public TView View { get; private set; }

		protected override void OnLayoutInstalled()
		{
			View = CreateView();
		}

		protected override void OnLayoutCleared()
		{
			if (View is IDisposable view)
				view.Dispose();
		}

		protected override void OnShow(TViewModel viewModel)
		{
			View.Update(viewModel);
		}

		protected override void OnReset(bool deactivate)
		{
			View.Reset();
		}

		protected abstract TView CreateView();
	}
}
