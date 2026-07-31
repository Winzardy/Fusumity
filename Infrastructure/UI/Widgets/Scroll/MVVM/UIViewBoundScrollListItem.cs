using System;
using Fusumity.MVVM;

namespace UI.Scroll
{
	public abstract class UIViewBoundScrollListItem<TSelf, TView, TLayout, TViewModel> : UIViewBoundScrollListItem<TView, TLayout, TViewModel>
		where TSelf : UIViewBoundScrollListItem<TSelf, TView, TLayout, TViewModel>
		where TView : IView<TViewModel>
		where TLayout : UIViewBoundScrollItemLayout
		where TViewModel : class
	{
		public event Action<TSelf> Clicked;

		protected override void OnLayoutInstalled()
		{
			base.OnLayoutInstalled();

			_layout.button.Subscribe(HandleClicked);
		}

		protected override void OnLayoutCleared()
		{
			base.OnLayoutCleared();
			_layout.button.Unsubscribe(HandleClicked);
		}

		private void HandleClicked() => Clicked?.Invoke(this as TSelf);
	}

	public abstract class UIViewBoundScrollListItem<TView, TLayout, TViewModel> : UIScrollListItemC<TLayout, TViewModel>
		where TView : IView<TViewModel>
		where TLayout : UIViewBoundScrollItemLayout
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
