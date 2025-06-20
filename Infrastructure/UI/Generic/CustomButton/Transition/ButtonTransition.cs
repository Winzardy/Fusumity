using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
	public abstract class ButtonTransition : MonoBehaviour
	{
		public abstract void DoStateTransition(int state, bool instant);
	}

	/// <inheritdoc cref="ButtonTransitionState.type"/>
	public static class ButtonTransitionType
	{
		public const int UNDEFINED = -1;

		public const int NORMAL = 0;
		public const int HIGHLIGHTED = 1;
		public const int PRESSED = 2;
		public const int SELECTED = 3;
		public const int DISABLED = 4;
	}

	//TODO: нужно переделать в StateSwitcher, а может и не нужно, позже подумаю...
	/// <summary>
	/// С радостью бы использовал enum, но юнитеки его protected...
	/// <see cref="UnityEngine.UI.Selectable.SelectionState"/>
	/// </summary>
	[Serializable]
	[Obsolete("Нужно переделать в StateSwitcher<int>...")]
	public struct ButtonTransitionState : IEquatable<ButtonTransitionState>
	{
		/// <summary>
		/// <see cref="UnityEngine.UI.Selectable.SelectionState"/>
		/// </summary>
		public int type;

		public ButtonTransitionState(int type) => this.type = type;

		public static implicit operator int(ButtonTransitionState state) => state.type;
		public static implicit operator ButtonTransitionState(int type) => new(type);

		public static implicit operator bool(ButtonTransitionState state) =>
			state.type >= 0;

		public override int GetHashCode() => type;

		public override string ToString() => ButtonTransitionUtility.ToLabel(type);

		public static bool operator ==(ButtonTransitionState x, ButtonTransitionState y) => x.type == y.type;

		public static bool operator !=(ButtonTransitionState x, ButtonTransitionState y) => !(x == y);

		public bool Equals(ButtonTransitionState other) => type == other.type;

		public override bool Equals(object obj) => obj is ButtonTransitionState other && Equals(other);
	}

	public static class ButtonTransitionUtility
	{
		public static IEnumerable<ButtonTransitionState> GetAll()
		{
			yield return ButtonTransitionType.NORMAL;
			yield return ButtonTransitionType.HIGHLIGHTED;
			yield return ButtonTransitionType.PRESSED;
			yield return ButtonTransitionType.SELECTED;
			yield return ButtonTransitionType.DISABLED;
		}

		public static string ToLabel(this in ButtonTransitionState state) =>
			state.type switch
			{
				0 => "Normal",
				1 => "Highlighted",
				2 => "Pressed",
				3 => "Selected",
				4 => "Disabled",
				_ => "None"
			};
	}
}
