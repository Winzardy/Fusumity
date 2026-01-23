using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Sapientia.Utility
{
	public static partial class UniTaskUtility
	{
		/// <summary>
		/// Преобразует вызов метода с колбеком (1 параметр) в UniTask<br/>
		/// Использовать: <b>int res = await FromCallback&lt;int&gt;(callback => MethodWithCallback(callback))</b>
		/// </summary>
		public static async UniTask<T> FromCallback<T>(Action<Action<T>> action, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var tcs = new UniTaskCompletionSource<T>();

			await using var _ = cancellationToken.CanBeCanceled
				? cancellationToken.Register(static tcs => ((UniTaskCompletionSource)tcs).TrySetCanceled(), tcs)
				: default;
			try
			{
				action(result => tcs.TrySetResult(result));
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}

			return await tcs.Task;
		}

		/// <summary>
		/// Преобразует вызов метода с колбеком (2 параметра) в UniTask<br/>
		/// Использовать: <b>(bool b, string s) = await FromCallback&lt;int, string&gt;(callback => MethodWithCallback(callback))</b>
		/// </summary>
		public static async UniTask<(T1, T2)> FromCallback<T1, T2>(Action<Action<T1, T2>> action, CancellationToken cancellationToken = default)
		{
			return await FromCallback<(T1, T2)>(cb => action((t1, t2) => cb((t1, t2))), cancellationToken);
		}

		/// <summary>
		/// Преобразует вызов метода с колбеком (3 параметра) в UniTask<br/>
		/// Использовать: <b>(bool b, string s, float f) = await FromCallback&lt;int, string, float&gt;(callback => MethodWithCallback(callback))</b>
		/// </summary>
		public static async UniTask<(T1, T2, T3)> FromCallback<T1, T2, T3>(Action<Action<T1, T2, T3>> action, CancellationToken cancellationToken = default)
		{
			return await FromCallback<(T1, T2, T3)>(cb => action((t1, t2, t3) => cb((t1, t2, t3))), cancellationToken);
		}
	}
}
