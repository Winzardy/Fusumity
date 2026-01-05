using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fusumity.Collections
{
	public interface ISerializableDictionary : IDictionary
	{
		/// <summary>
		/// Длина сериализованного списка
		/// </summary>
		public int Length { get; }

		/// <summary>
		/// Синхронизирует сериализованное представление со значениями из runtime-кеша словаря
		/// </summary>
		public void Sync();
	}

	[Serializable]
	public class SerializableReferenceDictionary<TKey, TValue> : SerializableDictionary<TKey, TValue, KeyReferenceValue<TKey, TValue>>
		where TValue : class
	{
		public SerializableReferenceDictionary() : base()
		{
		}

		public SerializableReferenceDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
		{
		}

		public SerializableReferenceDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary,
			comparer)
		{
		}

		public SerializableReferenceDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base(collection)
		{
		}

		public SerializableReferenceDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(
			collection, comparer)
		{
		}

		public SerializableReferenceDictionary(IEqualityComparer<TKey> comparer) : base(comparer)
		{
		}

		public SerializableReferenceDictionary(int capacity) : base(capacity)
		{
		}

		public SerializableReferenceDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer)
		{
		}
	}

	[Serializable]
	public class SerializableDictionary<TKey, TValue> : SerializableDictionary<TKey, TValue, KeyValue<TKey, TValue>>
	{
		public SerializableDictionary() : base()
		{
		}

		public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
		{
		}

		public SerializableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer)
		{
		}

		public SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base(collection)
		{
		}

		public SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(
			collection, comparer)
		{
		}

		public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer)
		{
		}

		public SerializableDictionary(int capacity) : base(capacity)
		{
		}

		public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer)
		{
		}
	}

	[Serializable]
	public abstract partial class SerializableDictionary<TKey, TValue, TKeyValue> : Dictionary<TKey, TValue>,
		ISerializableDictionary,
		ISerializationCallbackReceiver
		where TKeyValue : struct, IKeyValue<TKey, TValue>
	{
#if NEWTONSOFT
		[Newtonsoft.Json.JsonIgnore]
#endif
		[SerializeField, HideInInspector]
		protected TKeyValue[] elements;

		public int Length => elements?.Length ?? 0;

		protected SerializableDictionary() : base()
		{
		}

		protected SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
		{
		}

		protected SerializableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary,
			comparer)
		{
		}

		protected SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base(collection)
		{
		}

		protected SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(
			collection, comparer)
		{
		}

		protected SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer)
		{
		}

		protected SerializableDictionary(int capacity) : base(capacity)
		{
		}

		protected SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer)
		{
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			Sync();
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			Clear();

			elements ??= new TKeyValue[Count];

			for (var i = 0; i < elements.Length; i++)
				TryAdd(elements[i].Key, elements[i].Value);
		}

		void ISerializableDictionary.Sync() => Sync();

		private void Sync()
		{
			if (elements == null || elements.Length != Count)
				elements = new TKeyValue[Count];

			var i = 0;
			foreach (var pair in this)
			{
				var keyValue = default(TKeyValue);
				keyValue.Key = pair.Key;
				keyValue.Value = pair.Value;

				elements[i++] = keyValue;
			}
		}
	}

	[Serializable]
	public struct KeyValue<TKey, TValue> : IKeyValue<TKey, TValue>
	{
		public TKey key;
		public TValue value;

		public TKey Key
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => key;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => key = value;
		}

		public TValue Value
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => this.value = value;
		}

		public static implicit operator (TKey, TValue)(KeyValue<TKey, TValue> keyValue)
		{
			return (keyValue.Key, keyValue.Value);
		}
	}

	[Serializable]
	public struct KeyReferenceValue<TKey, TValue> : IKeyValue<TKey, TValue>
		where TValue : class
	{
		public TKey key;

		[SerializeReference]
		public TValue value;

		public TKey Key
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => key;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => key = value;
		}

		public TValue Value
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => this.value = value;
		}
	}

	public interface IKeyValue<TKey, TValue>
	{
		public TKey Key { get; set; }
		public TValue Value { get; set; }
	}
}
