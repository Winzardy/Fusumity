#if !UNITY_EDITOR && CLIENT
#define CONTENT_ENTRY_BUFFER
#endif
using System;
using Content.Management;
using UnityEngine;

namespace Content
{
	public sealed partial class ContentEntry<T> : ISerializationCallbackReceiver
	{
		public void SetValue(in T newValue) => ContentEditValue = newValue;

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
#if CONTENT_ENTRY_BUFFER
			ContentEntryBuffer.Push(this);
#endif
		}
	}

	public partial struct ContentEntry<T, TFilter> : IFilteredContentEntry
	{
		IContentEntry IFilteredContentEntry.Entry => entry;
		Type IFilteredContentEntry.Type => typeof(TFilter);
		public void SetValue(object value) => entry.SetValue((T) value);
	}

	/// <summary>
	/// Нужно в основном для редактора, чтобы ограничить выбор типов в инспекторе!
	/// </summary>
	public interface IFilteredContentEntry
	{
		public IContentEntry Entry { get; }

		public Type Type { get; }

		public void SetValue(object value);
	}
}
