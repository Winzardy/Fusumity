using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sapientia.Collections;
using UnityEngine;

namespace Fusumity.Collections
{
	public interface ISerializableDictionary : IDictionary
	{
		/// <summary>
		/// Синхронизирует сериализованное представление со значениями из runtime-кеша словаря
		/// </summary>
		public void Sync();

		/// <summary>
		/// Проверяет, совпадают ли сериализованное представление
		/// и runtime-содержимое словаря (ключи и значения)
		/// </summary>
		public bool NeedSync();
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
	public abstract partial class SerializableDictionary<TKey, TValue, TKeyValuePair> : Dictionary<TKey, TValue>,
		ISerializableDictionary,
		ISerializationCallbackReceiver
		where TKeyValuePair : struct, IKeyValuePair<TKey, TValue>
	{
#if NEWTONSOFT
		[Newtonsoft.Json.JsonIgnore]
#endif
		[SerializeField, HideInInspector]
		protected TKeyValuePair[] elements;

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
			base.Clear();

			elements ??= new TKeyValuePair[Count];

			for (var i = 0; i < elements.Length; i++)
				TryAdd(elements[i].Key, elements[i].Value);
		}

		void ISerializableDictionary.Sync() => Sync();

		bool ISerializableDictionary.NeedSync()
		{
			if (elements == null)
				return true;

			if (elements.Length != Count)
				return true;

			for (int i = 0; i < elements.Length; i++)
			{
				ref readonly var pair = ref elements[i];

				if (!TryGetValue(pair.Key, out var runtimeValue))
					return true;

				if (!EqualityComparer<TValue>.Default.Equals(pair.Value, runtimeValue))
					return true;
			}

			return false;
		}

		private void Sync()
		{
			if (elements == null || elements.Length != Count)
				elements = new TKeyValuePair[Count];

			var i = 0;
			foreach (var pair in this)
			{
				var keyValue = default(TKeyValuePair);
				keyValue.Key = pair.Key;
				keyValue.Value = pair.Value;

				elements[i++] = keyValue;
			}
		}

		public new void Clear()
		{
			base.Clear();
			Sync();
		}

		public new bool TryAdd(TKey key, TValue value)
		{
			if (base.TryAdd(key, value))
			{
				AddToArrayInternal(key, value);
				return true;
			}

			return false;
		}

		public new void Add(TKey key, TValue value)
		{
			base.Add(key, value);
			AddToArrayInternal(key, value);
		}

		public new bool Remove(TKey key)
		{
			if (base.Remove(key))
			{
				var selectedIndex = 0;
				for (int i = 0; i < elements.Length; i++)
				{
					if (!Equals(elements[i].Key, key))
						continue;
					selectedIndex = i;
					break;
				}

				elements = elements.RemoveAt(selectedIndex);
				return true;
			}

			return false;
		}

		private void AddToArrayInternal(TKey key, TValue value)
		{
			var keyValue = default(TKeyValuePair);
			keyValue.Key = key;
			keyValue.Value = value;
			elements = elements.Add(keyValue);
		}
	}

	[Serializable]
	public struct KeyValue<TKey, TValue> : IKeyValuePair<TKey, TValue>
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
	public struct KeyReferenceValue<TKey, TValue> : IKeyValuePair<TKey, TValue>
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

	public interface IKeyValuePair<TKey, TValue>
	{
		public TKey Key { get; set; }
		public TValue Value { get; set; }
	}
}
