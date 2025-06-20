namespace UI.Scroll
{
	public struct ScrollListItemArgs<TValue> : IScrollListItemArgs
		where TValue : struct
	{
		public TValue value;

		public int Index { get; }

		public ScrollListItemArgs(int index) : this() => Index = index;

		public static implicit operator TValue(ScrollListItemArgs<TValue> args) => args.value;
	}

	//TODO: убрать интерфейс
	public interface IScrollListItemArgs
	{
		public int Index { get; }

		/// <summary>
		/// Allows to set custom size of element.
		/// If set to zero, it is controlled by "defaultItemSize" property
		/// in the Scroll component.
		/// </summary>
		public float GetSize() => 0;
	}

	public abstract class UIScrollListItem<TLayout, TArgs> : UIWidget<TLayout, TArgs>, IScrollItem<TLayout>
		where TArgs : struct, IScrollListItemArgs
		where TLayout : UIScrollItemLayout
	{
		public int Index => _args.Index;
	}

	public static class ScrollListItemArgsExtensions
	{
		//TODO: убрать! так как это только в контроллере! размер ячейки определяется через контроллер (<see cref="UIScroll"/>) и по аргументам виджета!
		public static float GetCellSize(this IScrollListItemArgs itemArgs, float defaultValue)
		{
			if (itemArgs == null)
				return defaultValue;

			var size = itemArgs.GetSize();

			return size > 0 ? size : defaultValue;
		}
	}
}
