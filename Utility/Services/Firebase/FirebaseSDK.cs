using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Firebase;

namespace Fusumity.Utility
{
	public class FirebaseSDK
	{
		private static Task<DependencyStatus> _cache;

		public static async UniTask<DependencyStatus> CheckAndFixDependenciesAsync(CancellationToken cancellationToken = default)
		{
			if (_cache is {IsCompleted: true})
				return _cache.Result;

			_cache ??= FirebaseApp.CheckAndFixDependenciesAsync();

			return await _cache;
		}
	}
}
