using System;
using Fusumity.MVVM;
using Sapientia;

namespace UI.Popups
{
	public abstract class UIViewBoundPopup<TViewModel, TView, TLayout> : UIPopup<TLayout, TViewModel>, IBoundedView
		where TView : class, IView<TViewModel>
		where TLayout : UIBasePopupLayout
	{
		/// <summary>
		/// Dispose the owned view model when it is replaced
		/// or when the popup is reset
		/// <br></br>
		/// Don't do it if you're using view model as cache elsewhere.
		/// </summary>
		public abstract bool AutoDisposeViewModel { get; }

		protected TView _view;
		protected Action _onClose;

		public TViewModel ViewModel { get => _args; }

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
			ClearBoundViewModel();

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

			if (ReferenceEquals(_args, viewModel))
				return;

			UpdateArgs(in viewModel);
			BindCurrentViewModel();

			OnViewUpdated();
		}

		protected sealed override void OnShow(in TViewModel _)
		{
			BindCurrentViewModel();

			OnViewShow();
		}

		protected override void OnHide(in TViewModel _)
		{
			ClearBoundViewModel();
			OnViewHide();
		}

		protected override void OnEndedOpening()
		{
			OnViewShown();
		}

		protected override void OnEndedClosing()
		{
			OnViewHidden();
		}

		protected override void OnReset(bool deactivate)
		{
			UpdateArgs(default);

			var onClose = _onClose;
			_onClose = null;
			onClose?.Invoke();
			OnViewHidden();

			base.OnReset(deactivate);
		}

		protected override void OnDispose()
		{
			UpdateArgs(default);
			_onClose = null;

			base.OnDispose();
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

		protected override void UpdateArgs(in TViewModel viewModel)
		{
			if (ReferenceEquals(_args, viewModel))
				return;

			ClearBoundViewModel();
			AutoDisposeViewModelIfNeeded(_args);
			base.UpdateArgs(in viewModel);
		}

		private void BindCurrentViewModel()
		{
			_view?.Update(_args);
		}

		private void ClearBoundViewModel()
		{
			_view?.ClearViewModel();
		}

		private void AutoDisposeViewModelIfNeeded(TViewModel viewModel)
		{
			if (AutoDisposeViewModel && viewModel is IDisposable disposable)
				disposable.Dispose();
		}
	}
}
