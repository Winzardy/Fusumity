using Sirenix.OdinInspector;

namespace UI.Editor
{
	public interface IUIDispatcherEditorTab
	{
		public string Title { get; }
		public int Order => 99;
		SdfIconType? Icon => null;
	}
}
