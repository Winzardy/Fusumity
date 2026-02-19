using Fusumity.MVVM.UI;
using Sapientia;
using System;
using Fusumity.MVVM;

namespace UI.Popups
{
	public abstract class UIViewBoundPopup<TViewModel, TView, TLayout> : UIPopup<TLayout, TViewModel>, IBoundedView
		where TView : class, IView<TViewModel>
		where TLayout : UIBasePopupLayout
	{
		/// <summary>
		/// Dispose current view model every time the view is updated
		/// and upon window opening/closing.
		/// <br></br>
		/// Don't do it if you're using view model as cache elsewhere.
		/// </summary>
		public abstract bool AutoDisposeViewModel { get; }

		protected TView _view;
		protected Action _onClose;

		protected abstract TView CreateView(TLayout layout);

		/// <summary>
		/// Action will be called upon window deactivation
		/// (it is hidden and removed from the queue).
		/// </summary>
		public void CallOnViewClosure(Action action)
		{
			_onClose = action;
		}

		protected sealed override void OnLayoutInstalled()
		{
			_view = CreateView(_layout);

			if (_view is ICloseRequestor closeRequestor)
			{
				closeRequestor.CloseRequested += RequestClose;
			}

			OnViewCreated();
		}

		protected override void OnLayoutCleared()
		{
			if (_view == null)
				return;

			if (_view is ICloseRequestor closeRequestor)
			{
				closeRequestor.CloseRequested -= RequestClose;
			}

			BeforeViewDisposed();
			TryAutoDisposeViewModel();

			if (_view is IDisposable disposable)
				disposable.Dispose();

			OnViewDisposed();

			_view = null;
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
			_view.Update(viewModel);
			OnViewShown();
		}

		protected override void OnReset(bool deactivate)
		{
			TryAutoDisposeViewModel();

			_onClose?.Invoke();
			OnViewHidden();

			base.OnReset(deactivate);
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
			_view?.ClearViewModel(AutoDisposeViewModel);
		}
	}
}
