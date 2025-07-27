using Fusumity.Attributes;
using Fusumity.Utility;
using Sapientia.Extensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public class ColorCardBoxAttributeDrawer : OdinAttributeDrawer<ColorCardBoxAttribute>
	{
		private ValueResolver<string> _valueResolver;

		protected override void Initialize()
		{
			_valueResolver = ValueResolver.GetForString(Property, Attribute.Label);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			FusumityEditorGUILayout.BeginCardBox(new Color(Attribute.R, Attribute.G, Attribute.B, Attribute.A));

			if (!Attribute.Label.IsNullOrEmpty())
			{
				GUILayout.Label(_valueResolver.GetValue(), SirenixGUIStyles.MiniLabelCentered);
				if (Attribute.UseLabelSeparator)
					SirenixEditorGUI.HorizontalLineSeparator(Color.black.WithAlpha(0.2f));
			}

			CallNextDrawer(label);

			FusumityEditorGUILayout.EndCardBox();
		}
	}
}
