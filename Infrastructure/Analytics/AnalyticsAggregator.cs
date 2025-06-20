using System.Collections.Generic;
using Sapientia.Extensions;
using UnityEngine.Scripting;

namespace Analytics
{
	/// <summary>
	/// Базовая реализация, дублирует предназначение <see cref="Game.Presenters.IPresenter"/> (со своими особенностями)
	/// </summary>
	[Preserve]
	public abstract class AnalyticsAggregator : MessageSubscriber
	{
		public virtual void Initialize()
		{
			AnalyticsCenter.BeforeSend += OnBeforeSend;
			OnInitialize();
		}

		protected override void OnDisposeInternal()
		{
			AnalyticsCenter.BeforeSend -= OnBeforeSend;
			base.OnDisposeInternal();
		}

		protected virtual void OnInitialize()
		{
		}
		//TODO: можно добавить маску интеграций (например отправлять только в фейсбук)
		protected void Send(string id)
		{
			var args = new AnalyticsEventArgs(id);
			Send(ref args);
		}

		protected void Send(string id, Dictionary<string, object> parameters)
		{
			var args = new AnalyticsEventArgs(id)
			{
				parameters = parameters
			};
			Send(ref args);
		}

		private void Send(ref AnalyticsEventArgs args) => AnalyticsCenter.Send(ref args);

		private void OnBeforeSend(AnalyticsEventArgs args) => OnBeforeSend(args.id, ref args.parameters);

		/// <summary>
		/// Перехватить событие и добавить к нему параметры...
		/// </summary>
		protected virtual void OnBeforeSend(string id, ref Dictionary<string, object> parameters)
		{
		}
	}
}
