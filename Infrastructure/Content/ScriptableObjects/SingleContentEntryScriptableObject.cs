using System;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class SingleContentEntryScriptableObject<T> : SingleContentEntryScriptableObject, IContentEntryScriptableObject<T>
	{
		[SerializeField]
		private ScriptableSingleContentEntry<T> _entry;

		public IScriptableContentEntry<T> ScriptableContentEntry => _entry;

		public ref readonly T Value => ref _entry.Value;

		IScriptableContentEntry IContentEntryScriptableObject.ScriptableContentEntry
		{
			get
			{
				OnImport();
				return _entry;
			}
		}

		public Type ValueType => typeof(T);

		protected virtual void OnImport()
		{
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

	public abstract class SingleContentEntryScriptableObject : ContentScriptableObject
	{
	}
}
