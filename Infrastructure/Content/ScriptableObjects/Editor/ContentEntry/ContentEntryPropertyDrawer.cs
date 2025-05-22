#if UNITY_EDITOR
using System;
using Content.ScriptableObjects;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	using UnityObject = UnityEngine.Object;

	public class ContentEntryPropertyDrawer : OdinValueDrawer<IUniqueContentEntry>
	{
		private bool _supported;

		private int? _cacheArrayLenght;

		public override bool CanDrawTypeFilter(Type type)
			=> type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ContentEntry<>);

		protected override void Initialize()
		{
			var targetObject = Property.Tree.UnitySerializedObject?.targetObject;
			_supported = !targetObject || targetObject is ContentScriptableObject;
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (!_supported)
			{
				EditorGUILayout.HelpBox(
					"ContentEntry supports only objects derived from ContentScriptableObject",
					MessageType.Warning);
				return;
			}

			if (Register(Property))
				Property.Tree?.UnitySerializedObject.ApplyModifiedProperties();

			CallNextDrawer(label);
		}

		private bool Register(InspectorProperty property)
		{
			if (!CanRegister(property, out var entry, out var targetObject))
				return false;

			if (targetObject is not ContentScriptableObject asset)
				return false;

			if (asset is IContentEntryScriptableObject scriptableObject)
			{
				var reference = property.ToContentReference();
				if (reference == null)
					return false;

				//Попытка восстановить или задать новый гуид...
				var key = (asset, reference);
				if (!scriptableObject.ScriptableContentEntry.RegisterNestedEntry(in entry.Guid, reference))
				{
					if (ContentEntryEditorUtility.TryGet(key, out var guid))
					{
						if (reference.steps[^1].IsArrayElement)
						{
							var lenght = property.Parent.Children.Count;
							if (_cacheArrayLenght > lenght)
							{
								//можно сделать чтобы только массив обновлялся
								asset.Refresh();
							}
							else if (_cacheArrayLenght == lenght)
							{
								//TODO: подумать как отловить момент когда делают перетаскивание...
								Restore(in guid);
							}
							else
							{
								//TODO: потенциальная проблема если кто-то начнется пользоваться "Insert Pasted element"
							}

							_cacheArrayLenght = lenght;
						}
						else
						{
							Restore(in guid);
						}
					}
					else if (!scriptableObject.Remember(in entry.Guid))
					{
						property.RegenerateGuid(entry, asset);

						if (!scriptableObject.ScriptableContentEntry.RegisterNestedEntry(in entry.Guid, reference))
							throw new Exception("Can't register nested entry...");
					}
				}

				property.Children.Update();
				foreach (var child in property.Children.Recurse())
					Register(child);

				scriptableObject.Remember(in entry.Guid);

				return ContentEntryEditorUtility.Track(key, in entry.Guid);
			}

			return false;

			void Restore(in SerializableGuid guid)
			{
				if (entry.Guid != guid)
					property.RestoreGuid(entry, in guid);
			}
		}

		private bool CanRegister(InspectorProperty property, out IUniqueContentEntry entry, out UnityObject targetObject)
		{
			entry = null;
			targetObject = null;

			if (!GUI.enabled)
				return false;

			// Если использовать ValueEntry.SmartValue прыгают значения...
			if (property.ValueEntry?.WeakSmartValue is not IUniqueContentEntry uniqueContentEntry)
				return false;

			entry = uniqueContentEntry;

			if (property.Tree?.UnitySerializedObject == null)
				return false;

			if (!property.Tree?.UnitySerializedObject.targetObject)
				return false;

			targetObject = property.Tree.UnitySerializedObject.targetObject;

			if (property.IsAnyParentHasAttribute<DisableContentEntryDrawerAttribute>())
				return false;

			if (entry.Guid == Guid.Empty)
			{
				ContentEntryEditorUtility.RegenerateGuid(entry, property.UnityPropertyPath, targetObject);

				if(entry.Guid != SerializableGuid.Empty)
					return true;

				ContentDebug.LogError($"Guid is empty by property path [ {property.UnityPropertyPath} ]!", targetObject);
				return false;
			}

			return true;
		}
	}
}
#endif
