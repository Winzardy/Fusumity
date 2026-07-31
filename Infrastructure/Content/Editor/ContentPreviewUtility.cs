using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Content.Editor
{
	/// <summary>
	/// Разрешает <see cref="ContentPreviewAttribute"/> для источника контента: приоритет у <see cref="IContentEntrySource"/>,
	/// иконка от Value используется как фолбэк. Если помечены оба места — предупреждает в лог (один раз на пару типов)
	/// </summary>
	public static class ContentPreviewUtility
	{
		private const BindingFlags MEMBER_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private static readonly HashSet<(Type sourceType, Type valueType)> _warnedConflicts = new();
		private static readonly Dictionary<Type, Func<object, Sprite>> _accessors = new();

		public static Sprite GetPreviewIcon(IContentEntrySource source)
		{
			if (source == null)
				return null;

			var fromSource = ResolveIcon(source);
			var contentEntry = source.ContentEntry;
			var valueAccessor = GetAccessor(contentEntry?.ValueType);
			if (valueAccessor == null)
				return fromSource;

			object rawValue;
			Sprite fromValue;
			try
			{
				rawValue = contentEntry.RawValue;
				fromValue = rawValue != null ? valueAccessor.Invoke(rawValue) : null;
			}
			catch (NullReferenceException)
			{
				return fromSource;
			}
			catch (TargetInvocationException exception) when (exception.InnerException is NullReferenceException)
			{
				return fromSource;
			}

			if (fromSource != null && fromValue != null)
				WarnConflict(source, rawValue);

			return fromSource != null ? fromSource : fromValue;
		}

		private static Sprite ResolveIcon(object target)
		{
			if (target == null)
				return null;

			return GetAccessor(target.GetType())?.Invoke(target);
		}

		private static Func<object, Sprite> GetAccessor(Type type)
		{
			if (type == null)
				return null;

			if (!_accessors.TryGetValue(type, out var accessor))
			{
				accessor = BuildAccessor(type);
				_accessors[type] = accessor;
			}

			return accessor;
		}

		// Строим доступ к иконке рефлексией по имени члена из ContentPreviewAttribute (кешируется на тип)
		private static Func<object, Sprite> BuildAccessor(Type type)
		{
			var attribute = type.GetCustomAttribute<ContentPreviewAttribute>(inherit: false);
			if (attribute == null)
				return null;

			var name = attribute.IconSource;

			var property = type.GetProperty(name, MEMBER_FLAGS);
			if (property != null && typeof(Sprite).IsAssignableFrom(property.PropertyType))
				return target => property.GetValue(target) as Sprite;

			var field = type.GetField(name, MEMBER_FLAGS);
			if (field != null && typeof(Sprite).IsAssignableFrom(field.FieldType))
				return target => field.GetValue(target) as Sprite;

			ContentDebug.LogWarning(
				$"[ContentPreview] member '{name}' of type Sprite not found on [{type.Name}].");
			return null;
		}

		private static void WarnConflict(IContentEntrySource source, object rawValue)
		{
			var sourceType = source.GetType();
			var valueType = rawValue.GetType();

			if (!_warnedConflicts.Add((sourceType, valueType)))
				return;

			ContentDebug.LogWarning(
				$"ContentPreview is declared by both IContentEntrySource [{sourceType.Name}] " +
				$"and Value [{valueType.Name}] — using the icon from IContentEntrySource.");
		}
	}
}
