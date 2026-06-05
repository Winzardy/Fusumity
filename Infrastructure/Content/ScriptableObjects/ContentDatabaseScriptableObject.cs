using System;
using System.Collections.Generic;
using Sapientia;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentDatabaseScriptableObject<T> : ContentDatabaseScriptableObject, IContentEntryScriptableObject<T>
	{
		[SerializeField]
		private ScriptableSingleContentEntry<T> _entry;

		public IScriptableContentEntry<T> ScriptableContentEntry => _entry;

		protected ref readonly T Value => ref _entry.Value;

		public override Type ValueType => typeof(T);

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

		protected override IScriptableContentEntry BaseScriptableContentEntry => _entry;

		bool IContentEntrySource.Validate()
		{
#if UNITY_EDITOR
			if (NeedSync())
			{
				ContentDebug.LogWarning("Need sync!", this);
				return false;
			}
#endif
			if (Value is IValidatable validatable && !validatable.Validate(out var message))
			{
				ContentDebug.LogError($"Value is not valid! (error: {message})", this);
				return false;
			}

			if (this is IValidatable soValidatable && !soValidatable.Validate(out var soMessage))
			{
				ContentDebug.LogError($"Scriptable Object is not valid! (error: {soMessage})", this);
				return false;
			}

			return true;
		}
	}

	public abstract class ContentDatabaseScriptableObject : SingleContentEntryScriptableObject
	{
		public const string ADDRESSABLE_DATABASE_LABEL = "database";

		[Tooltip("Используется для сортировки при импорте")]
		public Toggle<int> priority;

		public List<ContentScriptableObject> scriptableObjects;

		public override bool Enabled { get => true; }

		protected override IScriptableContentEntry BaseScriptableContentEntry { get => null; }
		public override Type ValueType { get => null; }

		public virtual void OnUpdateContent()
		{
		}

		public void Sort() => scriptableObjects.Sort(SortByCreationTime);

		public void Cleanup()
		{
			for (int i = scriptableObjects.Count - 1; i >= 0; i--)
			{
				if (scriptableObjects[i] == null)
					scriptableObjects.RemoveAt(i);
			}
		}

		private static int SortByCreationTime(ContentScriptableObject x, ContentScriptableObject y)
			=> x.CreationTime.CompareTo(y.CreationTime);
	}
}
