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

					var subscribed = true;

					Unity.Services.Core.UnityServices.Initialized      += OnInitializedInternal;
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
					finally
					{
						Unsubscribe();
					}

					void Unsubscribe()
					{
						if (!subscribed)
							return;

						subscribed = false;

						Unity.Services.Core.UnityServices.Initialized      -= OnInitializedInternal;
						Unity.Services.Core.UnityServices.InitializeFailed -= OnInitializeFailedInternal;
					}

					void OnInitializedInternal()
					{
						Unsubscribe();
						tcs.TrySetResult(true);
					}

					void OnInitializeFailedInternal(Exception exception)
					{
						Unsubscribe();
						Debug.LogException(exception);
						tcs.TrySetResult(false);
					}

					void Cancel()
					{
						Unsubscribe();
						tcs.TrySetCanceled();
					}
			}

			return false;
		}
	}
}
