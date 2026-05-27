using System;
using System.Globalization;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentScriptableObject : ScriptableObject, IContentScriptableObject
	{
		[SerializeField]
		protected long timeCreated;

		public string techDescription;

		public DateTime CreationTime
		{
			get => new DateTime(timeCreated, DateTimeKind.Utc)
				.ToLocalTime();
		}

		public long TimeCreated { get => timeCreated; }

		/// <summary>
		/// Не всегда является временем когда был создан ассет, но стремится к этому
		/// </summary>
		public string CreationTimeStr { get => CreationTime.ToString(CultureInfo.InvariantCulture); }

		/// <summary>
		/// Используется ли контент? Если нет, то при обновлении базы пропустит ScriptableObject
		/// </summary>
		public virtual bool Enabled { get => true; }

		public virtual IContentEntry Import(bool clone) => null;

		public void SyncedUpdate() => OnUpdated();

		protected virtual void OnUpdated()
		{
		}

		public override string ToString() => $"[ 	<b>{name}</b>	 ]	(type: {GetType().Name})";

		protected internal virtual bool SkipValidation() => false;
	}
}
