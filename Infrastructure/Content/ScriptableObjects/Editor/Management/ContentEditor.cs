using System;
using System.Collections.Generic;
using Content.ScriptableObjects;

namespace Content.Editor
{
	/// <summary>
	/// Редактирование контента в редакторе
	/// </summary>
	public static class ContentEditor
	{
		public static IEnumerable<IUniqueContentEntryScriptableObject<T>> GetAllEntries<T>()
		{
			foreach (var target in ContentEditorCache.GetAssets<ContentScriptableObject>(false))
			{
				if (target is IUniqueContentEntryScriptableObject<T> source)
					yield return source;
			}
		}

		public static void Edit<T>(ContentEditing<T> editing)
		{
			foreach (var target in ContentEditorCache.GetAssets<ContentScriptableObject>(false))
			{
				if (target is not IContentEntryScriptableObject<T> source)
					continue;

				source.EditValue(editing);
				return;
			}

			throw new NullReferenceException("Not found single entry of type [ " + typeof(T).Name + " ]");
		}

		public static void Edit<T>(string id, ContentEditing<T> editing)
		{
			foreach (var source in GetAllEntries<T>())
			{
				if (source.Id != id)
					continue;

				source.EditValue(editing);
				return;
			}

			throw new NullReferenceException("Not found entry of type [ " + typeof(T).Name + " ] with id: [ " + id + " ]");
		}

		public static void Edit<T>(in SerializableGuid guid, ContentEditing<T> editing)
		{
			foreach (var source in GetAllEntries<T>())
			{
				if (source.UniqueContentEntry.Guid != guid)
					continue;

				source.EditValue(editing);
				return;
			}

			throw new NullReferenceException("Not found entry of type [ " + typeof(T).Name + " ] with guid: [ " + guid + " ]");
		}

		public static void Edit<T>(in ContentReference<T> reference, ContentEditing<T> editing)
		{
			foreach (var source in GetAllEntries<T>())
			{
				if (source.UniqueContentEntry.Guid != reference.guid)
					continue;

				source.EditValue(editing);
				return;
			}

			throw new NullReferenceException("Not found entry of type [ " + typeof(T).Name + " ] with guid: [ " + reference.guid.guid + " ]");
		}
	}

	public static class ContentEditorExtensions
	{
		/// <summary>Изменяем конкретный entry без повторного поиска по id</summary>
		public static void Edit<T>(this IUniqueContentEntryScriptableObject<T> source,
			ContentEditing<T> editing, bool save = true)
			=> source.EditValue(editing, save);

		/// <summary>
		/// Изменяем Value по reference
		/// </summary>
		public static void Edit<T>(this ContentReference<T> reference, ContentEditing<T> editing)
			=> ContentEditor.Edit(reference, editing);
	}
}
