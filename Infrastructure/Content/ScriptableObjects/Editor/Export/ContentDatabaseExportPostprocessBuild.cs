using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Content.ScriptableObjects.Editor
{
	public class ContentDatabaseExportBuildProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder => int.MaxValue;

		public void OnPreprocessBuild(BuildReport report)
		{
			ContentDatabaseExport.OnBuild(report.summary.outputPath);
		}
	}
}
