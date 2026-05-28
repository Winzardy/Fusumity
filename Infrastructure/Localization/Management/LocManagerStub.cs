#if !FULLWEIGHT_MODE

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia;

namespace Localization
{
	/// <summary>THIS VERSION USED AS STUB (MOCK) IN LIGHTWEIGHT MODE! </summary>
	public class LocManager : StaticWrapper<LocalizationResolver>
	{
		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => true;
		}

		public static string CurrentLocaleCode => "lightweight_mode";
		public static string CurrentLanguage => "lightweight_mode";

		public static event Action<string> CurrentLocaleCodeUpdated { add { } remove { } }
		public static event Action LanguageChanged { add { } remove { } }

		public static bool Has(string key) => false;
		public static string Get(string key, string defaultValue = null) => key;
		public static string GetFormatted(string key, params (string tag, string value)[] toReplace) => key;
		public static void SetLanguage(string localeCode) { }

		public static IEnumerable<string> GetAllLocaleCodes()
		{
			yield return "lightweight_mode";
		}

		public static IEnumerable<string> GetAllLanguages()
		{
			yield return "lightweight_mode";
		}

		public static UniTask AddTableAsync(LocTableReference tableRef, CancellationToken token) => UniTask.CompletedTask;
	}
}
#endif
