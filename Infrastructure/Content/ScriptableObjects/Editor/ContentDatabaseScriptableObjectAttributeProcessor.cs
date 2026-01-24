using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public class ContentDatabaseScriptableObjectAttributeProcessor : OdinAttributeProcessor<ContentDatabaseScriptableObject>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(ContentDatabaseScriptableObject.scriptableObjects):
					attributes.Add(new SearchableAttribute());
					attributes.Add(new PropertyOrderAttribute(99));
					attributes.Add(new PropertySpaceAttribute(10));
					attributes.Add(new ListDrawerSettingsAttribute
					{
						IsReadOnly = true,
						OnTitleBarGUI = $"@{nameof(ContentDatabaseScriptableObjectAttributeProcessor)}." +
							$"{nameof(DrawTitlebar)}($property)"
					});
					break;
			}
		}

		private static void DrawTitlebar(InspectorProperty property)
		{
			if (property.Parent.ValueEntry.WeakSmartValue is not ContentDatabaseScriptableObject database)
				return;

			if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
			{
				Sync(database);
			}

			if (SirenixEditorGUI.ToolbarButton(SdfIconType.SortNumericDown))
			{
				database.Sort();
			}
		}

		private static void Sync(ContentDatabaseScriptableObject database)
		{
			EditorApplication.delayCall += HandleDelayCall;

			void HandleDelayCall()
			{
				var origin = ContentDebug.Logging.database;
				try
				{
					ContentDebug.Logging.database = true;
					if (database is MiscDatabaseScriptableObject)
					{
						ContentDatabaseEditorUtility.SyncContent();
						return;
					}

					database.SyncContent();
				}
				finally
				{
					ContentDebug.Logging.database = origin;
				}
			}
		}
	}
}
