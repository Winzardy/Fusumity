using System;

namespace Audio
{
	/// <summary>
	/// Добавляет или убирает возможно настраивать зацикленное воспроизведение в <see cref="AudioEventRequest"/>
	/// </summary>
	public class AllowLoopAttribute : Attribute
	{
		public bool Loop { get; set; }

		public AllowLoopAttribute(bool loop)
		{
			Loop = loop;
		}
	}
}
