using Fusumity.Utility;
using UnityEngine;

namespace Fusumity.Reactive
{
	public partial class UnityLifecycle
	{
		private static UnityLifecycle _instance;

		public static void Initialize()
		{
			var go = new GameObject($"[{nameof(UnityLifecycle)}]");
			_instance = go.AddComponent<UnityLifecycle>();
			go.DontDestroyOnLoad();
		}
	}
}
