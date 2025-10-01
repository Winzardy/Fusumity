using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Fusumity.Utility
{
	public static class Wait
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UniTask Seconds(float seconds, CancellationToken token = default)
		{
			return UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UniTask Until(Func<bool> predicate, CancellationToken token = default)
		{
			return UniTask.WaitUntil(predicate, cancellationToken: token);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UniTask While(Func<bool> predicate, CancellationToken token = default)
		{
			return UniTask.WaitWhile(predicate, cancellationToken: token);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UniTask All(IEnumerable<UniTask> tasks, CancellationToken token = default)
		{
			return UniTask
				.WhenAll(tasks)
				.AttachExternalCancellation(token);
		}
	}
}
