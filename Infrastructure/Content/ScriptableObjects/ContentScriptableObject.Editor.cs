using System;
using System.Globalization;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentScriptableObject
	{
		/// <see cref="timeCreated"/>
		public const string TIME_CREATED_FILED_NAME = "timeCreated";

		/// <inheritdoc cref="creationTimeStr"/>
		public const string CREATION_TIME_TOOLTIP = "Не всегда является временем когда был создан ассет, но стремится к этомy";

		public long TimeCreated => timeCreated;
		public DateTime creationTime => new DateTime(timeCreated, DateTimeKind.Utc).ToLocalTime();

		/// <summary>
		/// Не всегда является временем когда был создан ассет, но стремится к этому
		/// </summary>
		public string creationTimeStr => creationTime.ToString(CultureInfo.InvariantCulture);

		/// <summary>
		/// Включает/выключает отображение основного Entry в инспекторе
		/// </summary>
		public virtual bool UseCustomInspector => false;

		protected virtual void OnValidate()
		{
			if (timeCreated != 0)
				return;

			ForceUpdateTimeCreated();
		}

		public void ForceUpdateTimeCreated()
		{
			timeCreated = DateTime.UtcNow.Ticks;
			UnityEditor.EditorUtility.SetDirty(this);
		}
	}
}
