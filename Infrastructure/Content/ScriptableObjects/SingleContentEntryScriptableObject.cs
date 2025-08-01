﻿using System;
using Sapientia;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class SingleContentEntryScriptableObject<T> : SingleContentEntryScriptableObject,
		IContentEntryScriptableObject<T>
	{
		[SerializeField]
		private ScriptableSingleContentEntry<T> _entry;

		public IScriptableContentEntry<T> ScriptableContentEntry => _entry;

		public ref readonly T Value => ref _entry.Value;

		IScriptableContentEntry IContentEntryScriptableObject.ScriptableContentEntry => _entry;

		public Type ValueType => typeof(T);

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

		bool IValidatable.Validate()
		{
#if UNITY_EDITOR
			if (NeedSync())
			{
				ContentDebug.LogWarning("Need sync!", this);
				return false;
			}
#endif
			if (Value is IValidatable validatable && !validatable.Validate())
			{
				ContentDebug.LogError("Value is not valid!", this);
				return false;
			}

			return true;
		}
	}

	public abstract class SingleContentEntryScriptableObject : ContentScriptableObject
	{
	}
}
