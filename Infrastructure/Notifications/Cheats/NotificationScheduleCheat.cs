using System;
using System.Linq;
using Content;
using Localization;
using MobileConsole;

namespace Notifications.Cheats
{
	internal class NotificationsCheatUtility
	{
		public const string COMMAND_PATH = "App/Notifications";
	}

	//TODO: без аргументов пока
	[ExecutableCommand(name = NotificationsCheatUtility.COMMAND_PATH + "/Schedule")]
	public class NotificationScheduleCheat : Command
	{
		[Dropdown(methodName: nameof(GetNotifications))]
		public string target;

		public int delay = 5;

		public bool forceShowInForeground = true;

		public NotificationScheduleCheat() => info.actionAfterExecuted = ActionAfterExecuted.CloseAllSubView;

		public override void Execute()
		{
			var entry = ContentManager.Get<NotificationConfig>(target);

			if (forceShowInForeground)
				entry.showInForeground = true;

			var args = new NotificationRequest(target, entry)
			{
				title = LocManager.Get(entry.titleLocKey),
				message = LocManager.Get(entry.messageLocKey),

				deliveryTime = DateTime.Now + TimeSpan.FromSeconds(delay)
			};

			NotificationsCenter.Schedule(ref args);
		}

		private string[] GetNotifications()
		{
			return ContentManager.GetAllEntries<NotificationConfig>()
			   .Where(x => x is IUniqueContentEntry)
			   .Select(x => ((IUniqueContentEntry) x).Id)
			   .ToArray();
		}
	}
}
