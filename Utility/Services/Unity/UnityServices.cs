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
			var servicesInitializationState = Unity.Services.Core.UnityServices.State;
			switch (servicesInitializationState)
			{
				case ServicesInitializationState.Initialized:
					return true;

				case ServicesInitializationState.Uninitialized:
					Debug.Log("[UnityServices] Initializing");
					await Unity.Services.Core.UnityServices.InitializeAsync()
					   .AsUniTask().AttachExternalCancellation(cancellationToken);
					servicesInitializationState = Unity.Services.Core.UnityServices.State;
					var success = servicesInitializationState == ServicesInitializationState.Initialized;
					Debug.Log($"[UnityServices] Initialized UnityServices, success: {success}");
					return success;

				case ServicesInitializationState.Initializing:
					var tcs = new UniTaskCompletionSource<bool>();

					Unity.Services.Core.UnityServices.Initialized += OnInitializedInternal;
					Unity.Services.Core.UnityServices.InitializeFailed += OnInitializeFailedInternal;

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
						Unity.Services.Core.UnityServices.Initialized -= OnInitializedInternal;
						Unity.Services.Core.UnityServices.InitializeFailed += OnInitializeFailedInternal;

						tcs.TrySetResult(true);
					}

					void OnInitializeFailedInternal(Exception exception)
					{
						Unity.Services.Core.UnityServices.Initialized += OnInitializedInternal;
						Unity.Services.Core.UnityServices.InitializeFailed -= OnInitializeFailedInternal;

						Debug.LogException(exception);
						tcs.TrySetResult(false);
					}

					void Cancel()
					{
						Unity.Services.Core.UnityServices.Initialized -= OnInitializedInternal;
						Unity.Services.Core.UnityServices.InitializeFailed -= OnInitializeFailedInternal;
						tcs.TrySetCanceled();
					}
			}

			return false;
		}
	}
}
