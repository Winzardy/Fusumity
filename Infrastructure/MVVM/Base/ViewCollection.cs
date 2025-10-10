using Fusumity.Utility;
using Sapientia.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusumity.MVVM
{
	public abstract class ViewCollection<TViewModel, TView, TViewLayout> : BaseViewCollection<TViewModel, TView, TViewLayout>
		where TView : class, IView
		where TViewLayout : MonoBehaviour
	{
		public ViewCollection(TViewLayout prefab, Transform root = null) : base(prefab, root)
		{
		}

		public ViewCollection(ViewCollectionLayout<TViewLayout> layout) : base(layout)
		{
		}

		protected override void Update(TView view, TViewModel viewModel) => view.Update(viewModel);
	}

	public abstract class BaseViewCollection<TViewModel, TView, TViewLayout> : IDisposable, IEnumerable<TView>
		where TView : class, IView
		where TViewLayout : MonoBehaviour
	{
		private ViewPool<TViewModel, TView, TViewLayout> _pool;
		private List<TView> _utilizedViews;

		public TView this[int index] { get { return _utilizedViews[index]; } }
		public int this[TView view] { get { return _utilizedViews.IndexOf(view); } }
		public int UtilizedCount { get { return _utilizedViews.Count; } }

		public BaseViewCollection(ViewCollectionLayout<TViewLayout> layout) : this(layout.prefab, layout.root)
		{
			layout.prefab.SetActive(false);
		}

		public BaseViewCollection(TViewLayout prefab, Transform root = null)
		{
			_pool = root != null ?
				new ViewPool<TViewModel, TView, TViewLayout>(prefab, root, CreateViewInstance, DisposeViewInstance) :
				new ViewPool<TViewModel, TView, TViewLayout>(prefab, CreateViewInstance, DisposeViewInstance);

			_utilizedViews = new List<TView>();
		}

		public void Dispose()
		{
			_pool.Dispose();
			OnDispose();
		}

		protected abstract TView CreateViewInstance(TViewLayout layout);
		protected abstract void Update(TView view, TViewModel viewModel);

		protected virtual void DisposeViewInstance(TView view)
		{
		}

		protected virtual void OnDispose()
		{
		}

		public void Update(IEnumerable<TViewModel> viewModels)
		{
			Reset();

			if (viewModels != null)
			{
				foreach (var viewModel in viewModels)
				{
					var view = Get();
					Update(view, viewModel);
				}
			}
		}

		public TView Get(TViewModel viewModel)
		{
			var view = Get();
			Update(view, viewModel);

			return view;
		}

		public TView Get()
		{
			var view = _pool.Get();
			_utilizedViews.Add(view);

			return view;
		}

		public IEnumerable<TView> Get(int amount)
		{
			for (int i = 0; i < amount; i++)
			{
				yield return Get();
			}
		}

		public IEnumerable<TView> Get(IEnumerable<TViewModel> viewModels)
		{
			foreach (var viewModel in viewModels)
			{
				yield return Get(viewModel);
			}
		}

		public void Reset()
		{
			if (_utilizedViews.Count == 0)
				return;

			for (int i = 0; i < _utilizedViews.Count; i++)
			{
				var view = _utilizedViews[i];
				_pool.Release(view);
			}

			_utilizedViews.Clear();
		}

		public void Sort(IComparer<TView> comparer)
		{
			_utilizedViews.Sort(comparer);
		}

		public IEnumerator<TView> GetEnumerator()
		{
			return _utilizedViews.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
