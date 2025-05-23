using Notifications.Android.Entry;
using UnityEngine;

namespace Content.ScriptableObjects.Notifications
{
	[CreateAssetMenu(menuName = ContentNotificationEditorConstants.CREATE_MENU + "Android/Channel", fileName = "Notifications_Android_Channel_New")]
	public class AndroidNotificationChannelScriptableObject : ContentEntryScriptableObject<AndroidNotificationChannelEntry>
	{
		public override bool Enabled
		{
			get
			{
#if !UNITY_ANDROID
				return false;
#endif
				return base.Enabled;
			}
		}
	}
}
