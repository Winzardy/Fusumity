using System.Threading;
using AssetManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Fusumity.Utility;
using Sapientia.Collections;

namespace UI
{
	public class UIObjectInspector<T> : UIBaseObjectInspector<T, UIObjectInspector<T>.Args>
		where T : Component
	{
		public struct Args : IObjectInspectorArgs<T>
		{
			public IAssetReferenceEntry reference { get; set; }
			public T prefab { get; set; }
			public ISpinner spinner { get; set; }
			public UITextureRendererArgs? render { get; set; }

			public UIObjectInspectorEntry entry { get; set; }
		}

		protected override async UniTask<T> CreateAsync(T prefab, CancellationToken cancellationToken)
		{
			var operation = Object.InstantiateAsync(prefab.gameObject);
			_gameObject = (await operation)[0];

			if (cancellationToken.IsCancellationRequested)
			{
				_gameObject.Destroy();
				cancellationToken.ThrowIfCancellationRequested();
			}

			if (_args.entry.useCustomRotation)
				_gameObject.transform.localRotation = Quaternion.Euler(_args.entry.rotation);

			//TODO: костылище...
			//Внутри персонажей есть канвас для игровой логики...
			var canvases = _gameObject.GetComponentsInChildren<Canvas>();
			foreach (var canvas in canvases)
				canvas.SetActive(false);

			return _gameObject.GetComponent<T>();
		}

		protected override void OnShow(T component)
		{
			Vector3? position = null;
			if (entry.useCustomPosition)
				position = entry.position;

			Vector3? scale = null;
			if (entry.useCustomScale)
				scale = entry.scale;

			_textureRenderer.FocusPoint.localPosition = position ?? Vector3.zero;
			_textureRenderer.FocusPoint.localScale = scale ?? Vector3.one;
			_textureRenderer.FocusPoint.localRotation = Quaternion.identity;

			_textureRenderer.FocusTo(component.gameObject);
		}

		protected override void OnClearPrefab()
		{
			Object.Destroy(target.gameObject);
			_gameObject = null;
		}

		protected override async UniTask<T> LoadAsync(IAssetReferenceEntry reference,
			CancellationToken cancellationToken)
			=> await reference.LoadComponentAsync<T>(cancellationToken);
	}
}
