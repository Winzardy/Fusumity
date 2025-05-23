using Notifications;
using UnityEngine;

namespace Content.ScriptableObjects.Notifications
{
	[CreateAssetMenu(menuName = ContentNotificationEditorConstants.CREATE_MENU + "Notification", fileName = "New Notification")]
	public class NotificationScriptableObject : ContentEntryScriptableObject<NotificationEntry>
	{
	}
}
