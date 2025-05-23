using System;
using UnityEngine;

namespace AssetManagement
{
	using ILogger = Sapientia.ILogger;

	public class AssetManagementDebug
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

		public static readonly Color COLOR = new(102 / 255f, 171 / 255f, 202 / 255f);
	}
}
