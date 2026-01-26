using Sapientia.Collections;
using System;

namespace Fusumity.Utility
{
	public static class LogUtility
	{
		public static string GetStackTrace()
		{
			var fullTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
			var lines = fullTrace.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

			return string.Join("\n", lines.RemoveAt(0).RemoveAt(0));
		}
	}
}
