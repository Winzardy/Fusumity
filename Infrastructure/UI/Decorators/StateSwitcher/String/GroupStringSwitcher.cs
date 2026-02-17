using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Utility;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public class GroupStringSwitcher : GroupStateSwitcher<string>
	{
#if UNITY_EDITOR
		public override IEnumerable<object> GetVariants()
		{
			if (!_allCollectedStates.IsNullOrEmpty())
			{
				foreach (var state in _allCollectedStates)
					yield return state;
			}

			if (_variants.IsNullOrEmpty())
				yield break;

			foreach (var variant in _variants)
			{
				if (!_allCollectedStates.IsNullOrEmpty() && _allCollectedStates.ContainsElement(variant))
					continue;

				yield return variant;
			}
		}

		[NonSerialized, ReadOnly]
		[OnInspectorInit(nameof(CollectStates))]
		[LabelText("All Collected States (editor only)")]
		private string[] _allCollectedStates;

		private void CollectStates()
		{
			if (_group.IsNullOrEmpty())
				return;

			var states = new HashSet<string>();
			foreach (var switcher in _group)
			{
				if (switcher == null)
					continue;

				if (!switcher.TryFindFieldRecursively("_dictionary", out var dictField, BindingFlags.Instance | BindingFlags.NonPublic))
					continue;

				var dictionary = dictField.GetValue(switcher) as IDictionary;

				if (dictionary == null)
					continue;

				foreach (var key in dictionary.Keys)
				{
					if (key is string s)
					{
						states.Add(s);
					}
				}
			}

			_allCollectedStates = states.ToArray();
		}

		[SerializeField]
		[Tooltip("Варианты для dropdown (только редактор)")]
		private string[] _variants;
#endif
	}
}
