using System.Threading;
using AssetManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UI
{
	public class UIObjectInspector : UIBaseObjectInspector<GameObject, UIObjectInspector.Args>
	{
		public struct Args : IObjectInspectorArgs<GameObject>
		{
			public IAssetReferenceEntry reference { get; set; }
			public GameObject prefab { get; set; }
			public ISpinner spinner { get; set; }
			public UITextureRendererArgs? render { get; set; }
			public UIObjectInspectorEntry entry { get; set; }
		}

		protected override GameObject Create(GameObject prefab)
		{
			_gameObject = Object.Instantiate(prefab);

			if (_args.entry.useCustomRotation)
				_gameObject.transform.localRotation = Quaternion.Euler(_args.entry.rotation);

			return _gameObject;
		}

		protected override void OnShow(GameObject gameObject)
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
