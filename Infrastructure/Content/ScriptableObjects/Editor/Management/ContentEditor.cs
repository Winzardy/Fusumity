using System;
using Content.ScriptableObjects;
using Content.ScriptableObjects.Editor;
using UnityEditor;

namespace Content.Editor
{
	/// <summary>
	/// Редактирование контента в редакторе
	/// </summary>
	public static class ContentEditor
	{
		public static void Edit<T>(ContentEditing<T> editing)
		{
			foreach (var rawDatabase in ContentDatabaseEditorUtility.Databases)
			{
				if (rawDatabase is IContentEntryScriptableObject<T> database)
				{
					database.Edit(editing);
					return;
				}

				foreach (var target in rawDatabase.scriptableObjects)
				{
					if (target is not IContentEntryScriptableObject<T> scriptableObject)
						continue;

					scriptableObject.Edit(editing);
					return;
				}
			}

			throw new NullReferenceException("Not found single entry of type [ " + typeof(T).Name + " ]");
		}

		public static void Edit<T>(string id, ContentEditing<T> editing)
		{
			foreach (var database in ContentDatabaseEditorUtility.Databases)
			{
				foreach (var target in database.scriptableObjects)
				{
					if (target is not IUniqueContentEntryScriptableObject<T> source)
						continue;

					if (source.Id != id)
						continue;

					source.Edit(editing);
					return;
				}
			}

			throw new NullReferenceException("Not found entry of type [ " + typeof(T).Name + " ] with id: [ " + id + " ]");
		}

		public static void Edit<T>(in SerializableGuid guid, ContentEditing<T> editing)
		{
			foreach (var database in ContentDatabaseEditorUtility.Databases)
			{
				foreach (var target in database.scriptableObjects)
				{
					if (target is not IUniqueContentEntryScriptableObject<T> source)
						continue;

					if (source.UniqueContentEntry.Guid != guid)
						continue;

					source.Edit(editing);
					return;
				}
			}

			throw new NullReferenceException("Not found entry of type [ " + typeof(T).Name + " ] with guid: [ " + guid + " ]");
		}

		public static void Edit<T>(in ContentReference<T> reference, ContentEditing<T> editing)
		{
			foreach (var database in ContentDatabaseEditorUtility.Databases)
			{
				foreach (var target in database.scriptableObjects)
				{
					if (target is not IUniqueContentEntryScriptableObject<T> source)
						continue;

					if (source.UniqueContentEntry.Guid != reference.guid)
						continue;

					source.Edit(editing);
					return;
				}
			}

			throw new NullReferenceException("Not found entry of type [ " + typeof(T).Name + " ] with guid: [ " + reference.guid.guid + " ]");
		}
	}

	public static class ContentEditorExtensions
	{
		/// <summary>
		/// Изменяем Value по reference
		/// </summary>
		public static void Edit<T>(this ContentReference<T> reference, ContentEditing<T> editing)
			=> ContentEditor.Edit(reference, editing);
	}
}
