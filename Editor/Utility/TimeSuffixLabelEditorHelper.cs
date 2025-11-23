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
			if (property.ValueEntry.WeakSmartValue is int ms)
			{
				var abs = ms.Abs();
				if (abs > 1000)
					return ms.ToLabel(false);
			}

			return string.Empty;
		}

		public static string SecondToTimespan(InspectorProperty property)
		{
			if (property.ValueEntry.WeakSmartValue is int sec)
			{
				var abs = sec.Abs();
				return abs.ToLabel(false, false);
			}

			return string.Empty;
		}
	}
}
