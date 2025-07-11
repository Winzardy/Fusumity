using Sapientia.Pooling;

namespace Localization
{
	public static partial class LocalizationUtility
	{
		public static string ToLocalize(this string key)
		{
			if (!LocManager.IsInitialized)
			{
#if UNITY_EDITOR
				return LocManager.GetEditor(key);
#endif
				return key;
			}

			return LocManager.Get(key);
		}

		public static bool HasLocalize(this string key)
		{
			if (!LocManager.IsInitialized)
				return false;

			return LocManager.Has(key);
		}

		public static string ToLocalize(this string key, params object[] args)
		{
			if (!LocManager.IsInitialized)
				return key;

			return new TextLocalizationArgs
			{
				key = key,
				args = args
			}.ToString();
		}

		public static string ToLocalize(this string key, string tag, string value)
		{
			if (!LocManager.IsInitialized)
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
			if (!LocManager.IsInitialized)
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
