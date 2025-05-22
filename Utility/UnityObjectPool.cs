using Sapientia.Pooling;
using UnityEngine;

namespace Fusumity.Pooling
{
	public class UnityObjectPool<T> : ObjectPool<T>
		where T : MonoBehaviour
	{
		public UnityObjectPool(Transform parent, T template, bool collectionCheck = false,
			int capacity = 0, int maxSize = 0)
			: base(new Policy(parent, template), collectionCheck, capacity, maxSize)
		{
		}

		private class Policy : IObjectPoolPolicy<T>
		{
			private readonly T _template;
			private readonly Transform _parent;

			public Policy(Transform parent, T template)
			{
				_parent = parent;
				_template = template;
			}

			public T Create() => Object.Instantiate(_template, _parent);

			public void OnGet(T obj) => obj.gameObject.SetActive(true);

			public void OnRelease(T obj) => obj.gameObject.SetActive(false);

			public void OnDispose(T obj) => Object.Destroy(obj.gameObject);
		}
	}
}
