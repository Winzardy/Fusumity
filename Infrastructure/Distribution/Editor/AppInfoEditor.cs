using Content.Editor;
using Distribution;

public class AppInfoEditor
{
	public static void SetupBuildNumber(int value)
	{
		ContentEditor.Edit<AppOptions>(OnEdit);

		void OnEdit(ref AppOptions options)
		{
			options.buildNumber = value;
		}
	}
}
