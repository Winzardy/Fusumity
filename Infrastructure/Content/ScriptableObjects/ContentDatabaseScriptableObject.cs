using System;
using System.Collections.Generic;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentDatabaseScriptableObject<T> : ContentDatabaseScriptableObject, IContentEntryScriptableObject<T>
	{
		[SerializeField]
		private ScriptableSingleContentEntry<T> _entry;

		public IScriptableContentEntry<T> ScriptableContentEntry => _entry;

		protected ref readonly T Value => ref _entry.Value;

		public Type ValueType => typeof(T);

		IScriptableContentEntry IContentEntryScriptableObject.ScriptableContentEntry => _entry;

		public override IContentEntry Import(bool clone)
		{
			OnImport(ref _entry.ScriptableEditValue);

			if (!clone)
				return _entry;

			return _entry.Clone();
		}

		protected virtual void OnImport(ref T value)
		{
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
