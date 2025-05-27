namespace Content.ScriptableObjects.Editor
{
	public partial class ContentDatabaseExport
	{
		public static void OnBuild()
		{
			if(!UseExportOnBuild)
				return;

			DefaultExport();
		}
	}
}
