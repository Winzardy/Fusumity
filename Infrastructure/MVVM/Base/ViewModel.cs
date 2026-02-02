using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace Fusumity.MVVM
{
	public static class ViewModelUtility
	{
		public static void CreateOrUpdate<TViewModel, TModel>(ref TViewModel viewModel, TModel model)
			where TViewModel : ViewModel<TModel>, new()
		{
			viewModel ??= new TViewModel();
			viewModel.Update(model);
		}
	}

	public abstract class ViewModel<TModel> : IDisposable
	{
		protected TModel _model;

		[ShowInInspector] [HideLabel] public TModel Model { get => _model; set => Update(value); }

		public ViewModel()
		{
			Initialize();
		}

		public ViewModel(TModel model) : this()
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

		protected abstract void OnUpdated(TModel model);

		protected virtual void OnCleared(TModel model)
		{
		}

		protected virtual void OnDisposed()
		{
		}
	}
}
