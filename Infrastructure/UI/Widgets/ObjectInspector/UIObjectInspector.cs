using System.Threading;
using AssetManagement;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Sapientia.Collections;
using UnityEngine;

namespace UI
{
	public class DefaultObjectInspectorViewModel : IObjectInspectorViewModel<GameObject>
	{
		public IAssetReferenceEntry reference { get; set; }
		public GameObject prefab { get; set; }
		public ISpinner spinner { get; set; }
		public UITextureRendererArgs? render { get; set; }
		public UIObjectInspectorSettings Settings { get; set; }
	}

	public class UIObjectInspector : UIBaseObjectInspector<GameObject, DefaultObjectInspectorViewModel>
	{
		protected override async UniTask<GameObject> CreateAsync(GameObject prefab, CancellationToken cancellationToken)
		{
			var operation = Object.InstantiateAsync(prefab);
			_gameObject = (await operation)[0];

			if (cancellationToken.IsCancellationRequested)
			{
				_gameObject.Destroy();
				cancellationToken.ThrowIfCancellationRequested();
			}

			if (Value.Settings.useCustomRotation)
				_gameObject.transform.localRotation = Quaternion.Euler(Value.Settings.rotation);

			return _gameObject;
		}

		protected override void OnShow(GameObject gameObject)
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

			_textureRenderer.FocusTo(gameObject);
		}

		protected override void OnClearPrefab()
		{
			Object.Destroy(target);
			_gameObject = null;
		}

		protected override async UniTask<GameObject> LoadAsync(IAssetReferenceEntry reference,
			CancellationToken cancellationToken)
			=> await reference.LoadAsync<GameObject>(cancellationToken);
	}
}
