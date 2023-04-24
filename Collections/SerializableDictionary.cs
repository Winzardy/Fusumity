using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fusumity.Attributes.Specific;
using Newtonsoft.Json;
using UnityEngine;

namespace Fusumity.Collections
{
	[Serializable]
	public class SerializableReferenceDictionary<TKey, TValue> : SerializableDictionary<TKey, TValue, KeyReferenceValue<TKey, TValue>>
		where TValue : class
	{
		public SerializableReferenceDictionary() : base() { }
		public SerializableReferenceDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
		public SerializableReferenceDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }
		public SerializableReferenceDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base(collection) { }
		public SerializableReferenceDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(collection, comparer) { }
		public SerializableReferenceDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
		public SerializableReferenceDictionary(int capacity) : base(capacity) { }
		public SerializableReferenceDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }
	}

	[Serializable]
	public class SerializableDictionary<TKey, TValue> : SerializableDictionary<TKey, TValue, KeyValue<TKey, TValue>>
	{
		public SerializableDictionary() : base() { }
		public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
		public SerializableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }
		public SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base(collection) { }
		public SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(collection, comparer) { }
		public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
		public SerializableDictionary(int capacity) : base(capacity) { }
		public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }
	}

	[Serializable]
	public abstract class SerializableDictionary<TKey, TValue, TKeyValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
		where TKeyValue : struct, IKeyValue<TKey, TValue>
	{
#if UNITY_EDITOR
		[SerializeField, Button("AddElement", drawBefore = false), RemoveFoldout, JsonIgnore]
		private TKeyValue _newElement;
#endif
		[SerializeField, HideLabel, JsonIgnore]
		private List<TKeyValue> _elements;

		protected SerializableDictionary() : base() { }
		protected SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
		protected SerializableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }
		protected SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base(collection) { }
		protected SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(collection, comparer) { }
		protected SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
		protected SerializableDictionary(int capacity) : base(capacity) { }
		protected SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			if (_elements == null)
				_elements = new List<TKeyValue>(Count);
			_elements.Clear();

			foreach (var pair in this)
			{
				var keyValue = default(TKeyValue);
				keyValue.Key = pair.Key;
				keyValue.Value = pair.Value;

				_elements.Add(keyValue);
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			Clear();

			for (var i = 0; i < _elements.Count; i++)
			{
				TryAdd(_elements[i].Key, _elements[i].Value);
			}
		}

#if UNITY_EDITOR
		private void AddElement()
		{
			TryAdd(_newElement.Key, _newElement.Value);
		}
#endif
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
	}

	[Serializable]
	public struct KeyReferenceValue<TKey, TValue> : IKeyValue<TKey, TValue>
		where TValue : class
	{
		public TKey key;
		[SerializeReference, ReferenceSelection]
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