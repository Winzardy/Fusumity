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