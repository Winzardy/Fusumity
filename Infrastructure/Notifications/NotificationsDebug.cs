using System;
using UnityEngine;

namespace Notifications
{
	using ILogger = Sapientia.ILogger;

	public class NotificationsDebug
	{
		public static ILogger logger;

		[HideInCallstack]
		public static void Log(object msg, object context = null) => logger?.Log(msg, context);

		[HideInCallstack]
		public static void LogWarning(object msg, object context = null) => logger?.LogWarning(msg, context);

		[HideInCallstack]
		public static void LogError(object msg, object context = null) => logger?.LogError(msg, context);

		[HideInCallstack]
		public static void LogException(Exception exception, object context = null) => logger?.LogException(exception, context);

		public static Exception NullException(object msg) =>
			logger?.NullReferenceException(msg) ?? new NullReferenceException(msg.ToString());

		public static Exception Exception(object msg) => logger?.Exception(msg) ?? new Exception(msg.ToString());

		public static Color COLOR = new(0.1f, 0.5f, 0.5f);
	}
}
