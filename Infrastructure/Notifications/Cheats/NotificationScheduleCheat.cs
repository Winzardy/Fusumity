using System;
using System.Linq;
using Content;
using Localization;
using MobileConsole;
using Sapientia.Extensions;

namespace Notifications.Cheats
{
	internal class NotificationsCheatUtility
	{
		public const string COMMAND_PATH = "App/Notifications";
	}

	[SettingCommand(name = "System/Notifications")]
	public class NotificationsSettingsCheat : Command
	{
		[Dropdown(methodName: nameof(GetSpeedOptions)), Variable(OnValueChanged = nameof(SetSpeed))]
		public string deliverySpeed = "x1";

		public override void InitDefaultVariableValue()
		{
			deliverySpeed = $"x{NotificationsDebug.DeliverySpeed}";
		}

		public override void OnVariableValueLoaded() => SetSpeed();

		private void SetSpeed()
		{
			var speed = deliverySpeed switch
			{
				"x10" => 10,
				"x60" => 60,
				"x360" => 360,
				"x3600" => 3600,
				"x36000" => 360000,
				_ => 1
			};

			NotificationsDebug.SetDeliverySpeed(speed);
		}

		private string[] GetSpeedOptions() => new[]
		{
			"x1",
			"x10",
			"x60",
			"x360",
			"x3600",
			"x36000"
		};
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

	[ExecutableCommand(name = NotificationsCheatUtility.COMMAND_PATH + "/Log Scheduled")]
	public class NotificationLogScheduledCheat : Command
	{
		public NotificationLogScheduledCheat() => info.actionAfterExecuted = ActionAfterExecuted.DoNothing;

		public override void Execute()
		{
			if (!NotificationsCenter.IsInitialized)
			{
				NotificationsDebug.LogWarning("Notifications center is not initialized");
				return;
			}

			var notifications = NotificationsCenter.GetScheduledNotifications()
				.OrderBy(x => x.deliveryTime)
				.ToArray();

			NotificationsDebug.Log($"Scheduled notifications: {notifications.Length}" +
				$"{notifications.GetCompositeString(vertical: false, separator: "\n————————")}");
		}
	}
}
