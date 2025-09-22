using Cysharp.Threading.Tasks;

namespace Fusumity.Utility
{
	public static class UniTaskUtility
	{
		public static void TrySetResultAndSetNull(ref UniTaskCompletionSource cts)
		{
			cts.TrySetResult();
			cts = null;
		}
	}
}
