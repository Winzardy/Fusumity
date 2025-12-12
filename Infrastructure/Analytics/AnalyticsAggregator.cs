using System.Collections.Generic;
using Sapientia.Extensions;
using UnityEngine.Scripting;

namespace Analytics
{
	[Preserve]
	public abstract class AnalyticsAggregator : MessageSubscriber
	{
		private bool _active;

		public AnalyticsAggregator()
		{
			if (!AnalyticsCenter.Register(this))
				return;

			_active = true;

			AnalyticsCenter.BeforeSend += OnBeforeSend;
			OnInitialize();
		}

		protected override void OnDisposeInternal()
		{
			if (!AnalyticsCenter.Unregister(this))
				return;

			_active = false;

			AnalyticsCenter.BeforeSend -= OnBeforeSend;
			base.OnDisposeInternal();
		}

		protected virtual void OnInitialize()
		{
		}

		// TODO: можно добавить маску интеграций (например отправлять только в фейсбук)
		protected void Send(string id)
		{
			var payload = new AnalyticsEventPayload(id);
			Send(ref payload);
		}

		protected void Send(string id, Dictionary<string, object> parameters)
		{
			if (!_active)
			{
				AnalyticsDebug.LogWarning($"Send skipped for inactive aggregator [ {GetType().Name} ]");
				return;
			}

			var args = new AnalyticsEventPayload(id)
			{
				parameters = parameters
			};
			Send(ref args);
		}

		private void Send(ref AnalyticsEventPayload payload) => AnalyticsCenter.Send(ref payload);

		private void OnBeforeSend(in AnalyticsEventPayload payload) => OnBeforeSend(payload.id, payload.parameters);

		/// <summary>
		/// Перехватить событие и добавить к нему параметры...
		/// </summary>
		protected virtual void OnBeforeSend(string id, Dictionary<string, object> parameters)
		{
		}
	}
}
