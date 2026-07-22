using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Localization;

namespace Localization
{
	public static class RemoteLocalizationsExtensions
	{
		public static bool EmbeddedKeyPresentInBuild(this RemoteLocalizationRequestResult requestResult)
		{
			return
				!requestResult.embeddedLocKey.IsNullOrEmpty() &&
				LocManager.Has(requestResult.embeddedLocKey);
		}

		public static bool TryAddRemoteStrings(this RemoteLocalizationRequestResult requestResult)
		{
			if (requestResult.remoteStrings.IsInvalid())
				return false;

			LocManager.AddRemotelyLoadedStrings(requestResult.remoteStrings);
			return true;
		}

		/// <summary>
		/// Extract key that can currently be used with <see cref="LocManager"/>.<br/>
		/// (Meaning it either has a valid embedded key, or provided remote strings have been added).
		/// </summary>
		public static bool TryExtractValidLocKey(this RemoteLocalizationRequestResult requestResult, out string locKey)
		{
			locKey = null;
			if (!requestResult.embeddedLocKey.IsNullOrEmpty() && LocManager.Has(requestResult.embeddedLocKey))
			{
				locKey = requestResult.embeddedLocKey;
			}
			else
			if (!requestResult.remoteStrings.IsInvalid() && LocManager.Has(requestResult.remoteStrings.key))
			{
				locKey = requestResult.remoteStrings.key;
			}

			return !locKey.IsNullOrEmpty();
		}

		public static bool IsEmptyOrInvalid(this RemoteLocalizationRequestResult requestResult)
		{
			return
				requestResult.embeddedLocKey.IsNullOrEmpty() &&
				requestResult.remoteStrings.IsInvalid();
		}

		public static bool IsInvalid(this RemoteLocalizationStrings strings)
		{
			return
				strings.key.IsNullOrEmpty() ||
				strings.languagePairs.IsNullOrEmpty();
		}
	}
}
