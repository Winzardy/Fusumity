using System;
using Fusumity.MVVM;
using Sapientia;

namespace UI.Screens
{
	public abstract class UIViewBoundScreen<TViewModel, TView, TLayout> : UIScreen<TLayout, TViewModel>, IBoundedView
		where TView : class, IView<TViewModel>
		where TLayout : UIBaseScreenLayout
	{
		/// <summary>
		/// Dispose current view model every time the view is updated
		/// and upon window opening/closing.
		/// <br></br>
		/// Don't do it if you're using view model as cache elsewhere.
		/// </summary>
		public virtual bool AutoDisposeViewModel { get => true; }

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
			TryСlearViewAndAutoDisposeViewModel();

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

			TryСlearViewAndAutoDisposeViewModel();
			_view?.Update(viewModel);

			OnViewUpdated();
		}

		protected override sealed void OnShow(in TViewModel viewModel)
		{
			TryСlearViewAndAutoDisposeViewModel();
			_view.Update(viewModel);

			_view.OnShow();
			OnViewShow();
		}

		protected override void OnHide(in TViewModel _)
		{
			_view.OnHide();
			OnViewHide();
		}

		protected override void OnEndedOpening()
		{
			_view.OnShown();
			OnViewShown();
		}

		protected override void OnEndedClosing()
		{
			_view.OnHidden();
			OnViewHidden();
		}

		protected override void OnReset(bool deactivate)
		{
			TryСlearViewAndAutoDisposeViewModel(true);

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

		protected virtual void OnViewShow()
		{
		}

		protected virtual void OnViewShown()
		{
		}

		protected virtual void OnViewHide()
		{
		}

		protected virtual void OnViewHidden()
		{
		}

		protected virtual void OnViewUpdated()
		{
		}

		protected void TryСlearViewAndAutoDisposeViewModel(bool dispose = false)
		{
			_view?.ClearViewModel(AutoDisposeViewModel && dispose);
		}
	}
}
