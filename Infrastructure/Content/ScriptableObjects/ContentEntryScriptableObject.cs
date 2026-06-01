using JetBrains.Annotations;
using Sapientia;
using Sapientia.Extensions;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentEntryScriptableObject<T> : ContentEntryScriptableObject,
		IIdentifierSource<ScriptableContentEntry<T>>, IUniqueContentEntryScriptableObject<T>, IIdentifiable
	{
		private const string DEFAULT_ID_REGEX_PATTERN = @"^\d+_";

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
					ContentDebug.LogError($"Null entry by type [ {typeof(T).Name} ] in scriptable object [ {name}  ] by type [ {GetType().Name} ]", this);
					return string.Empty;
				}
#endif

				return useCustomId ? _entry.Id : GetDefaultId();
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

		public override Type ValueType => typeof(T);

		public ref readonly SerializableGuid Guid => ref _entry.Guid;

		IContentEntry<T> IContentEntrySource<T>.ContentEntry => _entry;

		IUniqueContentEntry<T> IUniqueContentEntrySource<T>.UniqueContentEntry => _entry;

		IContentEntry IContentEntrySource.ContentEntry => _entry;

		protected override IScriptableContentEntry BaseScriptableContentEntry => _entry;

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

		protected virtual void OnImport(ref T value)
		{
		}



		public static implicit operator ContentReference<T>(ContentEntryScriptableObject<T> scriptableObject) =>
			scriptableObject ? new(in scriptableObject._entry.Guid) : new(SerializableGuid.Empty);

		private string GetDefaultId()
		{
			var match = Regex.Match(name, DEFAULT_ID_REGEX_PATTERN);
			return match.Success
				? name[match.Length..]
				: name;
		}
	}

	public abstract partial class ContentEntryScriptableObject : ContentScriptableObject, IContentEntryScriptableObject
	{
		[HideInInspector]
		public bool enabled = true;

		public override bool Enabled { get => enabled; }

		public abstract Type ValueType { get; }
		IScriptableContentEntry IContentEntryScriptableObject.ScriptableContentEntry { get => BaseScriptableContentEntry; }

		protected abstract IScriptableContentEntry BaseScriptableContentEntry { get; }

		bool IContentEntryScriptableObject.enabled { get => enabled; set => enabled = value; }
	}

	public interface IUniqueContentEntryScriptableObject<T> : IContentEntryScriptableObject<T>, IUniqueContentEntrySource<T>,
		IUniqueContentEntryScriptableObject
	{
	}

	public interface IContentEntryScriptableObject<T> : IContentEntryScriptableObject, IContentEntrySource<T>
	{
		IScriptableContentEntry<T> ScriptableContentEntry { get; }

		ref T EditValue => ref ScriptableContentEntry.EditValue;
	}

	public interface IUniqueContentEntryScriptableObject : IContentEntryScriptableObject, IUniqueContentEntrySource
	{
		bool UseCustomId { get; }
	}

	public interface IContentEntryScriptableObject : IContentScriptableObject
	{
		[CanBeNull] IScriptableContentEntry ScriptableContentEntry { get; }
		Type ValueType { get; }

		internal bool enabled { get; set; }
	}

	public partial interface IContentScriptableObject
	{
	}
}
