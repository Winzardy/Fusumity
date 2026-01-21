using Sapientia.Pooling;
using System;
using UnityEngine;

namespace Fusumity.MVVM
{
	public interface IViewPool : IDisposable
	{
		IView Get();
		void Release(IView obj);
	}

	public class ViewPool<TView> : OrderedPool<TView>, IViewPool where TView : class, IView
	{
		public ViewPool(IObjectPoolPolicy<TView> policy) : base(policy)
		{
		}

		public ViewPool(Func<TView> activator, Action<TView> destructor = null) :
			base(new ViewPoolPolicy<TView>(activator, destructor))
		{
		}

		public ViewPool(Transform root, Func<TView> activator, Action<TView> destructor = null) :
			base(new ViewPoolPolicy<TView>(root, activator, destructor))
		{
		}

		IView IViewPool.Get()
		{
			return Get();
		}

		void IViewPool.Release(IView view)
		{
			Release((TView)view);
		}
	}

	public class ViewPool<TView, TLayout> : ViewPool<TView>
		where TView : class, IView
		where TLayout : MonoBehaviour
	{
		public ViewPool(IObjectPoolPolicy<TView> policy) : base(policy)
		{
		}

		public ViewPool(TLayout prefab, Func<TLayout, TView> activator, Action<TView> destructor = null) :
			base(new ViewPoolPolicy<TView, TLayout>(prefab, activator, destructor))
		{
		}

		public ViewPool(TLayout prefab, Transform root,
			Func<TLayout, TView> activator,
			Action<TView> destructor = null) :
			base(new ViewPoolPolicy<TView, TLayout>(prefab, root, activator, destructor))
		{
		}
	}

	public class ViewPool<TViewModel, TView, TLayout> : ViewPool<TView, TLayout>
		where TView : class, IView
		where TLayout : MonoBehaviour
	{
		public ViewPool(IObjectPoolPolicy<TView> policy) : base(policy)
		{
		}

		public ViewPool(TLayout prefab, Func<TLayout, TView> activator, Action<TView> destructor = null) :
			base(new ViewPoolPolicy<TView, TLayout>(prefab, activator, destructor))
		{
		}

		public ViewPool(TLayout prefab, Transform root,
			Func<TLayout, TView> activator,
			Action<TView> destructor = null) :
			base(new ViewPoolPolicy<TView, TLayout>(prefab, root, activator, destructor))
		{
		}

		public TView Get(TViewModel viewModel)
		{
			var view = Get();
			view.Update(viewModel);

			return view;
		}
	}
}
