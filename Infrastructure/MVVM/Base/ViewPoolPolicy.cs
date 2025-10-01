using Fusumity.Utility;
using Sapientia.Collections;
using Sapientia.Pooling;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fusumity.MVVM
{
	public abstract class BaseViewPoolPolicy<TView> : IObjectPoolPolicy<TView>, IDisposable where TView : IView
	{
		protected readonly Transform _poolRoot;
		protected readonly Action<TView> _destructor;

		/// <summary>
		/// Pool-wise instances cache, to be able to perform collective disposal,
		/// without having to keep track elsewhere.
		/// </summary>
		protected List<TView> _instances;

		public BaseViewPoolPolicy(Action<TView> destructor = null) : this($"[Pool Holder] {typeof(TView).Name}s", destructor)
		{
		}

		public BaseViewPoolPolicy(string rootName, Action<TView> destructor = null)
		{
			_destructor = destructor;

			var gameObject = new GameObject(rootName);
			gameObject.SetActive(false);

			_poolRoot = gameObject.transform;
		}

		public BaseViewPoolPolicy(Transform poolRoot, Action<TView> destructor = null)
		{
			_destructor = destructor;
			_poolRoot = poolRoot;
		}

		public abstract TView Create();
		public void OnGet(TView widget)
		{
		}

		public virtual void OnRelease(TView view)
		{
			if (view.GameObject.transform.parent != _poolRoot)
			{
				view.GameObject.transform.SetParent(_poolRoot, false);
			}

			view.Reset();
		}

		public virtual void OnDispose(TView view)
		{
			_instances?.Remove(view);
			_destructor?.Invoke(view);

			if (view is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}

		public void Dispose()
		{
			if (_instances.IsNullOrEmpty())
				return;

			for (int i = _instances.Count; i-- > 0;)
			{
				OnDispose(_instances[i]);
			}
		}
	}

	public class ViewPoolPolicy<TView> : BaseViewPoolPolicy<TView> where TView : IView
	{
		private Func<TView> _activator;

		public ViewPoolPolicy(
			Func<TView> activator,
			Action<TView> destructor = null) : this($"[Pool Holder] {typeof(TView).Name}s", activator, destructor)
		{
		}

		public ViewPoolPolicy(string rootName,
			Func<TView> activator,
			Action<TView> destructor = null) : base(rootName, destructor)
		{
			_activator = activator;
		}

		public ViewPoolPolicy(Transform poolRoot,
			Func<TView> activator,
			Action<TView> destructor = null) : base(poolRoot, destructor)
		{
			_activator = activator;
		}

		public sealed override TView Create()
		{
			var instance = _activator.Invoke();
			(_instances ??= ListPool<TView>.Get()).Add(instance);

			return instance;
		}
	}

	public class ViewPoolPolicy<TView, TLayout> : BaseViewPoolPolicy<TView>
		where TView : IView
		where TLayout : MonoBehaviour
	{
		private readonly TLayout _prefab;
		private readonly Func<TLayout, TView> _activator;

		private string _viewName;

		public ViewPoolPolicy(TLayout prefab,
			Func<TLayout, TView> activator,
			Action<TView> destructor = null) : this(prefab, $"[Pool Holder] {typeof(TView).Name}s", activator, destructor)
		{
		}

		public ViewPoolPolicy(TLayout prefab, string rootName,
			Func<TLayout, TView> activator,
			Action<TView> destructor = null) : base(rootName, destructor)
		{
			_prefab = prefab;
			_activator = activator;
			_viewName = typeof(TView).Name;
		}

		public ViewPoolPolicy(TLayout prefab, Transform poolRoot,
			Func<TLayout, TView> activator,
			Action<TView> destructor = null) : base(poolRoot, destructor)
		{
			_prefab = prefab;
			_activator = activator;
			_viewName = typeof(TView).Name;
		}

		public sealed override TView Create()
		{
			var layout = UnityObjectsFactory.Create(_prefab, _poolRoot, name: $"[ {_viewName} ]");

			var instance = _activator.Invoke(layout);
			(_instances ??= ListPool<TView>.Get()).Add(instance);

			return instance;
		}
	}
}
