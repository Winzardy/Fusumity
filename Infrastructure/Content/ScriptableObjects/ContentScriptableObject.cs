using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentScriptableObject : ScriptableObject, IContentScriptableObject
	{
		/// <summary>
		/// Создан ли ассет через ScriptedImporter — тогда состоянием (enabled) владеет импортёр, а не сам SO
		/// </summary>
		[SerializeField, HideInInspector]
		private bool _imported;

		[SerializeField]
		protected long timeCreated;

		public string techDescription;

		#region Constants

		// Редакторные атрибуты — в ContentScriptableObjectAttributeProcessor
		[SerializeField]
		private ContentConstantsSettings _constants;

		/// <summary>
		/// Поддерживает ли тип генерацию констант по своему содержимому: переопредели вместе
		/// с <see cref="EnumerateConstants"/>, и в инспекторе появится блок настроек с кнопкой
		/// </summary>
		public virtual bool UseConstants { get => false; }

		/// <summary>
		/// Что именно превращать в константы. Вызывается только в редакторе, при генерации
		/// и при расчёте слепка «содержимое изменилось»
		/// </summary>
		public virtual IEnumerable<ContentConstantEntry> EnumerateConstants() => null;

		public ref ContentConstantsSettings ConstantsSettings { get => ref _constants; }

		#endregion

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

		protected internal virtual bool UseRedirect { get => false; }

		public virtual IContentEntry Import(bool clone) => null;

		public void SyncedUpdate() => OnUpdated();

		protected virtual void OnUpdated()
		{
		}

		public override string ToString() => $"[ 	<b>{name}</b>	 ]	(type: {GetType().Name})";

		protected internal virtual bool SkipValidation() => false;
	}
}
