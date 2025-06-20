using System;
using System.Runtime.CompilerServices;
using Sapientia;
using UnityEngine;

namespace InAppReview
{
	public static class InAppReviewDebug
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

		public static Color COLOR = Color.blue;
	}
}
