using System;
using System.Collections.Generic;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Расширяет Button
	/// </summary>
	public sealed class CustomButton : Button
	{
		//TODO: удалить после полного обновления UI

		#region Legacy

		[FoldoutGroup("Legacy"), Button]
		public void ClearLegacyTransitions()
		{
			spriteStateTransitions = new SpriteStateTransition[0];
			colorTintTransitions = new ColorTintTransition[0];

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		[FoldoutGroup("Legacy"), ReadOnly, Obsolete("Используйте ImageSpriteButtonTransition")]
		public SpriteStateTransition[] spriteStateTransitions = new SpriteStateTransition[0];

		[FoldoutGroup("Legacy"), ReadOnly, Obsolete("Используйте GraphicColorButtonTransition")]
		public ColorTintTransition[] colorTintTransitions = new ColorTintTransition[0];

		#endregion

		/// <summary>
		/// Пересобирает список transitions при Awake
		/// </summary>
		public bool refreshOnAwake = true;

		public List<ButtonTransition> transitions = new();

		protected override void Awake()
		{
			base.Awake();

			if (refreshOnAwake)
				Refresh();
		}

		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			base.DoStateTransition(state, instant);

			if (!gameObject.activeInHierarchy)
				return;

			//TODO: удалить после полного обновления UI
			for (int i = 0; i < spriteStateTransitions.Length; i++)
				DoSpriteStateTransition(state, in spriteStateTransitions[i]);

			for (int i = 0; i < colorTintTransitions.Length; i++)
				DoColorStateTransition(state, instant, in colorTintTransitions[i]);

			for (int i = 0; i < transitions.Count; i++)
				transitions[i].DoStateTransition(state.ToInt(), instant);
		}

		private void DoSpriteStateTransition(SelectionState state, in SpriteStateTransition transition)
		{
			if (!transition.image)
				return;

			var sprite = state switch
			{
				SelectionState.Normal => null,
				SelectionState.Highlighted => transition.spriteState.highlightedSprite,
				SelectionState.Pressed => transition.spriteState.pressedSprite,
				SelectionState.Selected => transition.spriteState.selectedSprite,
				SelectionState.Disabled => transition.spriteState.disabledSprite,
				_ => null
			};

			transition.image.overrideSprite = sprite;
		}

		private void DoColorStateTransition(SelectionState state, bool instant, in ColorTintTransition transition)
		{
			if (!transition.graphic)
				return;

			var tintColor = state switch
			{
				SelectionState.Normal => transition.block.normalColor,
				SelectionState.Highlighted => transition.block.highlightedColor,
				SelectionState.Pressed => transition.block.pressedColor,
				SelectionState.Selected => transition.block.selectedColor,
				SelectionState.Disabled => transition.block.disabledColor,
				_ => Color.black
			};

			var targetColor = tintColor * transition.block.colorMultiplier;

			transition.graphic.CrossFadeColor(targetColor, instant ? 0f : transition.block.fadeDuration, true, true);
		}

		[ContextMenu("Refresh")]
		public void Refresh()
		{
			var children = gameObject.GetComponentsInChildren<ButtonTransition>(true);

			foreach (var child in children)
			{
				if (!transitions.Contains(child))
					transitions.Add(child);
			}

			transitions.RemoveAll(item => !item);

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		private ButtonTransitionState _target;

		[PropertySpace(10), Button]
		private void DoStateTransition(ButtonTransitionState state, bool instant = false)
		{
			if (_target == state)
				_target = ButtonTransitionType.NORMAL;
			else
				_target = state;

			DoStateTransition((SelectionState) _target.type, instant);
		}
	}

	[Serializable]
	[Obsolete("Используйте ImageSpriteButtonTransition")]
	public class SpriteStateTransition
	{
		public Image image;

		[BoxGroup]
		public SpriteState spriteState;
	}

	[Serializable]
	[Obsolete("Используйте GraphicColorButtonTransition")]
	public class ColorTintTransition
	{
		public Graphic graphic;
		public ColorBlock block = ColorBlock.defaultColorBlock;
	}
}
