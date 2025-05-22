#if CLIENT
using System;
using Sapientia;
using Sapientia.Extensions;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentEntryScriptableObject<T> : ContentEntryScriptableObject,
		IIdentifierSource<ScriptableContentEntry<T>>,
		IValidatable, IUniqueContentEntryScriptableObject<T>
	{
		public bool useCustomId;

		// ReSharper disable once InconsistentNaming
		[SerializeField]
		private ScriptableContentEntry<T> _entry;

		public ref readonly T Value => ref _entry.Value;

		public IScriptableContentEntry<T> ScriptableContentEntry => _entry;

		ScriptableContentEntry<T> IIdentifierSource<ScriptableContentEntry<T>>.Source => _entry;

		public string Id
		{
			get
			{
#if UNITY_EDITOR
				if (_entry == null)
				{
					var nullException =
						ContentDebug.NullException(
							$"Null entry by type [ {typeof(T).Name} ] in scriptable object [ {name}  ] by type [ {GetType().Name} ]");
					ContentDebug.LogException(nullException, this);
					throw nullException;
				}
#endif

				return useCustomId ? _entry.id : name;
			}
		}

		IScriptableContentEntry IContentEntryScriptableObject.ScriptableContentEntry
		{
			get
			{
				if (!useCustomId)
					_entry.id = Id;

				OnImport();
				return _entry;
			}
		}

		bool IUniqueContentEntryScriptableObject.UseCustomId => useCustomId;
		IUniqueContentEntry IUniqueContentEntrySource.UniqueContentEntry => _entry;
		public Type ValueType => typeof(T);

		public ref readonly SerializableGuid Guid => ref _entry.Guid;

		protected virtual void OnImport()
		{
		}

		IContentEntry<T> IContentEntrySource<T>.ContentEntry => _entry;
		IUniqueContentEntry<T> IUniqueContentEntrySource<T>.UniqueContentEntry => _entry;
		IContentEntry IContentEntrySource.ContentEntry => _entry;

		public virtual bool Validate()
		{
#if UNITY_EDITOR
			ForceUpdateEntry();
#endif
			if (Id.IsNullOrEmpty())
			{
				ContentDebug.LogError("Empty id!", this);
				return false;
			}

			return true;
		}

		public static implicit operator ContentReference<T>(ContentEntryScriptableObject<T> scriptableObject) =>
			scriptableObject ? new(in scriptableObject._entry.Guid) : new(SerializableGuid.Empty);

		public void SetValue(T value) => _entry.SetValue(value);

		public void Edit(ContentEditing<T> editing)
		{
			var value = _entry.Value;
			editing(ref value);
			SetValue(value);
		}
	}

	public abstract partial class ContentEntryScriptableObject : ContentScriptableObject
	{
	}

	public interface IUniqueContentEntryScriptableObject<T> : IContentEntryScriptableObject<T>, IUniqueContentEntrySource<T>,
		IUniqueContentEntryScriptableObject
	{
	}

	public interface IContentEntryScriptableObject<T> : IContentEntryScriptableObject, IContentEntrySource<T>
	{
		public IScriptableContentEntry<T> ScriptableContentEntry { get; }

		public void Edit(ContentEditing<T> editing);
	}

	public delegate void ContentEditing<T>(ref T value);

	public interface IUniqueContentEntryScriptableObject : IContentEntryScriptableObject, IUniqueContentEntrySource
	{
		public bool Enabled { get; }
		public bool UseCustomId { get; }
	}

	public interface IContentEntryScriptableObject : IContentScriptableObject
	{
		public IScriptableContentEntry ScriptableContentEntry { get; }
		public Type ValueType { get; }
	}

	public partial interface IContentScriptableObject
	{
	}
}
#endif
