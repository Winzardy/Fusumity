using Content.Editor;
using Targeting;

public class TargetingDeckEditor
{
	public static void SetupBuildNumber(int value)
	{
		ContentEditor.Edit<TargetingOptions>(OnEdit);

		void OnEdit(ref TargetingOptions options)
		{
			options.buildNumber = value;
		}
	}
}
