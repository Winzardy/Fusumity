using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public interface IStateSwitcher
	{
		GameObject gameObject { get; }
		string Name { get; }
		Type StateType { get; }
		[CanBeNull] IStateSwitcher Parent { get; }

		[CanBeNull]
		IEnumerable<object> GetVariants();
	}

	/// <typeparam name="TState">
	/// Настоятельно рекомендуется использовать:<br/>
	/// <c>bool</c>, <c>int</c>, <c>string</c>
	/// </typeparam>
	public abstract class StateSwitcher<TState> : MonoBehaviour, IStateSwitcher
	{
		protected bool _immediate;

		[NonSerialized]
		[PropertyOrder(-2), ReadOnly, ShowInInspector, ShowIf(nameof(_parent), null)]
		private StateSwitcher<TState> _parent;

		[HideInInspector]
		[SerializeField]
		protected TState current;

		[ShowInInspector, PropertyOrder(-1), DelayedProperty]
		public TState Current { get => current; set => Switch(value); }

		public string Name { get => gameObject.name; }
		public Type StateType { get => typeof(TState); }
		public IStateSwitcher Parent { get => _parent; }
		protected virtual bool UseEquals { get => false; }

		protected abstract void OnStateSwitched(TState state);

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

		internal void SetParent([CanBeNull] StateSwitcher<TState> parent) => _parent = parent;

		internal void ClearParent(IStateSwitcher parent)
		{
			if (ReferenceEquals(_parent, parent))
				_parent = null;
		}
	}

	public static class StateSwitcherExtensions
	{
		public static void Switch<TSwitcher, TEnum>(this TSwitcher switcher, TEnum value, bool immediate = false)
			where TSwitcher : StateSwitcher<string>
			where TEnum : struct, Enum
		{
			switcher.Switch(value.ToString(), immediate);
		}
	}
}
