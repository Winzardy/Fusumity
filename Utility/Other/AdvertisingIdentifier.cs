using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Extensions;
using UnityEngine;

namespace Fusumity.Utility
{
	/// <summary>
	/// Рекламный идентификатор устройства: GAID на Android, IDFA на iOS
	/// </summary>
	/// <remarks>
	/// Unity отдаёт идентификатор только на iOS и UWP, на Android остаётся нативный клиент
	/// Google Play services. Тот запрещает вызов с главного потока, поэтому запрос уходит в пул.
	/// Нужен там, где устройство надо опознать снаружи: тестовые девайсы в кабинетах MMP, атрибуция, реклама
	/// </remarks>
	public static class AdvertisingIdentifier
	{
		private const string UNSUPPORTED_ERROR = "platform does not provide an advertising identifier";

		public readonly struct Info
		{
			public readonly string id;

			/// <summary>
			/// Игрок запретил использовать идентификатор для персонализации рекламы
			/// </summary>
			public readonly bool limited;

			public readonly string error;

			public bool IsValid => error.IsNullOrEmpty() && !id.IsNullOrEmpty();

			public Info(string id, bool limited, string error = null)
			{
				this.id = id;
				this.limited = limited;
				this.error = error;
			}
		}

		public static async UniTask<Info> RequestAsync(CancellationToken cancellationToken = default)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			var id = string.Empty;
			var limited = false;
			var error = string.Empty;

			await UniTask.RunOnThreadPool(() =>
			{
				AndroidJNI.AttachCurrentThread();

				try
				{
					using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
					using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
					using var client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
					using var info = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", activity);

					id = info.Call<string>("getId");
					limited = info.Call<bool>("isLimitAdTrackingEnabled");
				}
				catch (Exception exception)
				{
					error = exception.Message;
				}
				finally
				{
					AndroidJNI.DetachCurrentThread();
				}
			}, cancellationToken: cancellationToken);

			return new Info(id, limited, error);
#elif UNITY_IOS && !UNITY_EDITOR
			var source = new UniTaskCompletionSource<Info>();

			// Колбэк отдаёт разрешение на трекинг, а наружу удобнее противоположный флаг
			if (!Application.RequestAdvertisingIdentifierAsync((identifier, trackingEnabled, error)
				=> source.TrySetResult(new Info(identifier, !trackingEnabled, error))))
				return new Info(string.Empty, false, UNSUPPORTED_ERROR);

			return await source.Task;
#else
			await UniTask.CompletedTask;

			return new Info(string.Empty, false, UNSUPPORTED_ERROR);
#endif
		}
	}
}
