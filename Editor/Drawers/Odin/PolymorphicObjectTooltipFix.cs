using System;
using System.Collections.Generic;
using Content;
using Fusumity.Editor.Utility;
using Sapientia;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	/// <summary>
	/// Набор editor-классов для корректного отображения tooltip’ов
	/// для полиморфных типов в Odin Inspector
	/// </summary>
	public class TypeSelectorV2AttributeProcessor : OdinAttributeProcessor<TypeSelectorV2>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new CustomTypeSelectorV2DrawAttribute());
		}
	}

	internal class CustomTypeSelectorV2DrawAttribute : Attribute
	{
	}

	[DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
	internal class CustomTypeSelectorDrawAttributeDrawer : OdinAttributeDrawer<CustomTypeSelectorV2DrawAttribute, TypeSelectorV2>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			CallNextDrawer(label);

			var selector = ValueEntry.SmartValue;

			var tree = selector?.SelectionTree;
			if (tree == null)
				return;

			foreach (var item in tree.MenuItems)
			{
				DrawInfo(item);
			}
		}

		private static void DrawInfo(OdinMenuItem item)
		{
			if (item.Value is not Type type)
				return;

			if (!TypeHint.TryGet(type, out var hint))
				return;

			item.OnDrawItem -= OnDrawItem;
			item.OnDrawItem += OnDrawItem;

			void OnDrawItem(OdinMenuItem drawingItem)
			{
				GUI.DrawTexture(drawingItem.Rect
						.Padding(5f, 3f)
						.AlignRight(16f)
						.AlignCenterY(16f),
					hint.icon);

				GUI.Label(drawingItem.Rect, new GUIContent("", hint.message));
			}
		}
	}

	[DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
	public class SerializeReferenceHintDrawer : OdinAttributeDrawer<SerializeReference>
	{
		private const string KEY = "SerializeReferenceHintDrawer";

		protected override bool CanDrawAttributeProperty(InspectorProperty property)
		{
			if (property.Context.GetPersistent<bool>(this, KEY, out var ctx))
				return ctx.Value;

			ctx.Value = property.ValueEntry.BaseValueType.IsSerializeReference();
			return ctx.Value;
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			CallNextDrawer(label);
			DrawHintIfNeeded();
		}

		private bool DrawHintIfNeeded()
		{
			var entry = Property.ValueEntry;
			var type = entry.TypeOfValue;
			if (type == null)
				return false;
			if (!TypeHint.TryGet(type, out var hint))
				return false;

			var rect = Property.LastDrawnValueRect;
			rect = rect.AlignTop(EditorGUIUtility.singleLineHeight);
			GUI.Label(rect, new GUIContent(string.Empty, hint.message));
			return true;
		}
	}
}
