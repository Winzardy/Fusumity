namespace UI.Editor
{
	public interface IUIDispatcherEditorTab
	{
		public string Title { get; }
		public int Order => 99;
	}
}
