using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.Bridge
{
	public enum BridgeMode
	{
		Single,
		Group
	}

	public abstract class BridgeStateSwitcher<TInputState, TOutputState> : StateSwitcher<TInputState>, IBridgeStateSwitcher
	{
		public Type InputType { get => typeof(TInputState); }
		public Type OutputType { get => typeof(TOutputState); }

		private BridgeMode _mode;

		[FormerlySerializedAs("_switcher")]
		[SerializeField]
		[NotNull]
		private StateSwitcher<TOutputState> _single;

		[SerializeField]
		[NotNull]
		public List<StateSwitcher<TOutputState>> _group;

		[SerializeField]
		private TOutputState _default;

		[SerializeField]
		private SerializableDictionary<TInputState, TOutputState> _dictionary;

		protected override void OnStateSwitched(TInputState state)
		{
			var linkedState = _dictionary.GetValueOrDefaultSafe(state, _default);
			switch (_mode)
			{
				case BridgeMode.Single:
					_single.Switch(linkedState);
					break;
				case BridgeMode.Group:
					foreach (var switcher in _group)
						switcher.Switch(linkedState);
					break;
			}
		}
	}

	public interface IBridgeStateSwitcher
	{
		public Type InputType { get; }
		public Type OutputType { get; }
	}
}
