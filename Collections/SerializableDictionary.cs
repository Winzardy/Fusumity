using System;
using System.Collections.Generic;
using Fusumity.Attributes.Specific;
using UnityEngine;

namespace Fusumity.Collections
{
	[Serializable]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
#if UNITY_EDITOR
		[SerializeField, Button("AddElement", drawBefore = false), RemoveFoldout]
		private KeyValue _newElement;
#endif
		[SerializeField, HideLabel]
		private List<KeyValue> _elements;

		public SerializableDictionary() : base() { }
		public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
		public SerializableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }
		public SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base(collection) { }
		public SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(collection, comparer) { }
		public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
		public SerializableDictionary(int capacity) : base(capacity) { }
		public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			if (_elements == null)
				_elements = new List<KeyValue>(Count);
			_elements.Clear();

			foreach (var pair in this)
			{
				_elements.Add(pair);
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			Clear();

			for (var i = 0; i < _elements.Count; i++)
			{
				TryAdd(_elements[i].key, _elements[i].value);
			}
		}

#if UNITY_EDITOR
		private void AddElement()
		{
			TryAdd(_newElement.key, _newElement.value);
		}
#endif

		[Serializable]
		public struct KeyValue
		{
			public TKey key;
			[SerializeReference, ReferenceSelection]
			public TValue value;

			public static implicit operator KeyValue(KeyValuePair<TKey, TValue> keyValue)
			{
				return new KeyValue
				{
					key = keyValue.Key,
					value = keyValue.Value,
				};
			}
		}
	}
}