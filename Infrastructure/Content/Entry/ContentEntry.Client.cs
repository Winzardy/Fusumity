#if !UNITY_EDITOR && CLIENT
#define CONTENT_ENTRY_BUFFER
#endif
using System;
#if CLIENT
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace Content
{
	public sealed partial class ContentEntry<T> : ISerializationCallbackReceiver
	{
		// [ClientOnly]
		// [HideInInspector]
		// [SerializeField]
		// private long timeCreated;

		public void SetValue(in T newValue) => ContentEditValue = newValue;

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			// if (timeCreated == 0)
			// 	timeCreated = DateTime.UtcNow.Ticks;
			//
			// //ContentDebug.LogError(guid);

#if CONTENT_ENTRY_BUFFER
			ContentEntryBuffer.Add(this);
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
		IContentEntry Entry { get; }

		Type Type { get; }

		void SetValue(object value);
	}
}
