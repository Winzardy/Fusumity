using System;
using UnityEngine;

namespace UI
{
	public partial interface IWidget : IDisposable
	{
		Type GetDeclaredArgsType() => typeof(EmptyArgs);
		object GetArgs() => null;

		/// <summary>
		///Когда виджет активирован (начало анимации - начало закрывание)
		/// </summary>
		bool Active { get; }

		/// <summary>
		///Когда виджет вообще виден на экране (начало анимации - конец анимации)
		/// </summary>
		bool Visible { get; }

		/// <summary>
		/// Когда виджет проиграл анимацию открытия и уже полностью открыт
		/// </summary>
		bool Open { get; }

		RectTransform RectTransform { get; }
		UIBaseLayout BaseLayout { get; }

		string Layer { get; }
		WidgetFlags Flags { get; }

		event WidgetShownDelegate Shown;
		event WidgetHiddenDelegate Hidden;
		event WidgetLayoutInstalledDelegate LayoutInstalled;
		event WidgetLayoutClearedDelegate LayoutCleared;

		void Initialize()
		{
		}

		void Reset();
		void Reset(bool deactivate);

		void Refresh()
		{
		}

		void SetActive(bool active, bool immediate = false, bool useCacheImmediate = true)
		{
		}

		protected internal void SetVisible(bool value)
		{
		}

		bool IsActive() => Active;
		bool IsVisible() => Visible;
		bool IsOpen() => Open;
	}

	[Flags]
	public enum WidgetFlags
	{
		None,

		/// <summary>
		/// Указывает, что виджет полностью окклюзирует игровой экран.
		/// Может использоваться для отключения рендера сцены и оптимизации производительности
		/// </summary>
		Fullscreen = 1 << 0
	}
}
