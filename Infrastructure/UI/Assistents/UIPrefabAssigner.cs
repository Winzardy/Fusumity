using AssetManagement;
using Cysharp.Threading.Tasks;
using Sapientia.Collections;
using Sapientia.Pooling;
using System;
using System.Collections.Generic;
using System.Threading;
using Fusumity.Utility;
using Sapientia.Utility;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// Отвечает за подгрузку префаба и инстанс его на указанный parent (<see cref="RectTransform"/>)
	/// </summary>
	public class UIPrefabAssigner : IDisposable
	{
		private ISpinner _spinner;

		// Чтобы не аллоцировать Dictionary для единичных случаев!
		private (RectTransform parent, PrefabAssignerHandle handle) _single;
		private HashMap<RectTransform, PrefabAssignerHandle> _parentToHandle;

		private bool _disposed;

		public UIPrefabAssigner()
		{
		}

		public UIPrefabAssigner(ISpinner spinner)
		{
			_spinner = spinner;
		}

		public void Dispose()
		{
			_disposed = true;

			_single.handle.Release();

			if (_parentToHandle.IsNullOrEmpty())
				return;

			foreach (ref var handle in _parentToHandle)
				handle.Release();

			StaticObjectPoolUtility.ReleaseAndSetNull(ref _parentToHandle);
		}

		public void TrySetPrefab(RectTransform parent, IAssetReferenceEntry<GameObject> prefabRef, Action callback = null,
			bool disableDuringLoad = true)
		{
			if (parent == null || prefabRef.IsEmptyOrInvalid())
				return;

			SetPrefab(parent, prefabRef, callback, disableDuringLoad);
		}

		public void SetPrefab(IEnumerable<RectTransform> parents, IAssetReferenceEntry<GameObject> prefabRef)
		{
			foreach (var parent in parents)
				SetPrefab(parent, prefabRef);
		}

		public void SetPrefab(RectTransform parent, IAssetReferenceEntry<GameObject> prefabRef, Action callback = null,
			bool disableDuringLoad = true)
		{
			if (disableDuringLoad)
			{
				parent.SetActive(false);
				callback += () => parent.SetActive(true);
			}

			if (_single.parent != null)
			{
				if (TryUpdateSingle(parent, prefabRef, callback))
					return;
			}
			else
			{
				_single = (parent, handle: new PrefabAssignerHandle(prefabRef));
				LoadAndPlaceAsync(parent, callback)
					.Forget();
				return;
			}

			_parentToHandle ??= HashMapPool<RectTransform, PrefabAssignerHandle>.Get();

			if (_parentToHandle.TryGetValue(parent, out var pair))
			{
				//Какой смысл если там и так такой ассет
				if (pair.prefabRef.Equals(prefabRef))
				{
					callback?.Invoke();
					return;
				}

				pair.Release();
			}

			_parentToHandle[parent] = new PrefabAssignerHandle(prefabRef);
			LoadAndPlaceAsync(parent, callback).Forget();
		}

		private bool TryUpdateSingle(RectTransform parent, IAssetReferenceEntry<GameObject> entry, Action callback = null)
		{
			if (_single.parent == parent)
			{
				//Какой смысл если там и так такой ассет
				if (_single.handle.prefabRef.Equals(entry))
				{
					callback?.Invoke();
					return true;
				}

				_single.handle.Release();
				_single.handle = new PrefabAssignerHandle(entry);
				LoadAndPlaceAsync(parent, callback)
					.Forget();
				return true;
			}

			return false;
		}

		public void SetSpinner(ISpinner spinner)
		{
			_spinner = spinner;
		}

		public void TryCancelOrClear(RectTransform parent)
		{
			if (_single.parent == parent)
			{
				_single.handle.Release();
				_single.parent = null;
				return;
			}

			if (_parentToHandle.IsNullOrEmpty())
				return;

			if (!_parentToHandle.TryGetValue(parent, out var handle))
				return;

			handle.Release();
			_parentToHandle.Remove(parent);
		}

		private async UniTaskVoid LoadAndPlaceAsync(RectTransform parent, Action callback = null)
		{
			GetHandle(parent).cts = new CancellationTokenSource();
			OnStartLoading();

			var prefab = await GetHandle(parent).prefabRef.LoadAsync(GetHandle(parent).cts.Token);

			if (GetHandle(parent).cts.IsCancellationRequested || _disposed || parent == null)
			{
				OnEndLoading();
				return;
			}

			GetHandle(parent)
				.Instantiate(prefab, parent);

			callback?.Invoke();
			OnEndLoading();

			void OnStartLoading() => _spinner?.Show(GetHandle(parent).cts);
			void OnEndLoading() => _spinner?.Hide(GetHandle(parent).cts);
		}

		private ref PrefabAssignerHandle GetHandle(RectTransform parent)
		{
			if (_single.parent == parent)
				return ref _single.handle;

			return ref _parentToHandle[parent];
		}
	}

	public struct PrefabAssignerHandle
	{
		public IAssetReferenceEntry<GameObject> prefabRef;
		public CancellationTokenSource cts;

		private GameObject _instance;

		public PrefabAssignerHandle(IAssetReferenceEntry<GameObject> prefabRef)
		{
			this.prefabRef = prefabRef;
			cts            = null;
			_instance      = null;
		}

		public void Release()
		{
			_instance.DestroySafe();

			prefabRef?.Release();
			prefabRef = null;

			AsyncUtility.TriggerAndSetNull(ref cts);
		}

		public void Instantiate(GameObject prefab, RectTransform parent)
		{
			_instance = UnityObjectsFactory.Create(prefab, parent);
		}
	}
}
