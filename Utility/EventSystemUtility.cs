using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fusumity.Utility
{
	public static class EventSystemUtility
	{
		public static bool IsPointerOverUIObject(Vector2 screenPoint)
		{
			var eventDataCurrentPosition = new PointerEventData(EventSystem.current);
			eventDataCurrentPosition.position = new Vector2(screenPoint.x, screenPoint.y);
			var results = new List<RaycastResult>();

			EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

			for (int i = 0; i < results.Count; i++)
			{
				var result = results[i];
				if (result.module is GraphicRaycaster)
					return true;
			}

			return false;
		}
	}
}
