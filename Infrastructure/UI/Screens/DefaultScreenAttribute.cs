using System;
using UnityEngine.Scripting;

namespace UI.Screens
{
	/// <summary>
	/// Помечаем экран как дефолтный, чтобы он загрузился при старте
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class DefaultScreenAttribute : PreserveAttribute
	{
		public bool autoShow;

		/// <param name="autoShow">Чтобы контролировать показ экрана.
		/// Например подождать подгрузки и плавно вызвать с анимацией</param>
		public DefaultScreenAttribute(bool autoShow = true)
		{
			this.autoShow = autoShow;
		}
	}
}
