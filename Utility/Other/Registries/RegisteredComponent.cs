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

			OnStart();
		}

		private void OnDestroy()
		{
			if (!UnityLifecycle.ApplicationQuitting)
			{
				_registry?.Unregister(this as T);
				OnDispose();
			}
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
