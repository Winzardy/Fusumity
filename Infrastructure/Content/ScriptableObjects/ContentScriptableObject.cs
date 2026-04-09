using System;
using System.Globalization;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentScriptableObject : ScriptableObject, IContentScriptableObject
	{
		[SerializeField]
		protected long timeCreated;

		public DateTime CreationTime => new DateTime(timeCreated, DateTimeKind.Utc)
			.ToLocalTime();

		public long TimeCreated => timeCreated;

		/// <summary>
		/// Не всегда является временем когда был создан ассет, но стремится к этому
		/// </summary>
		public string CreationTimeStr => CreationTime.ToString(CultureInfo.InvariantCulture);

		public virtual IContentEntry Import(bool clone) => null;

		/// <summary>
		/// Используется ли контент? Если нет, то при обновлении базы пропустит ScriptableObject
		/// </summary>
		public virtual bool Enabled => true;

		public string techDescription;

		public void SyncedUpdate() => OnUpdated();

		protected virtual void OnUpdated()
		{
		}

		public override string ToString() => $"[ 	<b>{name}</b>	 ]	(type: {GetType().Name})";
	}
}
