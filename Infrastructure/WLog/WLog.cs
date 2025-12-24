using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Fusumity.Utility;
using JetBrains.Annotations;
using UnityEngine;

namespace WLog
{
	using UnityDebug = UnityEngine.Debug;
	using UnityObject = UnityEngine.Object;

	// Output format:
	// [MyClass.TestLog:18] log
	//
	// Via this:
	//
	// this.Log("log, try add this as context (UnityEngine.Object)");
	// this.Log("log with another context", anotherGameObject);
	// this.LogFormat("My format {0} and {1}, try add this as context", args: new object[] { "FIRST_ARG", 2 });
	// this.LogFormat(anotherGameObject, "My format {0} and {1}, with another context", args: new object[] { "FIRST_ARG", 2 });
	// this.LogFormat(LogType.Log, LogOption.NoStacktrace, anotherGameObject, "My format {0} and {1}, with another context", args: new object[] { "FIRST_ARG", 2 });
	//
	//
	// Via custom created context:
	//
	// var logContext = LogContext.Create("MyContext"); // or LogContext.Create<MyClass>();
	// logContext.Log("log");
	// logContext.Log("log with another context", anotherGameObject);
	// logContext.LogFormat("My format {0} and {1}, try add this as context", args: new object[] { "FIRST_ARG", 2 });
	// logContext.LogFormat(anotherGameObject, "My format {0} and {1}, with another context", args: new object[] { "FIRST_ARG", 2 });
	// logContext.LogFormat(LogType.Log, LogOption.NoStacktrace, anotherGameObject, "My format {0} and {1}, with another context", args: new object[] { "FIRST_ARG", 2 });
	//
	// Change log level:
	// this.SetLogType(WLogType.Debug | WLogType.Error);
	//

	[Flags]
	public enum WLogType
	{
		None = 0,
		Assert = 1 << 0,
		Debug = 1 << 1,
		Warning = 1 << 2,
		Error = 1 << 3,
		Exception = 1 << 4,

		All = Assert | Debug | Warning | Error | Exception,

		AssertAndHigher = Assert | DebugAndHigher,
		DebugAndHigher = Debug | WarningAndHigher,
		WarningAndHigher = Warning | ErrorAndHigher,
		ErrorAndHigher = Error | Exception,
	}

	public enum MiniStackTracePosition
	{
		Start,
		End
	}

	public class WLogContext
	{
		public static WLogContext Create<T>(WLogType logType = WLogType.All,
			MiniStackTracePosition miniStackTracePosition = MiniStackTracePosition.Start)
		{
			var context = WLogContextHolder.GetOrCreate(typeof(T));
			context.LogType = logType;
			context.miniStackTracePosition = miniStackTracePosition;
			return context;
		}

		public static WLogContext Create(string name, WLogType logType = WLogType.All,
			MiniStackTracePosition miniStackTracePosition = MiniStackTracePosition.Start)
		{
			var context = WLogContextHolder.GetOrCreate(name);
			context.LogType = logType;
			context.miniStackTracePosition = miniStackTracePosition;
			return context;
		}

		public readonly string Name;

		public WLogType LogType;

		public MiniStackTracePosition miniStackTracePosition = MiniStackTracePosition.Start;

		internal WLogContext(string name, WLogType logType = WLogType.All)
		{
			Name = name;
			LogType = logType;
		}

		public bool IsLogTypeAllowed(WLogType logType)
		{
			return LogType.HasFlag(logType);
		}
	}

	public static class WLogExtensions
	{
		public const string CHANNEL_FORMAT = "[{0}]";

		[HideInCallstack]
		public static void LogMe<T>(this T self,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Debug))
				return;
			UnityDebug.Log($"[{logContext.Name}.{memberName}:{sourceLineNumber}]", self as UnityObject);
		}

		[HideInCallstack]
		public static void LogMeWarning<T>(this T self,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Warning))
				return;
			UnityDebug.LogWarning($"[{logContext.Name}.{memberName}:{sourceLineNumber}]", self as UnityObject);
		}

		[HideInCallstack]
		public static void LogMeError<T>(this T self,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Error))
				return;
			UnityDebug.LogError($"[{logContext.Name}.{memberName}:{sourceLineNumber}]", self as UnityObject);
		}

		[HideInCallstack]
		public static void Log<T>(this T self, object message,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Debug))
				return;

			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.Log($"{prefix}{message}{postfix}", self as UnityObject);
		}

		[HideInCallstack]
		public static void Log<T>(this T self, object message, UnityObject context,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Debug))
				return;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.Log($"{prefix}{message}{postfix}", context);
		}

		[HideInCallstack]
		public static void LogFormat<T>(this T self, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Debug))
				return;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.LogFormat(self as UnityObject, $"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogFormat<T>(this T self, UnityObject context, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Debug))
				return;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.LogFormat(context, $"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogFormat<T>(this T self, LogType logType, LogOption logOptions, UnityObject context,
			string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Debug))
				return;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.LogFormat(logType, logOptions, context,
				$"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogWarning<T>(this T self, object message,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Warning))
				return;
			UnityObject context = self as UnityObject;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.LogWarning($"{prefix}{message}{postfix}", context);
		}

		[HideInCallstack]
		public static void LogWarning<T>(this T self, object message, UnityObject context,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Warning))
				return;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.LogWarning($"{prefix}{message}{postfix}", context);
		}

		[HideInCallstack]
		public static void LogWarningFormat<T>(this T self, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Warning))
				return;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.LogWarningFormat(self as UnityObject, $"{prefix}{format}{postfix}",
				args);
		}

		[HideInCallstack]
		public static void LogWarningFormat<T>(this T self, UnityObject context, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Warning))
				return;

			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.LogWarningFormat(context, $"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogError<T>(this T self, object message,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Error))
				return;
			UnityObject context = self as UnityObject;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.LogError($"{prefix}{message}{postfix}", context);
		}

		[HideInCallstack]
		public static void LogError<T>(this T self, object message, UnityObject context,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Error))
				return;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.LogError($"{prefix}{message}{postfix}", context);
		}

		[HideInCallstack]
		public static void LogErrorFormat<T>(this T self, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Error))
				return;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.LogErrorFormat(self as UnityObject, $"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogErrorFormat<T>(this T self, UnityObject context, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Error))
				return;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			UnityDebug.LogErrorFormat(context, $"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogException<T>(this T self, Exception exception, string message = null,
			UnityObject context = null,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Exception))
				return;
			UnityObject objectContext = context ?? self as UnityObject;
			var miniStackTrace = $"{logContext.Name}.{memberName}:{sourceLineNumber}";
			var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;

			var prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
			var postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			if (message != null)
				UnityDebug.Log($"{prefix}{message}{postfix}", objectContext);
			UnityDebug.LogException(exception, objectContext);
		}

		[HideInCallstack]
		public static void LogUnexpected<T>(this T self, Exception exception, string message = null,
			UnityObject context = null,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			LogException(self, exception, "Unexpected exception occured", context, memberName, sourceLineNumber);
		}

		[HideInCallstack]
		[Conditional("UNITY_ASSERTIONS")]
		[AssertionMethod]
		[ContractAnnotation("condition:false=>halt")]
		public static void Assert<T>(this T self, bool condition)
		{
			UnityDebug.Assert(condition);
		}

		[HideInCallstack]
		[Conditional("UNITY_ASSERTIONS")]
		[AssertionMethod]
		[ContractAnnotation("condition:false=>halt")]
		public static void Assert<T>(this T self, bool condition, string message)
		{
			UnityDebug.Assert(condition, message);
		}

		public static void SetLogType<T>(this T self, WLogType logType)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			logContext.LogType = logType;
		}
	}
}
