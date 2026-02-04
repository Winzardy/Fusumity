using System;
using System.Collections.Generic;
using Fusumity.Reactive;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Utility
{
	using UnityObject = UnityEngine.Object;

	public static class ComponentUtility
	{
		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
		{
			var component = gameObject.GetComponent<T>();
			if (component == null)
				component = gameObject.AddComponent<T>();
			return component;
		}

		public static bool HasComponentInChildren<T>(this GameObject gameObject) where T : Component
		{
			var component = gameObject.GetComponentInChildren<T>();
			return component != null;
		}

		public static bool HasComponentInChildren(this GameObject gameObject, Type type)
		{
			var component = gameObject.GetComponentInChildren(type);
			return component != null;
		}

		public static void DisableComponentsInChildren(this GameObject gameObject, Type type)
		{
			var components = gameObject.GetComponentsInChildren(type);
			foreach (MonoBehaviour component in components)
			{
				component.enabled = false;
			}
		}

		public static bool DestroyComponentsInChildren(this GameObject gameObject, Type type, bool allowDestroyingAssets = false)
		{
			var components = gameObject.GetComponentsInChildren(type);
			foreach (var component in components)
			{
				UnityObject.DestroyImmediate(component, allowDestroyingAssets);
			}

			return components.Length > 0;
		}

		public static bool DestroyComponentsInChildren<T>(this GameObject gameObject, bool allowDestroyingAssets = false)
			where T : Component
		{
			var components = gameObject.GetComponentsInChildren<T>();
			foreach (var component in components)
			{
				UnityObject.DestroyImmediate(component, allowDestroyingAssets);
			}

			return components.Length > 0;
		}

		public static bool IsActive<T>(this T component) where T : Component
			=> component.gameObject.IsActive();

		public static void SetActive<T>(this T component, bool active)
			where T : Component
		{
			component.gameObject.SetActive(active);
		}

		public static void SetActiveSafe<T>(this T component, bool active)
			where T : Component
		{
			if(!component || !component.gameObject)
				return;

			component.gameObject.SetActive(active);
		}

		public static void SetEnableSafe<T>(this T component, bool enable)
			where T : Behaviour
		{
			if (component)
				SetEnable(component, enable);
		}

		public static void SetEnable<T>(this T component, bool enable)
			where T : Behaviour
		{
			component.enabled = enable;
		}

		public static void SetActive<T>(this IEnumerable<T> components, bool active) where T : Component
		{
			foreach (var component in components)
			{
				component.SetActive(active);
			}
		}

		public static void DontDestroyOnLoad(this GameObject gameObject)
		{
			UnityObject.DontDestroyOnLoad(gameObject);
		}

		public static void DontDestroyOnLoad<T>(this T component)
			where T : Component
		{
			UnityObject.DontDestroyOnLoad(component.gameObject);
		}

		public static T AddComponent<T>(this Component component)
			where T : Component
		{
			return component.gameObject.AddComponent<T>();
		}

		/// <summary>
		/// Уничтожает GameObject!
		/// </summary>
		public static void DestroyGameObject<T>(this T component)
			where T : Component
		{
			UnityObject.Destroy(component.gameObject);
		}

		/// <summary>
		/// Уничтожает GameObject!
		/// </summary>
		public static void DestroyGameObjectSafe<T>(this T component)
			where T : Component
		{
			if (component)
				component.DestroyGameObject();
		}

		public static void DestroySafe(this UnityObject obj)
		{
			if (obj)
				obj.Destroy();
		}

		public static void Destroy<T>(this T component) where T : Component
		{
			UnityObject.Destroy(component);
		}

		public static void DestroyComponentSafe<T>(this T component)
			where T : Component
		{
			if (component)
				UnityObject.Destroy(component);
		}

		/// <summary>
		/// Уничтожает GameObject!
		/// </summary>
		public static void LateDestroySafe<T>(this T component)
			where T : Component
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				EditorApplication.delayCall += OnDelayCall;

				void OnDelayCall()
				{
					EditorApplication.delayCall -= OnDelayCall;

					if (component)
						UnityObject.DestroyImmediate(component.gameObject, true);
				}

				return;
			}
#endif
			UnityLifecycle.LateUpdateEvent.Subscribe(DestroyInternal);

			void DestroyInternal()
			{
				UnityLifecycle.LateUpdateEvent.UnSubscribe(DestroyInternal);

				UnityObject.Destroy(component.gameObject);
			}
		}

		public static void LateDestroyComponentSafe<T>(this T component, Func<bool> cancel = null,
			Action callback = null)
			where T : Component
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				EditorApplication.delayCall += OnDelayCall;

				void OnDelayCall()
				{
					EditorApplication.delayCall -= OnDelayCall;

					if (cancel != null && cancel())
						return;

					if (component)
						UnityObject.DestroyImmediate(component, true);

					callback?.Invoke();
				}

				return;
			}
#endif
			UnityLifecycle.LateUpdateEvent.Subscribe(DestroyInternal);

			void DestroyInternal()
			{
				UnityLifecycle.LateUpdateEvent.UnSubscribe(DestroyInternal);

				if (cancel != null && cancel())
					return;

				UnityObject.Destroy(component);

				callback?.Invoke();
			}
		}

		public static void LateDestroyComponentSafe<T>(this T component, Component removingComponentsBefore,
			Func<bool> cancel = null, Action callback = null)
			where T : Component
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				EditorApplication.delayCall += OnDelayCall;

				void OnDelayCall()
				{
					EditorApplication.delayCall -= OnDelayCall;

					if (cancel != null && cancel())
						return;

					if (removingComponentsBefore)
						UnityObject.DestroyImmediate(removingComponentsBefore, true);

					if (component)
						UnityObject.DestroyImmediate(component, true);

					callback?.Invoke();
				}

				return;
			}
#endif
			UnityLifecycle.LateUpdateEvent.Subscribe(DestroyInternal);

			void DestroyInternal()
			{
				UnityLifecycle.LateUpdateEvent.UnSubscribe(DestroyInternal);

				if (cancel != null && cancel())
					return;

				if (removingComponentsBefore)
					UnityObject.Destroy(removingComponentsBefore);

				UnityObject.Destroy(component);

				callback?.Invoke();
			}
		}

		public static void LateDestroyComponentSafe<T>(this T component, Component[] removingComponentsBefore)
			where T : Component
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				EditorApplication.delayCall += OnDelayCall;

				void OnDelayCall()
				{
					EditorApplication.delayCall -= OnDelayCall;

					for (int i = 0; i < removingComponentsBefore.Length; i++)
					{
						if (removingComponentsBefore[i])
							UnityObject.DestroyImmediate(removingComponentsBefore[i], true);
					}

					if (component)
						UnityObject.DestroyImmediate(component, true);
				}

				return;
			}
#endif
			UnityLifecycle.LateUpdateEvent.Subscribe(DestroyInternal);

			void DestroyInternal()
			{
				UnityLifecycle.LateUpdateEvent.UnSubscribe(DestroyInternal);

				for (int i = 0; i < removingComponentsBefore.Length; i++)
				{
					if (removingComponentsBefore[i])
						UnityObject.Destroy(removingComponentsBefore[i]);
				}

				UnityObject.Destroy(component);
			}
		}

		public static T CreateGameObjectWithComponent<T>(this string name)
			where T : Component
		{
			var gameObject = new GameObject();

			if (!gameObject.TryGetComponent(out T component))
			{
				component = gameObject.AddComponent<T>();
			}

			return component;
		}

		public static void CreateGameObjectWithComponent<T>(this string name, out T component)
			where T : Component
		{
			var gameObject = new GameObject(name);

			if (!gameObject.TryGetComponent(out component))
			{
				component = gameObject.AddComponent<T>();
			}
		}

		public static GameObjectUtility.DisableGameObjectScope BeginDisableScope<T>(this T component)
			where T : Component
		{
			return component.gameObject.BeginDisableScope();
		}
	}
}
