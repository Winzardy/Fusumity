#nullable enable
using Sapientia;
using Sapientia.Pooling;
using UnityEngine;

namespace Fusumity
{
	public class BlackboardSource : MonoBehaviour
	{
		private Blackboard? _blackboard;

		private void Awake()
		{
			Pool<Blackboard>.Get(out _blackboard);
		}

		private void OnDestroy()
		{
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _blackboard);
		}

		public T Get<T>(string? key = null, T defaultValue = default)
		{
			if (_blackboard == null)
				return defaultValue;

			if (!_blackboard.Contains<T>(key))
				return defaultValue;
			return _blackboard.Get<T>(key);
		}

		public BlackboardToken Register<T>(T value, string? key = null)
		{
			return _blackboard!.Register(value, key);
		}
	}
}
