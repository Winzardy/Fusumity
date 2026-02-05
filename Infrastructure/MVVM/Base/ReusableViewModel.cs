using JetBrains.Annotations;
using Sirenix.OdinInspector;
using System;

namespace Fusumity.MVVM
{
	public static class ReusableViewModelUtility
	{
		public static void CreateOrUpdate<TViewModel, TModel>(ref TViewModel viewModel, TModel model)
			where TViewModel : ReusableViewModel<TModel>, new()
		{
			viewModel ??= new TViewModel();
			viewModel.Update(model);
		}
	}

	/// <summary>
	/// Only inherit from it in specific scenarios, when you are absolutely sure you need it.
	/// </summary>
	public abstract class ReusableViewModel<TModel> : IDisposable
	{
		protected TModel _model;

		[ShowInInspector]
		[HideLabel]
		public TModel Model
		{
			get => _model;
#if UNITY_EDITOR
			set => Update(value);
#endif
		}

		public ReusableViewModel()
		{
			Initialize();
		}

		public ReusableViewModel(TModel model) : this()
		{
			Update(model);
		}

		public void Update(TModel model)
		{
			if (_model != null)
				Clear();

			_model = model;
			OnUpdated(model);
		}

		public void Clear()
		{
			if (_model != null)
				OnCleared(_model);

			_model = default;
		}

		public void Dispose()
		{
			Clear();
			OnDisposed();
		}

		private void Initialize() => OnInitialized();

		protected virtual void OnInitialized()
		{
		}

		protected abstract void OnUpdated([CanBeNull] TModel model);

		protected virtual void OnCleared([NotNull] TModel model)
		{
		}

		protected virtual void OnDisposed()
		{
		}
	}
}
