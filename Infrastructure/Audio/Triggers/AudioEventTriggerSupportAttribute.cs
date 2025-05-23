using System;

namespace Audio
{
	/// <summary>
	/// Добавляет или убирает возможно зацикленное воспроизведение в <see cref="AudioEventTriggerArgs"/>
	/// </summary>
	public class AudioEventTriggerSupportAttribute : Attribute
	{
		public bool Loop { get; set; }

		public AudioEventTriggerSupportAttribute(bool loop)
		{
			Loop = true;
		}
	}
}
