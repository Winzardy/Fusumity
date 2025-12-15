using Content.Editor;
using ProjectInformation;

public class ProjectInfoEditor
{
	public static void SetupBuildNumber(int value)
	{
		ContentEditor.Edit<ProjectInfoConfig>(OnEdit);
		void OnEdit(ref ProjectInfoConfig options)
		{
			options.buildNumber = value;
		}
	}
}
