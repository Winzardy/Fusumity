using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace UI
{
	//TODO: подчистить чтобы блок с Анимациями был отдельно, возможно будут проблемы из-за потери уже существующих серилизованных полей...
	[RequireComponent(typeof(RectTransform))]
	public abstract partial class UIBaseLayout : UIBehaviour
	{
		[FormerlySerializedAs("root")]
		public RectTransform rectTransform;

#if UNITY_EDITOR
		[Tooltip("Ссылка на изначальный префаб (only editor)")]
		[NonSerialized]
		public Object prefab;
#endif

		[ContextMenu("Reset Transform")]
		protected new virtual void Reset()
		{
			ResetRectTransform();

			#region Debug

#if UNITY_EDITOR
			//На всякий...
			var i = 20;
			while (!IsValidPosition() && i >= 0)
			{
				ComponentUtility.MoveComponentUp(this);
				i--;
			}

			bool IsValidPosition()
			{
				var components = GetComponents<Component>();
				//0 is Transform

				if (components[1] is CanvasRenderer)
					return components[2] == this;

				return components[1] == this;
			}
#endif

			#endregion
		}

		private void ResetRectTransform()
		{
			rectTransform = transform as RectTransform;
#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		protected internal new virtual void OnValidate()
		{
			if (!rectTransform || rectTransform.gameObject != gameObject)
			{
				rectTransform = transform as RectTransform;
#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
#endif
			}

			openingSequence?.Validate(gameObject);
			closingSequence?.Validate(gameObject);
		}

		#region UI Behaviour

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

		#endregion
	}
}
