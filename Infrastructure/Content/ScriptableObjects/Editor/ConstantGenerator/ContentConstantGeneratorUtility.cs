using System.Reflection;

namespace Content.ScriptableObjects.Editor
{
	public static class ContentConstantGeneratorUtility
	{
		public static bool HasContentGeneration(this ContentEntryScriptableObject scrObj)
		{
			var valueType = scrObj.ValueType;

			var attribute = valueType.GetCustomAttribute<ConstantsAttribute>();

			if (attribute == null)
			{
				var scrObjType = scrObj
					.GetType();
				attribute = scrObjType.GetCustomAttribute<ConstantsAttribute>();
			}

			return attribute != null;
		}
	}
}
