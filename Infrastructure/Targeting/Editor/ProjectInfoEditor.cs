using Content.Editor;
using Targeting;

public class ProjectInfoEditor
{
	public static void SetupBuildNumber(int value)
	{
		ContentEditor.Edit<ProjectInfo>(OnEdit);
		void OnEdit(ref ProjectInfo options)
		{
			options.buildNumber = value;
		}
	}
}
