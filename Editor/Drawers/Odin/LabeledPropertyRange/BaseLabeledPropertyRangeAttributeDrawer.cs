using Fusumity.Attributes;
using Fusumity.Utility;
using Sapientia.Extensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public abstract class BaseLabeledPropertyRangeAttributeDrawer<T> : OdinAttributeDrawer<LabeledPropertyRangeAttribute, T>
	{
		protected ValueResolver<T> getterMinValue;
		protected ValueResolver<T> getterMaxValue;

		/// <summary>Initialized the drawer.</summary>
		protected override void Initialize()
		{
			if (this.Attribute.MinGetter != null)
				this.getterMinValue = ValueResolver.Get<T>(this.Property, this.Attribute.MinGetter);
			if (this.Attribute.MaxGetter == null)
				return;
			this.getterMaxValue = ValueResolver.Get<T>(this.Property, this.Attribute.MaxGetter);
		}

		/// <summary>Draws the property.</summary>
		protected override void DrawPropertyLayout(GUIContent label)
		{
			DrawSlider(label);
			DrawLabels();
		}

		protected abstract void DrawSlider(GUIContent label);

		private void DrawLabels()
		{
			var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight / 3f);
			rect.y -= 8.5f;
			rect.height = EditorGUIUtility.singleLineHeight;

			var emptyLabel = new GUIContent(" ");
			var style = new GUIStyle(SirenixGUIStyles.MiniLabelCentered);
			style.normal.textColor = style.normal.textColor.WithAlpha(0.5f);

			if (!Attribute.MinLabel.IsNullOrEmpty())
			{
				style.alignment = TextAnchor.MiddleLeft;
				EditorGUI.LabelField(rect, emptyLabel, new GUIContent(Attribute.MinLabel), style);
			}

			if (!Attribute.MaxLabel.IsNullOrEmpty())
			{
				style.alignment = TextAnchor.MiddleRight;
				style.padding.right = 58;
				EditorGUI.LabelField(rect, emptyLabel, new GUIContent(Attribute.MaxLabel), style);
			}
		}
	}
}
