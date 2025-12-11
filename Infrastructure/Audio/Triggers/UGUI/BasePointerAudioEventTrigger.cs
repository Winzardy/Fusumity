using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Audio
{
	public abstract class BasePointerAudioEventTrigger : AudioEventTrigger, ISubmitHandler
	{
		public AudioEventRequest audioEvent;

		public Selectable selectable;
		public bool onlyInteractable = true;
		public bool useCustomAudioEventForNonInteractable;

		public AudioEventRequest customAudioEvent;

		private RectTransform _rect;

		protected void OnPointerTrigger(PointerEventData eventData) => OnPointerTrigger(eventData.GetAudioSpatialPosition());

		void ISubmitHandler.OnSubmit(BaseEventData _)
		{
			if (!_rect)
				_rect = selectable ? (RectTransform) selectable.transform : (RectTransform) transform;

			OnPointerTrigger(_rect.GetAudioSpatialPosition());
		}

		protected void OnPointerTrigger(Vector3 position)
		{
			var isPlay = selectable?.IsInteractable() ?? true;

			if (onlyInteractable && !isPlay)
				return;

			if (!isPlay && useCustomAudioEventForNonInteractable)
			{
				if (customAudioEvent.IsEmpty)
				{
					AudioDebug.LogError("Non interactable audio event is not set");
					return;
				}

				customAudioEvent.Play(position);
			}
			else
			{
				audioEvent.Play(position);
			}
		}

		private void Reset()
		{
			selectable = GetComponent<Selectable>();
		}
	}
}
