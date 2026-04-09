using System;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace UI
{
	public interface IStateSwitcher
	{
		Type StateType { get; }

		[CanBeNull]
		IEnumerable<object> GetVariants();
	}

	// Похоже что это можно вообще вынести вне UI... ладно пока пусть будет тут
	public abstract class StateSwitcher<TState> : MonoBehaviour, IStateSwitcher
	{
		protected bool _immediate;

		[HideInInspector]
		[SerializeField]
		protected TState current;

		[ShowInInspector, PropertyOrder(-1), DelayedProperty]
		public TState Current { get => current; set => Switch(value); }

		public Type StateType => typeof(TState);

		protected abstract void OnStateSwitched(TState state);

		protected virtual bool UseEquals => false;

		public void Switch(TState value, bool immediate = false)
		{
			_immediate = immediate;

			if (UseEquals && !immediate &&
				EqualityComparer<TState>.Default.Equals(current, value))
				return;

			current = value;
			OnStateSwitched(current);
		}

		public virtual IEnumerable<object> GetVariants() => null;

		public virtual bool IsTransitioning() => false;
	}
}
