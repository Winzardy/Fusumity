using Fusumity.MVVM.UI;

namespace UI.Popups
{
	public abstract class ViewBoundPopup<TViewModel, TView, TLayout> : UIPopup<TLayout, TViewModel>
		where TView : UIView<TViewModel, TLayout>
		where TLayout : UIBasePopupLayout
	{
		/// <summary>
		/// Dispose current view model every time the view is updated
		/// and upon window opening/closing.
		/// </summary>
		public abstract bool AutoDisposeViewModel { get; }

		protected TView _view;

		protected abstract TView CreateView(TLayout layout);

		protected sealed override void OnLayoutInstalled()
		{
			_view = CreateView(_layout);
			OnViewCreated();
		}

		protected sealed override void BeforeDispose()
		{
			if (_view == null)
				return;

			BeforeViewDisposed();
			TryAutoDisposeViewModel();

			_view.Dispose();
			OnViewDisposed();
		}

		/// <summary>
		/// Update currently open view. Is ignored if window is closed.
		/// Call that manually where you need it.
		/// </summary>
		public void TryUpdateView(TViewModel viewModel)
		{
			if (!Active)
				return;

			TryAutoDisposeViewModel();
			_view?.Update(viewModel);

			OnViewUpdated();
		}

		protected sealed override void OnShow(ref TViewModel viewModel)
		{
			TryAutoDisposeViewModel();
			_view?.Update(viewModel);
			OnViewShown();
		}

		protected sealed override void OnEndedClosing()
		{
			TryAutoDisposeViewModel();
			OnViewHidden();
		}

		protected virtual void OnViewCreated()
		{
		}
		protected virtual void BeforeViewDisposed()
		{
		}
		protected virtual void OnViewDisposed()
		{
		}
		protected virtual void OnViewShown()
		{
		}
		protected virtual void OnViewHidden()
		{
		}
		protected virtual void OnViewUpdated()
		{
		}

		protected void TryAutoDisposeViewModel()
		{
			if (AutoDisposeViewModel)
			{
				_view?.ClearViewModel(true);
			}
		}
	}
}
