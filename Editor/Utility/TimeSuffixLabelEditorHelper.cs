using Sapientia.Extensions;
using Sirenix.OdinInspector.Editor;

namespace Fusumity.Editor
{
	/// <summary>
	/// <see cref="TimeFromMsSuffixLabelAttribute"/>
	/// </summary>
	public static class TimeSuffixLabelEditorHelper
	{
		public static string MillisecondToTimespan(InspectorProperty property)
		{
			if (property.ValueEntry.WeakSmartValue is int ms and >= 1000)
				return ms.ToLabel(false);

			return string.Empty;
		}
	}
}
