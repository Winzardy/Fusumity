using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Content.ScriptableObjects.Editor
{
	public class ContentDatabaseExportBuildProcessor : IPostprocessBuildWithReport
	{
		public int callbackOrder => 0;

		public void OnPostprocessBuild(BuildReport report)
		{
			ContentDatabaseExport.OnBuild(report.summary.outputPath);
		}
	}
}
