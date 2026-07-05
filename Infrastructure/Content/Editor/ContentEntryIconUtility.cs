using System;
using System.Collections.Generic;
using Sapientia;
using UnityEngine;

namespace Content.Editor
{
	/// <summary>
	/// Разрешает <see cref="IPreviewIcon"/> для источника контента: приоритет у <see cref="IContentEntrySource"/>,
	/// иконка от Value используется как фолбэк. Если реализовано в обоих местах — предупреждает в лог (один раз на пару типов)
	/// </summary>
	public static class ContentEntryIconUtility
	{
		private static readonly HashSet<(Type sourceType, Type valueType)> _warnedConflicts = new();

		public static Sprite GetPreviewIcon(IContentEntrySource source)
		{
			if (source == null)
				return null;

			var fromSource = source is IPreviewIcon sourceIcon ? sourceIcon.PreviewIcon : null;

			var rawValue = source.ContentEntry?.RawValue;
			var fromValue = rawValue is IPreviewIcon valueIcon ? valueIcon.PreviewIcon : null;

			if (fromSource != null && fromValue != null)
				WarnConflict(source, rawValue);

			return fromSource != null ? fromSource : fromValue;
		}

		private static void WarnConflict(IContentEntrySource source, object rawValue)
		{
			var sourceType = source.GetType();
			var valueType = rawValue.GetType();

			if (!_warnedConflicts.Add((sourceType, valueType)))
				return;

			ContentDebug.LogWarning(
				$"IPreviewIcon is implemented by both IContentEntrySource [{sourceType.Name}] " +
				$"and Value [{valueType.Name}] — using the icon from IContentEntrySource.");
		}
	}
}
