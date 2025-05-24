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

		IScriptableContentEntry IContentEntryScriptableObject.ScriptableContentEntry => _entry;

		public override IScriptableContentEntry Import()
		{
			OnImport(ref _entry.ScriptableEditValue);
			return _entry;
		}

		protected virtual void OnImport(ref T value)
		{
		}

		protected override void OnValidate()
		{
			base.OnValidate();
			_entry.scriptableObject = this;
		}

		IContentEntry<T> IContentEntrySource<T>.ContentEntry => _entry;
		IContentEntry IContentEntrySource.ContentEntry => _entry;
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
