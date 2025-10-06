using System;
using Sapientia;
using Sapientia.Extensions;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentEntryScriptableObject<T> : ContentEntryScriptableObject,
		IIdentifierSource<ScriptableContentEntry<T>>, IUniqueContentEntryScriptableObject<T>
	{
		public bool useCustomId;

		// ReSharper disable once InconsistentNaming
		[SerializeField]
		private ScriptableContentEntry<T> _entry;

		protected ref readonly T Value => ref _entry.Value;

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

				return useCustomId ? _entry.Id : name;
			}
		}

		IScriptableContentEntry IContentEntryScriptableObject.ScriptableContentEntry
		{
			get
			{
				if (!useCustomId)
					_entry.SetId(Id);

				return _entry;
			}
		}

		public override IContentEntry Import(bool clone)
		{
			OnImport(ref _entry.ScriptableEditValue);

			if (!clone)
				return _entry;

			return _entry.Clone();
		}

		bool IUniqueContentEntryScriptableObject.UseCustomId => IsExternallyIdentifiable || useCustomId;

		IUniqueContentEntry IUniqueContentEntrySource.UniqueContentEntry => _entry;

		public Type ValueType => typeof(T);

		public ref readonly SerializableGuid Guid => ref _entry.Guid;

		IContentEntry<T> IContentEntrySource<T>.ContentEntry => _entry;

		IUniqueContentEntry<T> IUniqueContentEntrySource<T>.UniqueContentEntry => _entry;

		IContentEntry IContentEntrySource.ContentEntry => _entry;

		private bool IsExternallyIdentifiable => typeof(IExternallyIdentifiable).IsAssignableFrom(typeof(T));

		bool IContentEntrySource.Validate()
		{
#if UNITY_EDITOR
			if (NeedSync())
			{
				ContentDebug.LogWarning("Need sync!", this);
				return false;
			}
#endif
			if (Id.IsNullOrEmpty())
			{
				ContentDebug.LogError("Empty id!", this);
				return false;
			}

			if (Value is IValidatable validatable && !validatable.Validate())
			{
				ContentDebug.LogError("Value is not valid!", this);
				return false;
			}

			if (this is IValidatable soValidatable && !soValidatable.Validate())
			{
				ContentDebug.LogError("Scriptable Object is not valid!", this);
				return false;
			}

			return true;
		}

		protected virtual void OnImport(ref T value)
		{
		}

		public static implicit operator ContentReference<T>(ContentEntryScriptableObject<T> scriptableObject) =>
			scriptableObject ? new(in scriptableObject._entry.Guid) : new(SerializableGuid.Empty);
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

		public ref T EditValue => ref ScriptableContentEntry.EditValue;
	}

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
