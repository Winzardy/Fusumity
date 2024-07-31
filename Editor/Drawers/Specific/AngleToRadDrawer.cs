using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	public class AngleToRadDrawer : OdinAttributeDrawer<AngleToRadAttribute, float>
	{
		protected override void Initialize()
		{
			base.Initialize();
			ValueEntry.Property.Label.text = ValueEntry.Property.Label.text.Replace("Rad", "Angle") + "°";
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var rad = ValueEntry.SmartValue;

			var angle = SirenixEditorFields.FloatField(label, rad * Mathf.Rad2Deg);
			if (angle == 360f)
				rad = Mathf.PI * 2;
			else
				rad = angle * Mathf.Deg2Rad;

			ValueEntry.SmartValue = rad;
		}
	}
}