namespace Content.ScriptableObjects.Editor
{
	public partial class ContentDatabaseExport
	{
		public static void OnBuild(string buildOutputPath)
		{
			if(!UseExportOnBuild)
				return;

			DefaultExport(buildOutputPath);
		}
	}
}
