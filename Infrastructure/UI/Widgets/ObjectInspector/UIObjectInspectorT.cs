using System.Threading;
using AssetManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Fusumity.Utility;

namespace UI
{
	public class DefaultObjectInspectorViewModel<T> : IObjectInspectorViewModel<T>
		where T : Component
	{
		public IAssetReferenceEntry Reference { get; set; }
		public T Prefab { get; set; }
		public ISpinner Spinner { get; set; }
		public UITextureRendererArgs? Render { get; set; }
		public UIObjectInspectorSettings Settings { get; set; }
	}

	public class UIObjectInspector<T> : UIBaseObjectInspector<T, DefaultObjectInspectorViewModel<T>>
		where T : Component
	{
		protected override async UniTask<T> CreateAsync(T prefab, CancellationToken cancellationToken)
		{
			var operation = Object.InstantiateAsync(prefab.gameObject);
			_gameObject = (await operation)[0];

			if (cancellationToken.IsCancellationRequested)
			{
				_gameObject.Destroy();
				cancellationToken.ThrowIfCancellationRequested();
			}

			if (_args.Settings.useCustomRotation)
				_gameObject.transform.localRotation = Quaternion.Euler(_args.Settings.rotation);

			return _gameObject.GetComponent<T>();
		}

		protected override void OnShow(T component)
		{
			Vector3? position = null;
			if (Settings.useCustomPosition)
				position = Settings.position;

			Vector3? scale = null;
			if (Settings.useCustomScale)
				scale = Settings.scale;

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
