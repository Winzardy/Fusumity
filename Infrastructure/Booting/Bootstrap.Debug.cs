using System;
using Fusumity.Utility;
using WLog;

namespace Booting
{
	public partial class Bootstrap
	{
		private const string CHANNEL_NAME = "Bootstrap";

		private static readonly UnityEngine.Color COLOR = new(0.4f, 0.4f, 0.8f);

		private static readonly string PREFIX = string.Format(WLogExtensions.CHANNEL_FORMAT,
			CHANNEL_NAME.ColorTextInEditorOnly(COLOR));

		private static readonly WLogContext _log = WLogContext.Create(CHANNEL_NAME, miniStackTracePosition: MiniStackTracePosition.End);

		private static void Log(string msg)
		{
			_log.Log($"{PREFIX} {msg}");
		}

		public static void LogException(Exception exception)
		{
			_log.LogException(exception, $"{PREFIX} {exception.Message}");
		}
	}
}
