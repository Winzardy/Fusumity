using Fusumity.Utility;
using Sapientia.Pooling;
using UnityEngine;

namespace UI
{
	public class UICameraPool : ObjectPool<Camera>
	{
		private static ObjectPool<Camera> _pool = new UICameraPool();

		public static Camera Get() => _pool.Get();
		public static void Release(Camera camera) => _pool.Release(camera);

		public UICameraPool() : base(new Policy())
		{
		}

		private class Policy : IObjectPoolPolicy<Camera>
		{
			private const string NAME_FORMAT = "UI Camera #{0} (pooled)";

			private int _i;

			public Camera Create()
			{
				_i++;

				var name = string.Format(NAME_FORMAT, _i);
				var gameObject = new GameObject(name);

				//Не придумал лучше как расположить их далеко друг от друга (нужно чтобы в камеры не попадали другие объекты)
				gameObject.transform.position = new Vector3(100, _i * 100);

				var camera = gameObject.AddComponent<Camera>();
				gameObject.MoveToScene(UIFactory.scene);

				return camera;
			}

			public void OnGet(Camera camera)
			{
				camera.SetActive(true);
			}

			public void OnRelease(Camera camera)
			{
				camera.SetActive(false);
				camera.targetTexture = null;
			}

			public void OnDispose(Camera camera)
			{
				camera.DestroyGameObjectSafe();
			}
		}
	}
}
