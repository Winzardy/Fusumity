using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Notifications
{
	public static class NotificationsDebug
	{
		public static Sapientia.ILogger logger;

		[HideInCallstack]
		public static void Log(object msg, object context = null,
			[CallerMemberName] string memberName = "",
			[CallerLineNumber] int sourceLineNumber = 0)
			=> logger?.Log(msg, context, memberName, sourceLineNumber);

		[HideInCallstack]
		public static void LogWarning(object msg, object context = null,
			[CallerMemberName] string memberName = "",
			[CallerLineNumber] int sourceLineNumber = 0)
			=> logger?.LogWarning(msg, context, memberName, sourceLineNumber);

		[HideInCallstack]
		public static void LogError(object msg, object context = null,
			[CallerMemberName] string memberName = "",
			[CallerLineNumber] int sourceLineNumber = 0)
			=> logger?.LogError(msg, context, memberName, sourceLineNumber);

		[HideInCallstack]
		public static void LogException(Exception exception, object context = null,
			[CallerMemberName] string memberName = "",
			[CallerLineNumber] int sourceLineNumber = 0)
			=> logger?.LogException(exception, context, memberName, sourceLineNumber);

		public static Exception NullException(object msg) =>
			logger?.NullReferenceException(msg) ?? new NullReferenceException(msg.ToString());

		public static Exception Exception(object msg) => logger?.Exception(msg) ?? new Exception(msg.ToString());

		public static Color COLOR = new(0.1f, 0.5f, 0.5f);

#if DEV
		public static class Settings
		{
			public const int DEFAULT_DELIVERY_SPEED = 1;

			public static int deliverySpeed = DEFAULT_DELIVERY_SPEED;
			public static bool forceForegroundAll = false;
		}

		internal static void ApplySettings(ref NotificationRequest request, DateTime now)
		{
			if (Settings.forceForegroundAll)
				request.config.showInForeground = true;

			if (Settings.deliverySpeed <= Settings.DEFAULT_DELIVERY_SPEED)
				return;

			var originalDeliveryTime = request.deliveryTime!.Value;
			request.deliveryTime = now + TimeSpan.FromTicks((originalDeliveryTime - now).Ticks / Settings.deliverySpeed);

			var localDeliveryTime = originalDeliveryTime.Kind == DateTimeKind.Utc
				? originalDeliveryTime.ToLocalTime()
				: originalDeliveryTime;
			request.message += $"\n[Debug x{Settings.deliverySpeed}] Original delivery time: {localDeliveryTime:G}";
		}
#endif
	}
}
