using UnityEngine.EventSystems;

namespace Audio
{
	public class PointerUpAudioEventTrigger : BasePointerAudioEventTrigger, IPointerUpHandler
	{
		public void OnPointerUp(PointerEventData eventData) => OnPointerTrigger(eventData);
	}
}
