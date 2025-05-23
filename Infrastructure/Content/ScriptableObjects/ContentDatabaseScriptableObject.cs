using System;
using System.Collections.Generic;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract class ContentDatabaseScriptableObject<T> : ContentDatabaseScriptableObject, IContentEntryScriptableObject<T>
	{
		[SerializeField]
		private ScriptableSingleContentEntry<T> _entry;

		public IScriptableContentEntry<T> ScriptableContentEntry => _entry;

		protected ref readonly T Value => ref _entry.Value;

		public Type ValueType => typeof(T);

		IScriptableContentEntry IContentEntryScriptableObject.ScriptableContentEntry
		{
			get
			{
				OnImport();
				return _entry;
			}
		}

		protected virtual void OnImport()
		{
		}

		private void OnValidate()
		{
			_entry.scriptableObject = this;
		}

		IContentEntry<T> IContentEntrySource<T>.ContentEntry => _entry;
		IContentEntry IContentEntrySource.ContentEntry => _entry;

		public void SetValue(T value) => _entry.SetValue(value);
		public void Edit(ContentEditing<T> editing)
		{
			var value = _entry.Value;
			editing(ref value);
			_entry.SetValue(value);
		}
	}

	public abstract class ContentDatabaseScriptableObject : SingleContentEntryScriptableObject
	{
		public const string LABEL = "database";

		public List<ContentScriptableObject> scriptableObjects;

		public virtual void OnUpdateContent()
		{
		}
	}
}
