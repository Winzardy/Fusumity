using Sapientia.Collections;
using System;

namespace Fusumity.Utility
{
	public static class LogUtility
	{
		public static string GetStackTrace(int stripLines = 3)
		{
			var fullTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
			var lines = fullTrace.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

			for (int i = 0; i < stripLines; i++)
			{
				lines = lines.RemoveAt(0);
			}

			return string.Join("\n", lines);
		}
	}
}
