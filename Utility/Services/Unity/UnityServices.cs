using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;

namespace Fusumity.Utility
{
	public static class UnityServices
	{
		/// <summary>
		/// Метод (async) для ожидания или инициализации UnityServices
		/// </summary>
		public static async UniTask<bool> UnityServiceInitializationAsync(CancellationToken cancellationToken = default)
		{
			switch (global::Unity.Services.Core.UnityServices.State)
			{
				case ServicesInitializationState.Initialized:
					return true;

				case ServicesInitializationState.Uninitialized:
					await global::Unity.Services.Core.UnityServices.InitializeAsync()
					   .AsUniTask().
						AttachExternalCancellation(cancellationToken);
					return global::Unity.Services.Core.UnityServices.State == ServicesInitializationState.Initialized;

				case ServicesInitializationState.Initializing:
					var tcs = new UniTaskCompletionSource<bool>();

					global::Unity.Services.Core.UnityServices.Initialized += OnInitializedInternal;
					global::Unity.Services.Core.UnityServices.InitializeFailed += OnInitializeFailedInternal;

					try
					{
						await using (cancellationToken.Register(Cancel))
							return await tcs.Task;
					}
					catch (OperationCanceledException)
					{
						return false;
					}

					void OnInitializedInternal()
					{
						global::Unity.Services.Core.UnityServices.Initialized -= OnInitializedInternal;
						global::Unity.Services.Core.UnityServices.InitializeFailed += OnInitializeFailedInternal;

						tcs.TrySetResult(true);
					}

					void OnInitializeFailedInternal(Exception exception)
					{
						global::Unity.Services.Core.UnityServices.Initialized += OnInitializedInternal;
						global::Unity.Services.Core.UnityServices.InitializeFailed -= OnInitializeFailedInternal;

						Debug.LogException(exception);
						tcs.TrySetResult(false);
					}

					void Cancel()
					{
						global::Unity.Services.Core.UnityServices.Initialized -= OnInitializedInternal;
						global::Unity.Services.Core.UnityServices.InitializeFailed -= OnInitializeFailedInternal;
						tcs.TrySetCanceled();
					}
			}

			return false;
		}
	}
}
