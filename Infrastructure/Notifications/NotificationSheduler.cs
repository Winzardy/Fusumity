using System;
using JetBrains.Annotations;
using Sapientia;
using Sapientia.Extensions;
using UnityEngine.Scripting;

namespace Notifications
{
	/// <summary>
	/// Базовая реализация, дублирует предназначение <see cref="IPresenter"/> (со своими особенностями)
	/// </summary>
	[Preserve]
	public abstract class NotificationScheduler : MessageSubscriber
	{
		[CanBeNull]
		private ISchedulerOverrider _overrider;

		public NotificationScheduler()
		{
			if (!NotificationsCenter.Register(this))
				return;

			_overrider?.Initialize(this);

			OnInitializeInternal();
		}

		protected override void OnDisposeInternal()
		{
			if (!NotificationsCenter.Unregister(this))
				return;

			_overrider?.Dispose();

			base.OnDisposeInternal();
		}

		protected virtual void OnInitializeInternal() => OnInitialize();

		protected virtual void OnInitialize()
		{
		}

		protected virtual void Schedule(ref NotificationRequest request)
		{
			_overrider?.Override(ref request);
			NotificationsCenter.Schedule(ref request);
		}

		// ReSharper disable once PossibleMistakenCallToGetType.2
		public static implicit operator Type(NotificationScheduler scheduler) => scheduler.GetType();

		protected internal void Construct(ISchedulerOverrider overrider)
		{
			_overrider = overrider;
		}
	}
}
