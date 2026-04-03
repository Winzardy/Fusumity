using Fusumity.Utility;
using JetBrains.Annotations;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
	// var logContext = WLogContext.Create("MyContext"); // or LogContext.Create<MyClass>();
	// logContext.Log("log");
	// logContext.Log("log with another context", anotherGameObject);
	// logContext.LogFormat("My format {0} and {1}, try add this as context", args: new object[] { "FIRST_ARG", 2 });
	// logContext.LogFormat(anotherGameObject, "My format {0} and {1}, with another context", args: new object[] { "FIRST_ARG", 2 });
	// logContext.LogFormat(LogType.Log, LogOption.NoStacktrace, anotherGameObject, "My format {0} and {1}, with another context", args: new object[] { "FIRST_ARG", 2 });
	//
	// Change log level:
	// this.SetLogType(WLogType.Debug | WLogType.Error);
	//
	//
	// Output format:
	// [ContextName] Message
	//
	// Via custom context with disabled mini stack trace:
	//
	// var logContext = WLogContext.Create("MyContext",  miniStackTracePosition: MiniStackTracePosition.None);
	// logContext.Log("Message");

	[Flags]
	public enum WLogType
	{
		None = 0,
		Debug = 1 << 0,
		Warning = 1 << 1,
		Assert = 1 << 2,
		Error = 1 << 3,
		Exception = 1 << 4,

		All = Debug | Warning | Assert | Error | Exception,

		DebugAndHigher = Debug | WarningAndHigher,
		WarningAndHigher = Warning | AssertAndHigher,
		AssertAndHigher = Assert | ErrorAndHigher,
		ErrorAndHigher = Error | Exception,
	}

	public enum MiniStackTracePosition
	{
		None,

		Start,
		End
	}

	public static class WLogTypeExtension
	{
		public static LogType ToUnityLogType(this WLogType logType)
		{
			return logType switch
			{
				WLogType.Debug => LogType.Log,
				WLogType.Warning => LogType.Warning,
				WLogType.Assert => LogType.Assert,
				WLogType.Error => LogType.Error,
				WLogType.Exception => LogType.Exception,
				_ => throw new ArgumentOutOfRangeException(nameof(logType), logType, null)
			};
		}
	}

	public class WLogContext
	{
		public readonly string name;
		public WLogType logType;
		public MiniStackTracePosition miniStackTracePosition = MiniStackTracePosition.Start;

		internal WLogContext(string name, WLogType logType = WLogType.All)
		{
			this.name = name;
			this.logType = logType;
		}

		public static WLogContext Create<T>(WLogType logType = WLogType.All,
			MiniStackTracePosition miniStackTracePosition = MiniStackTracePosition.Start)
		{
			var context = WLogContextHolder.GetOrCreate(typeof(T));
			context.logType = logType;
			context.miniStackTracePosition = miniStackTracePosition;
			return context;
		}

		public static WLogContext Create(string name, WLogType logType = WLogType.All,
			MiniStackTracePosition miniStackTracePosition = MiniStackTracePosition.Start)
		{
			var context = WLogContextHolder.GetOrCreate(name);
			context.logType = logType;
			context.miniStackTracePosition = miniStackTracePosition;
			return context;
		}

		public bool IsLogTypeAllowed(WLogType logType)
		{
			return
				this.logType.HasFlag(logType) &&
				UnityDebug.unityLogger.IsLogTypeAllowed(logType.ToUnityLogType());
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

			UnityDebug.Log($"[{logContext.name}.{memberName}:{sourceLineNumber}]", self as UnityObject);
		}

		[HideInCallstack]
		public static void LogMeWarning<T>(this T self,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Warning))
				return;

			UnityDebug.LogWarning($"[{logContext.name}.{memberName}:{sourceLineNumber}]", self as UnityObject);
		}

		[HideInCallstack]
		public static void LogMeError<T>(this T self,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			var logContext = WLogContextHolder.GetLogContext(self);
			if (!logContext.IsLogTypeAllowed(WLogType.Error))
				return;

			UnityDebug.LogError($"[{logContext.name}.{memberName}:{sourceLineNumber}]", self as UnityObject);
		}

		[HideInCallstack]
		public static void Log<T>(this T self, object message,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			if (!TryExtractLogComponents(self, WLogType.Debug, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

			UnityDebug.Log($"{prefix}{message}{postfix}", self as UnityObject);
		}

		[HideInCallstack]
		public static void Log<T>(this T self, object message, UnityObject context,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			if (!TryExtractLogComponents(self, WLogType.Debug, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

			UnityDebug.Log($"{prefix}{message}{postfix}", context);
		}

		[HideInCallstack]
		public static void LogFormat<T>(this T self, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			if (!TryExtractLogComponents(self, WLogType.Debug, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

			UnityDebug.LogFormat(self as UnityObject, $"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogFormat<T>(this T self, UnityObject context, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			if (!TryExtractLogComponents(self, WLogType.Debug, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

			UnityDebug.LogFormat(context, $"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogFormat<T>(this T self, LogType logType, LogOption logOptions, UnityObject context,
			string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			if (!TryExtractLogComponents(self, WLogType.Debug, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

			UnityDebug.LogFormat(logType, logOptions, context, $"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogWarning<T>(this T self, object message,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			if (!TryExtractLogComponents(self, WLogType.Warning, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

			UnityDebug.LogWarning($"{prefix}{message}{postfix}", self as UnityObject);
		}

		[HideInCallstack]
		public static void LogWarning<T>(this T self, object message, UnityObject context,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			if (!TryExtractLogComponents(self, WLogType.Warning, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

			UnityDebug.LogWarning($"{prefix}{message}{postfix}", context);
		}

		[HideInCallstack]
		public static void LogWarningFormat<T>(this T self, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			if (!TryExtractLogComponents(self, WLogType.Warning, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

			UnityDebug.LogWarningFormat(self as UnityObject, $"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogWarningFormat<T>(this T self, UnityObject context, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			if (!TryExtractLogComponents(self, WLogType.Warning, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

			UnityDebug.LogWarningFormat(context, $"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogError<T>(this T self, object message,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			if (!TryExtractLogComponents(self, WLogType.Error, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

			UnityDebug.LogError($"{prefix}{message}{postfix}", self as UnityObject);
		}

		[HideInCallstack]
		public static void LogError<T>(this T self, object message, UnityObject context,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
		{
			if (!TryExtractLogComponents(self, WLogType.Error, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;
			UnityDebug.LogError($"{prefix}{message}{postfix}", context);
		}

		[HideInCallstack]
		public static void LogErrorFormat<T>(this T self, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			if (!TryExtractLogComponents(self, WLogType.Error, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

			UnityDebug.LogErrorFormat(self as UnityObject, $"{prefix}{format}{postfix}", args);
		}

		[HideInCallstack]
		public static void LogErrorFormat<T>(this T self, UnityObject context, string format,
			[CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0,
			params object[] args)
		{
			if (!TryExtractLogComponents(self, WLogType.Error, memberName, sourceLineNumber,
				out var prefix, out var postfix))
				return;

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
			GetPrefixAndPostfix(logContext, memberName, sourceLineNumber, out var prefix, out var postfix);
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
			logContext.logType = logType;
		}

		private static bool TryExtractLogComponents<T>(T obj,
			WLogType expectedLogType, string memberName, int sourceLineNumber,
			out string prefix, out string postfix)
		{
			prefix = null;
			postfix = null;

			var logContext = WLogContextHolder.GetLogContext(obj);
			if (!logContext.IsLogTypeAllowed(expectedLogType))
				return false;

			GetPrefixAndPostfix(logContext, memberName, sourceLineNumber, out prefix, out postfix);
			return true;
		}

		private static void GetPrefixAndPostfix(WLogContext logContext, string memberName, int sourceLineNumber,
			out string prefix, out string postfix)
		{
			if (logContext.miniStackTracePosition == MiniStackTracePosition.None)
			{
				prefix = $"[{logContext.name}] ";
				postfix = null;
			}
			else
			{
				var miniStackTrace = $"{logContext.name}.{memberName}:{sourceLineNumber}";
				var isAtStart = logContext.miniStackTracePosition == MiniStackTracePosition.Start;
				prefix = isAtStart ? $"[{miniStackTrace}] " : string.Empty;
				postfix = isAtStart ? string.Empty : "\n" + miniStackTrace.ColorTextInEditorOnly(Color.gray);
			}
		}
	}
}
