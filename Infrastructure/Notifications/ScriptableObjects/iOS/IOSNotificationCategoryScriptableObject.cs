using Notifications.iOS;
using UnityEngine;

namespace Content.ScriptableObjects.Notifications
{
	[CreateAssetMenu(menuName = ContentNotificationEditorConstants.CREATE_MENU + "iOS/Category", fileName = "Notifications_iOS_Category_New")]
	public class IOSNotificationCategoryScriptableObject : ContentEntryScriptableObject<IOSNotificationCategoryConfig>
	{
		public override bool Enabled
		{
			get
			{
#if !UNITY_IOS
				return false;
#endif
				return base.Enabled;
			}
		}
	}
}
