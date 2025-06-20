namespace UI.Scroll
{
	public interface IScrollItem<TLayout> : IWidget<TLayout>
		where TLayout : UIScrollItemLayout
	{
		public string Identifier => Layout.cellIdentifier;
		public int CellIndex => Layout.CellIndex;
		public int DataIndex => Layout.DataIndex;
		public bool CellActive => Layout.Active;
	}
}
