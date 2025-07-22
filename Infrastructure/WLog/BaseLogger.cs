using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace WLog
{
	using UnityObject = UnityEngine.Object;

	public abstract class BaseLogger : Sapientia.ILogger
	{
		protected abstract string Prefix { get; }

		protected abstract WLogContext LogContext { get; }

		[HideInCallstack]
		public void Log(object msg, object context = null,
			[CallerMemberName] string memberName = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			LogContext.Log($"{Prefix} {msg}", (UnityObject) context, memberName, sourceLineNumber);
		}

		[HideInCallstack]
		public void LogWarning(object msg, object context = null,
			[CallerMemberName] string memberName = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			LogContext.LogWarning($"{Prefix} {msg}", (UnityObject) context, memberName, sourceLineNumber);
		}

		[HideInCallstack]
		public void LogError(object msg, object context = null,
			[CallerMemberName] string memberName = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			LogContext.LogError($"{Prefix} {msg}", (UnityObject) context, memberName, sourceLineNumber);
		}

		[HideInCallstack]
		public void LogException(Exception exception, object context = null,
			[CallerMemberName] string memberName = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			LogContext.LogException(exception, null, (UnityObject) context, memberName, sourceLineNumber);
		}

		public NullReferenceException NullReferenceException(object msg) => new($"{Prefix} {msg}");
		public Exception Exception(object msg) => new($"{Prefix} {msg}");
	}
}
