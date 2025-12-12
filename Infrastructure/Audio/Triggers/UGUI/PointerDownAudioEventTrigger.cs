using UnityEngine.EventSystems;

namespace Audio
{
	public class PointerDownAudioEventTrigger : BasePointerAudioEventTrigger, IPointerDownHandler
	{
		public void OnPointerDown(PointerEventData eventData) => OnPointerTrigger(eventData);
	}
}
