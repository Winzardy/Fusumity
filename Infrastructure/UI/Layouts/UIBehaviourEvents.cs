using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
	[InfoBox("Только для runtime", InfoMessageType.Warning)]
	public class UIBehaviourEvents : UIBehaviour
	{
		public event Action RectTransformDimensionsChanged;
		public event Action BeforeTransformParentChanged;
		public event Action TransformParentChanged;
		public event Action DidApplyAnimationProperties;
		public event Action CanvasGroupChanged;

		//Если нужно добавить Override, сделайте новый метод OnRectTransformDimensionsChange => OnRectTransformDimensionsChanged
		protected sealed override void OnRectTransformDimensionsChange() => RectTransformDimensionsChanged?.Invoke();
		protected sealed override void OnBeforeTransformParentChanged() => BeforeTransformParentChanged?.Invoke();
		protected sealed override void OnTransformParentChanged() => TransformParentChanged?.Invoke();
		protected sealed override void OnDidApplyAnimationProperties() => DidApplyAnimationProperties?.Invoke();
		protected sealed override void OnCanvasGroupChanged() => CanvasGroupChanged?.Invoke();

		// Только для runtime
#if UNITY_EDITOR
		protected override void Awake()
		{
			base.Awake();

			hideFlags = HideFlags.DontSave;
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			UnityEditor.EditorApplication.delayCall += HandleDelayCall;

			void HandleDelayCall()
			{
				if (this == null)
					return;

				if (!Application.isPlaying)
					DestroyImmediate(this, true);
			}
		}
#endif
	}
}
