using Fusumity.Utility;
using Sapientia.Extensions;
using System;
using UnityEngine;

namespace Fusumity.MVVM
{
	public interface IView
	{
		Type ViewModelType { get; }
		void Update(object viewModel);
		void Reset();

		GameObject GameObject { get; }

		void ClearViewModel(bool dispose = false);
	}

	public interface IView<TViewModel> : IView
	{
		void Update(TViewModel viewModel);
	}

	public abstract class View<TViewModel> : IView<TViewModel>, IDisposable
	{
		public abstract GameObject GameObject { get; }

		public Transform Transform { get { return GameObject != null ? GameObject.transform : null; } }
		public bool IsActive { get { return GameObject != null && GameObject.activeSelf; } }

		public Type ViewModelType { get { return typeof(TViewModel); } }
		public TViewModel ViewModel { get; private set; }

		public void Dispose()
		{
			ClearViewModel();
			OnDispose();

			_disposables?.Dispose();
			_bindings?.Dispose();
		}

		void IView.Update(object baseModel)
		{
			if (baseModel is TViewModel viewModel)
			{
				Update(viewModel);
			}
			else
			{
				var typeStr = baseModel?.GetType().ToString() ?? "null";
				Debug.LogError($"Provided invalid ViewModel type: [ {typeStr} ]");
			}
		}

		public void Update(TViewModel viewModel)
		{
			ClearViewModel();
			ViewModel = viewModel;

			if (viewModel != null)
			{
				OnUpdate(viewModel);
			}
			else
			{
				OnNullViewModel();
			}
		}

		public void ClearViewModel(bool dispose = false)
		{
			if (ViewModel != null)
			{
				_bindings?.Dispose();
				OnClear(ViewModel);

				if (dispose && ViewModel is IDisposable disposable)
					disposable.Dispose();

				ViewModel = default;
			}
		}

		/// <summary>
		/// View model updated.
		/// Provided argument will never be null.
		/// </summary>
		protected abstract void OnUpdate(TViewModel viewModel);

		/// <summary>
		/// View model cleared.
		/// Provided argument will never be null.
		/// </summary>
		protected virtual void OnClear(TViewModel viewModel)
		{
		}

		/// <summary>
		/// Null view model has been provided upon update.
		/// </summary>
		protected virtual void OnNullViewModel()
		{
		}

		protected virtual void OnDispose()
		{
		}

		public virtual void Destroy()
		{
			Dispose();

			if (GameObject != null)
				GameObject.Destroy();
		}

		public void Destroy(bool disposeViewModel)
		{
			ClearViewModel(disposeViewModel);
			Destroy();
		}

		public virtual void Reset()
		{
			ClearViewModel();
		}

		public static implicit operator bool(View<TViewModel> view) => view != null;

		#region Disposables

		private CompositeDisposable _disposables;

		protected void AddDisposable(IDisposable disposable)
		{
			var lazyCd = _disposables ??= new CompositeDisposable();
			lazyCd.AddDisposable(disposable);
		}

		protected void AddSubscription<T>(BaseSubscription<T> subscription) where T : Delegate
		{
			if (subscription != null && !subscription.IsDisposed)
				AddDisposable(subscription);
		}

		protected void Subscribe(IClickable clickable, Action handler)
		{
			AddSubscription(new ClickableSubscription(clickable, handler));
		}

		protected void Subscribe<T>(IClickable<T> clickable, Action<T> handler)
		{
			AddSubscription(new ClickableSubscription<T>(clickable, handler));
		}

		#endregion Disposables

		#region Bindings

		private CompositeDisposable _bindings;

		/// <summary>
		/// Bindings are disposed of upon each view model clearing.
		/// </summary>
		protected void Bind<T>(IBinding<T> binding, Action<T> handler)
		{
			var lazyCd = _bindings ??= new CompositeDisposable();
			lazyCd.AddDisposable(new BindingSubscription<T>(binding, handler));
		}

		#endregion Bindings
	}

	public abstract class View<TViewModel, TLayout> : View<TViewModel>
		where TLayout : MonoBehaviour
	{
		protected TLayout _layout;

		public override GameObject GameObject { get { return _layout.gameObject; } }

		protected View(TLayout layout)
		{
			_layout = layout;
		}

		public virtual void SetActive(bool active)
		{
			if (_layout != null)
			{
				_layout.SetActive(active);
			}

			OnSetActive(active);
		}

		protected virtual void OnSetActive(bool active)
		{
		}
	}
}
