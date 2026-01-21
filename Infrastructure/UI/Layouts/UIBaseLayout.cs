using System;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace UI
{
	using UnityObject = UnityEngine.Object;

	//TODO: подчистить чтобы блок с Анимациями был отдельно, возможно будут проблемы из-за потери уже существующих серилизованных полей...
	[RequireComponent(typeof(RectTransform))]
	public abstract partial class UIBaseLayout : MonoBehaviour
	{
		private UIBehaviourEvents _events;

		[FormerlySerializedAs("root")]
		public RectTransform rectTransform;

#if UNITY_EDITOR
		[Tooltip("Ссылка на изначальный префаб (only editor)")]
		[NonSerialized]
		public UnityObject prefab;
#endif

		public UIBehaviourEvents Events
		{
			get
			{
				if (!_events)
				{
					_events = gameObject.TryGetComponent(out UIBehaviourEvents events)
						? events
						: gameObject.AddComponent<UIBehaviourEvents>();
				}

				return _events;
			}
		}

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
	}
}
