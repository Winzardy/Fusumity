using UnityEngine.EventSystems;

namespace Audio
{
	public class PointerClickAudioEventTrigger : BasePointerAudioEventTrigger, IPointerClickHandler
	{
		public void OnPointerClick(PointerEventData eventData) => OnPointerTrigger(eventData);
	}
}
