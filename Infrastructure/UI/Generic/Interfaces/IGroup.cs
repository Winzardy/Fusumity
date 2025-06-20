namespace UI
{
	public enum AlignmentGroupType
	{
		None = 0,

		Top = 1,
		Bottom = 2,
		Left = 3,
		Right = 4
	}

	public interface IGroup<T>
	{
		public int IndexOf(T tab);

		public AlignmentGroupType GetAlignmentGroupType() => AlignmentGroupType.None;
	}

	public interface ISelectedGroup<T> : IGroup<T>
	{
		public int PrevTabIndex { get; }
		public int NextTabIndex { get; }
	}
}
