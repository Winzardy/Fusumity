#nullable enable
using Fusumity.Utility;
using Sapientia;
using Sapientia.Deterministic;
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

			_blackboard.Register(UnityRandomizer<int>.Default);
			_blackboard.Register(UnityRandomizer<Fix64>.Default);
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

		public static implicit operator Blackboard(BlackboardSource source) => source._blackboard;
	}
}
