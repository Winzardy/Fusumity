using Fusumity.Reactive;
using Sapientia.ServiceManagement;
using System;
using UnityEngine;

namespace Fusumity.Utility
{
	/// <summary>
	/// Allows component to be registered
	/// in <see cref="UnityObjectsRegistry"/> automatically.
	/// <br></br>
	/// Provide implementation class as generic argument
	/// to specify registered type.
	/// </summary>
	public abstract class RegisteredComponent<T> : MonoBehaviour where T : Component
	{
		private UnityObjectsRegistry _registry;
		private bool _isRegistered;

		private void Awake()
		{
			if (ServiceLocator.TryGet(out _registry))
			{
				OnInitialize();
			}
			else
			{
				Debug.LogWarning(
					$"Could not find UnityObjectsRegistry " +
					$"for RegisteredComponent: [ {gameObject.name} ]",
					gameObject);
			}
		}

		private void Start()
		{
			Register();
			OnStart();
		}

		private void OnDestroy()
		{
			if (!UnityLifecycle.ApplicationQuitting)
			{
				if (_isRegistered)
				{
					Unregister();
				}

				OnDispose();
			}
		}

		public void Register()
		{
			if (_isRegistered)
			{
				Debug.LogError(
					$"Trying to register component [ {gameObject.name} ] twice.",
					gameObject);

				return;
			}

			var component = this as T;
			if (component == null)
			{
				Debug.LogError(
					$"Registered component type [ {GetType().Name} ] " +
					$"does not implement [ {typeof(T).Name} ]");

				return;
			}

			if (registrationPredicate == null ||
				registrationPredicate.Invoke())
			{
				_registry?.Register(component);
			}
			else
			{
				_registry?.RegisterAfter(component, registrationPredicate);
			}

			_isRegistered = true;
		}

		public void Unregister()
		{
			if (!_isRegistered)
			{
				Debug.LogError(
					$"Trying to unregister not registered component [ {gameObject.name} ]",
					gameObject);

				return;
			}

			_registry?.Unregister(this as T);
			_isRegistered = false;
		}

		protected virtual void OnInitialize()
		{
		}

		protected virtual void OnDispose()
		{
		}

		protected virtual void OnStart()
		{
		}

		protected virtual Func<bool> registrationPredicate => null;
	}
}
