using UnityEngine;
using UnityEngine.UI;

namespace UI.Scroll
{
	public abstract class UIScrollItemLayout : UIBaseLayout
	{
		private LayoutElement _layoutElement;

		public LayoutElement LayoutElement
		{
			get
			{
				if (_layoutElement == null)
					TryGetComponent(out _layoutElement);

				return _layoutElement;
			}
		}

		#region Internal

		/// <summary>
		/// cellIdentifier — уникальная строка, которая позволяет скроллеру
		/// обрабатывать различные типы элементов в одном списке. Каждый тип
		/// элемента должен иметь свой собственный идентификатор
		/// </summary>
		[HideInInspector]
		public string cellIdentifier;

		/// <summary>
		/// Индекс виджета, который используется в верстке.
		/// Это будет отличаться от dataIndex, если список зациклен
		/// </summary>
		public int CellIndex { get; internal set; }

		/// <summary>
		/// Индекс элемента в дате (список аргументов)
		/// </summary>
		public int DataIndex { get; internal set; }

		/// <summary>
		/// Является ли элемент активным или переработанным
		/// </summary>
		public bool Active { get; internal set; }

		public int InstanceIndex { get; internal set; }

		/// <summary>
		/// Этот метод вызывается Scroll, когда RefreshActiveCells вызывается на прокрутке
		/// Вы можете переопределить его, чтобы обновить UID вашей ячейки
		/// </summary>
		public virtual void Refresh()
		{
		}

		private void OnValidate()
		{
			cellIdentifier = gameObject.name;
		}

		#endregion
	}
}
