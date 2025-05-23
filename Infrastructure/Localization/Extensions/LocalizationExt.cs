using Sapientia.Pooling;

namespace Localizations
{
	public static class LocalizationExt
	{
		public static string ToLocalize(this string key)
		{
			if (!Localization.IsInitialized)
			{
#if UNITY_EDITOR
				return Localization.GetEditor(key);
#endif
				return key;
			}

			return Localization.Get(key);
		}

		public static bool HasLocalize(this string key)
		{
			if (!Localization.IsInitialized)
				return false;

			return Localization.Contains(key);
		}

		public static string ToLocalize(this string key, params object[] args)
		{
			if (!Localization.IsInitialized)
				return key;

			return new TextLocalizationArgs
			{
				key = key,
				args = args
			}.ToString();
		}

		public static string ToLocalize(this string key, string tag, string value)
		{
			if (!Localization.IsInitialized)
				return key;

			var args = new TextLocalizationArgs
			{
				key = key,
				autoReturnToPool = true,
			};

			args.tags ??= DictionaryPool<string, object>.Get();
			args.tags[tag] = value;

			return args.ToString();
		}

		public static string ToLocalize(this string key, params (string tag, string value)[] tags)
		{
			if (!Localization.IsInitialized)
				return key;

			var args = new TextLocalizationArgs
			{
				key = key,
				autoReturnToPool = true,
			};

			foreach (var (name, value) in tags)
			{
				args.tags ??= DictionaryPool<string, object>.Get();
				args.tags[name] = value;
			}

			return args.ToString();
		}

		internal static bool IsEmpty(string key) =>
			string.IsNullOrWhiteSpace(key);
	}
}
