using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using UnityEngine;
using UnityEngine.Networking;

namespace Fusumity.Utility.UserLocator
{
	public static class UserLocator
	{
		public const string CHANNEL_FORMAT = "[{0}]";
		public const string UNDEFINED = "?";

		#region Debug

		private const string CHANNEL_NAME = "Location";

		private static readonly string PREFIX = string.Format(CHANNEL_FORMAT,
			CHANNEL_NAME.ColorTextInEditor(Clipboard.DEBUG_COLOR));

		#endregion

		private const int REQUEST_TIMEOUT_SECONDS = 5;
		private const int ROUNDS = 10;
		private const int ROUND_DELAY_MS = 1000;
		private const int NETWORK_WAIT_ATTEMPTS = 5;
		private const int NETWORK_WAIT_DELAY_MS = 2000;

		private static readonly GeoProvider[] _providers =
		{
			new FreeIpApiProvider(),
			new GeoJsProvider(),
			new CountryIsProvider(),
		};

		private static string _header;
		private static AsyncLazy<LocationResult> _locationLazy;
		private static LocationResult _location = LocationResult.Invalid;

		/// <summary>
		/// Country code (2 letter, ISO 3166-1 alpha-2)
		/// </summary>
		public static string CountryCode => _location.countryCode ?? UNDEFINED;

		public static string City => _location.city ?? UNDEFINED;

		public static async UniTask<string> GetCountryAsync(CancellationToken cancellationToken = default)
		{
			var location = await FetchAsync(cancellationToken);
			return location.countryCode ?? UNDEFINED;
		}

		public static async UniTask<string> GetCityAsync(CancellationToken cancellationToken = default)
		{
			var location = await FetchAsync(cancellationToken);
			return location.city ?? UNDEFINED;
		}

		private static async UniTask<LocationResult> FetchAsync(CancellationToken cancellationToken)
		{
			_locationLazy ??= new AsyncLazy<LocationResult>(FetchLocationAsync);
			_location = await _locationLazy.Task.AttachExternalCancellation(cancellationToken);
			return _location;
		}

		private static async UniTask<LocationResult> FetchLocationAsync()
		{
			// Кэш общий на всё приложение, поэтому фабрика AsyncLazy не должна зависеть от токена
			// конкретного вызывающего
			var token = UnityLifecycle.ApplicationCancellationToken;

			for (var round = 0; round < ROUNDS; round++)
			{
				if (Application.internetReachability == NetworkReachability.NotReachable)
					await WaitForNetworkAsync(token);

				for (var i = 0; i < _providers.Length; i++)
				{
					var provider = _providers[i];
					LocationResult? result = null;
					try
					{
						result = await provider.FetchAsync(token);
					}
					catch { }
					if (result.HasValue)
					{
						Debug.Log($"{PREFIX} identified country: {result.Value.countryCode} (via {provider.Name})");
						return result.Value;
					}
				}

				if (round + 1 < ROUNDS)
					await UniTask.Delay(ROUND_DELAY_MS, DelayType.Realtime, cancellationToken: token);
			}

			Debug.LogError($"{PREFIX} all providers failed after {ROUNDS} rounds, country is {UNDEFINED}");
			_locationLazy = null;
			return LocationResult.Invalid;
		}

		private static async UniTask WaitForNetworkAsync(CancellationToken cancellationToken)
		{
			for (var i = 0; i < NETWORK_WAIT_ATTEMPTS; i++)
			{
				if (Application.internetReachability != NetworkReachability.NotReachable)
					return;

				await UniTask.Delay(NETWORK_WAIT_DELAY_MS, DelayType.Realtime, cancellationToken: cancellationToken);
			}
		}

		private static string GetHeader() =>
			_header ??= $"{Application.productName}/{Application.version} (Unity)";

		private static bool IsValidCountryCode(string code)
		{
			if (code == null || code.Length != 2)
				return false;

			for (var i = 0; i < 2; i++)
			{
				var c = code[i];
				if ((c < 'A' || c > 'Z') && (c < 'a' || c > 'z'))
					return false;
			}

			return true;
		}

		private readonly struct LocationResult
		{
			public static readonly LocationResult Invalid = new(null, null);

			public readonly string countryCode;
			public readonly string city;

			public LocationResult(string countryCode, string city)
			{
				this.countryCode = IsValidCountryCode(countryCode) ? countryCode.ToUpperInvariant() : null;
				this.city        = string.IsNullOrEmpty(city) ? null : city;
			}

			public bool IsValid => countryCode != null;
		}

		private abstract class GeoProvider
		{
			public abstract string Name { get; }

			protected abstract string Url { get; }

			protected abstract LocationResult Parse(string json);

			public async UniTask<LocationResult?> FetchAsync(CancellationToken cancellationToken)
			{
				using var request = UnityWebRequest.Get(Url);
				request.timeout = REQUEST_TIMEOUT_SECONDS;
				request.SetRequestHeader("User-Agent", GetHeader());

				var (isCanceled, _) = await request.SendWebRequest()
					.WithCancellation(cancellationToken)
					.SuppressCancellationThrow();

				if (isCanceled)
					cancellationToken.ThrowIfCancellationRequested();

				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.LogWarning($"{PREFIX} {Name} failed: {request.error}");
					return null;
				}

				var result = Parse(request.downloadHandler.text);
				if (!result.IsValid)
				{
					Debug.LogWarning($"{PREFIX} {Name} returned unusable payload");
					return null;
				}

				return result;
			}
		}

		private sealed class FreeIpApiProvider : GeoProvider
		{
			public override string Name => "FreeIPAPI";

			protected override string Url => "https://free.freeipapi.com/api/json/";

			protected override LocationResult Parse(string json)
			{
				try
				{
					var response = JsonUtility.FromJson<Response>(json);
					return new LocationResult(response?.countryCode, response?.cityName);
				}
				catch (Exception)
				{
					return LocationResult.Invalid;
				}
			}

			[Serializable]
			private class Response
			{
				public string countryCode;
				public string cityName;
			}
		}

		private sealed class GeoJsProvider : GeoProvider
		{
			public override string Name => "GeoJS";

			protected override string Url => "https://get.geojs.io/v1/ip/geo.json";

			protected override LocationResult Parse(string json)
			{
				try
				{
					var response = JsonUtility.FromJson<Response>(json);
					return new LocationResult(response?.country_code, response?.city);
				}
				catch (Exception)
				{
					return LocationResult.Invalid;
				}
			}

			// JsonUtility мапит поля по точному имени, без атрибута переименования, поэтому
			// snake_case здесь обязателен вопреки house style.
			[Serializable]
			private class Response
			{
				public string country_code;
				public string city;
			}
		}

		private sealed class CountryIsProvider : GeoProvider
		{
			public override string Name => "country.is";

			protected override string Url => "https://api.country.is/";

			protected override LocationResult Parse(string json)
			{
				try
				{
					var response = JsonUtility.FromJson<Response>(json);
					return new LocationResult(response?.country, null);
				}
				catch (Exception)
				{
					return LocationResult.Invalid;
				}
			}

			[Serializable]
			private class Response
			{
				public string country;
			}
		}
	}
}
