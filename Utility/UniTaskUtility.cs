using Cysharp.Threading.Tasks;

namespace Fusumity.Utility
{
	public static partial class UniTaskUtility
	{
		public static void TrySetResultAndSetNull(ref UniTaskCompletionSource cts)
		{
			if(cts == null)
				return;

			cts.TrySetResult();
			cts = null;
		}
	}
}
