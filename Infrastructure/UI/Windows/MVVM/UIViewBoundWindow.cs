using Fusumity.MVVM.UI;

namespace UI.Windows
{
	public abstract class UIViewBoundWindow<TViewModel, TView, TLayout> : UIWindow<TLayout, TViewModel>
			where TView : UIView<TViewModel, TLayout>
			where TLayout : UIBaseWindowLayout
	{
		/// <summary>
		/// Dispose current view model every time the view is updated
		/// and upon window opening/closing.
		/// </summary>
		public abstract bool AutoDisposeViewModel { get; }

		protected TView _view;

		protected abstract TView CreateView(TLayout layout);

		protected override sealed void OnLayoutInstalled()
		{
			_view = CreateView(_layout);
			OnViewCreated();
		}

		protected override sealed void BeforeDispose()
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

		protected override sealed void OnShow(ref TViewModel viewModel)
		{
			TryAutoDisposeViewModel();
			_view?.Update(viewModel);

			OnViewShown();
		}

		protected override void OnReset(bool deactivate)
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
