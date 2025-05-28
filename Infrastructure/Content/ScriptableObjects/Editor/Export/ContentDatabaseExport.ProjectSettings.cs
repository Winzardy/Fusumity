using System;

namespace Content.ScriptableObjects.Editor
{
	public partial class ContentDatabaseExport
	{
		[Serializable]
		public class ProjectSettings
		{
			public bool exportOnBuild = true;
			public string[] skipDatabases;
		}

		public ProjectSettings projectSettings;
	}
}
