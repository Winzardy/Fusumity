using System;
using System.Collections.Generic;
using Fusumity.Utility;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class GameObjectArrayAttributeProcessor : OdinAttributeProcessor<GameObject[]>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var attribute = new ListDrawerSettingsAttribute();
			attribute.OnTitleBarGUI = $"@{nameof(GameObjectArrayAttributeProcessor)}.{nameof(DrawButton)}($property)";
			attributes.Add(attribute);
		}

		private static void DrawButton(InspectorProperty property)
		{
			if (property.ValueEntry.WeakSmartValue is not GameObject[] gameObjects)
				return;

			if (gameObjects.IsNullOrEmpty() || gameObjects.Any(x => !x))
				return;

			var active = !gameObjects.Any(x => !x.activeSelf);
			if (SirenixEditorGUI.ToolbarButton(active ? SdfIconType.Bookmarks : SdfIconType.BookmarksFill))
				gameObjects.SetActive(!active);
		}
	}
}
