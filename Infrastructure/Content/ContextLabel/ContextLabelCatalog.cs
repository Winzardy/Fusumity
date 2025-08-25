using System;
using System.Collections.Generic;
using Fusumity.Collections;
using UnityEngine;

namespace Content.ContextLabel
{
	public interface IContextLabelCatalog
	{
		public Type Type { get; }
	}

	// TODO: добавить подсветку одинаковых label's
	[Serializable]
	public struct ContextLabelCatalog<TKey> : IContextLabelCatalog
	{
		[SerializeField]
		private SerializableDictionary<TKey, string> _keyToLabel;

		public readonly Type Type => typeof(TKey);
		public readonly int Count => _keyToLabel.Count;

		public readonly string this[TKey key] => _keyToLabel[key];

		public readonly bool TryGet(TKey key, out string value) => _keyToLabel.TryGetValue(key, out value);

		public readonly bool Contains(TKey key) => _keyToLabel?.ContainsKey(key) ?? false;

		public readonly IEnumerable<TKey> GetKeys() => _keyToLabel.Keys;

		public static implicit operator SerializableDictionary<TKey, string>(in ContextLabelCatalog<TKey> contextLabel) =>
			contextLabel._keyToLabel;
	}
}
