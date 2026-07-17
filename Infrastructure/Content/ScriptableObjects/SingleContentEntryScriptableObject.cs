using System;
using Sapientia;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class SingleContentEntryScriptableObject<T> : SingleContentEntryScriptableObject,
		IContentEntryScriptableObject<T>
	{
		[SerializeField]
		private ScriptableSingleContentEntry<T> _entry;

		public IScriptableContentEntry<T> ScriptableContentEntry { get => _entry; }

		public ref readonly T Value { get => ref _entry.Value; }

		IScriptableContentEntry IContentEntryScriptableObject.ScriptableContentEntry { get => _entry; }

		public override Type ValueType { get => typeof(T); }

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

		bool IContentEntrySource.enabled { get => enabled; }
		IContentEntry<T> IContentEntrySource<T>.ContentEntry { get => _entry; }
		IContentEntry IContentEntrySource.ContentEntry { get => _entry; }
		protected override IScriptableContentEntry BaseScriptableContentEntry { get => _entry; }

		protected override bool OnValidated()
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

	public abstract class SingleContentEntryScriptableObject : ContentScriptableObject, IContentEntryScriptableObject
	{
		[HideInInspector]
		public bool enabled = true;

		public override bool Enabled { get => enabled; }

		public abstract Type ValueType { get; }
		IScriptableContentEntry IContentEntryScriptableObject.ScriptableContentEntry { get => BaseScriptableContentEntry; }

		protected abstract IScriptableContentEntry BaseScriptableContentEntry { get; }

		bool IContentEntrySource.enabled { get => enabled; }
		IContentEntry IContentEntrySource.ContentEntry { get => BaseScriptableContentEntry; }
		bool IContentEntrySource.Validate() => OnValidated();

		void IContentEntryScriptableObject.SetEnable(bool value)
		{
			enabled = value;
		}

		protected virtual bool OnValidated() => true;
	}
}
