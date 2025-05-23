namespace UI.Scroll
{
	/// <summary>
	/// All scripts that handle the scroll's callbacks should inherit from this interface
	/// </summary>
	public interface IScrollListDelegate
	{
		/// <summary>
		/// Gets the number of cells in a list of data
		/// </summary>
		/// <param name="scroller"></param>
		/// <returns></returns>
		int GetNumberOfItems(UIScrollLayout scroller);

		/// <summary>
		/// Gets the size of a cell view given the index of the data set.
		/// This allows you to have different sized cells
		/// </summary>
		/// <param name="scroller"></param>
		/// <param name="dataIndex"></param>
		/// <returns></returns>
		float GetCellSize(UIScrollLayout scroller, int dataIndex);

		/// <summary>
		/// Gets the cell that should be used for the data index. Your implementation
		/// of this function should request a new cell from the scroller so that it can
		/// properly recycle old cells.
		/// </summary>
		/// <param name="scroller"></param>
		/// <param name="dataIndex"></param>
		/// <param name="cellIndex"></param>
		/// <returns></returns>
		UIScrollItemLayout GetCell(UIScrollLayout scroller, int dataIndex, int cellIndex);
	}
}
