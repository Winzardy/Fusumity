using System;
using Content;
using Localization;
using UnityEngine;

namespace Notifications
{
	/// <summary>
	/// Важно чтобы наследники этого интерфейса были доступны с любой платформы иначе сломается ассет!
	/// </summary>
	public interface IPlatformNotificationEntry
	{
	}

	[Serializable]
	[Constants]
	public struct NotificationEntry
	{
		[LocKey]
		public string titleLocKey;

		[LocKey]
		public string messageLocKey;

		[Tooltip("Показывать ли уведомление при открытом приложении")]
		public bool showInForeground;

		[Space]
		[SerializeReference]
		public IPlatformNotificationEntry[] platforms;
	}
}
