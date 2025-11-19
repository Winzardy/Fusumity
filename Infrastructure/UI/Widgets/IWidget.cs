using System;
using UnityEngine;

namespace UI
{
	public partial interface IWidget : IDisposable
	{
		/// <summary>
		///Когда виджет активирован (начало анимации - начало закрывание)
		/// </summary>
		public bool Active { get; }

		/// <summary>
		///Когда виджет вообще виден на экране (начало анимации - конец анимации)
		/// </summary>
		public bool Visible { get; }

		/// <summary>
		/// Когда виджет проиграл анимацию открытия и уже полностью открыт
		/// </summary>
		public bool Open { get; }

		public RectTransform RectTransform { get; }

		public event WidgetShownDelegate Shown;
		public event WidgetHiddenDelegate Hidden;
		public event WidgetLayoutInstalledDelegate LayoutInstalled;
		public event WidgetLayoutClearedDelegate LayoutCleared;

		public void Initialize()
		{
		}

		public void Reset()
		{
		}

		public void Refresh()
		{
		}

		public void SetActive(bool active, bool immediate = false, bool useCacheImmediate = true)
		{
		}

		protected internal void SetVisible(bool value)
		{
		}

		public bool IsActive() => Active;
		public bool IsVisible() => Visible;
		public bool IsOpen() => Open;
	}
}
