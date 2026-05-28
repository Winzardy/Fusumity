using System;
using Content.ScriptableObjects;
using Sapientia.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	using UnityObject = UnityEngine.Object;

	public class ContentEntryPropertyDrawer : OdinValueDrawer<IContentEntryT>, IDefinesGenericMenuItems
	{
		private const double THRESHOLD = 0.5d;

		private bool _supported;
		private (string path, MemberReflectionReference<IUniqueContentEntry> reference) _pathToReference;

		private double _lastRegisterTime;

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

			if (!ContentEditorCache.IsRebuilding)
			{
				if (Event.current.type == EventType.Repaint)
				{
					var now = EditorApplication.timeSinceStartup;
					if (_lastRegisterTime == 0 || now - _lastRegisterTime >= THRESHOLD)
					{
						if (Register(Property))
						{
							var targetObject = Property.Tree?.UnitySerializedObject.targetObject;
							EditorUtility.SetDirty(targetObject);
						}

						_lastRegisterTime = now;
					}
				}
			}

			CallNextDrawer(label);
		}

		private bool Register(InspectorProperty property)
		{
			if (!CanRegister(property, out var entry, out var targetObject))
				return false;

			if (targetObject is not ContentScriptableObject asset)
				return false;

			if (asset is not IContentEntryScriptableObject scriptableObject)
				return false;

			if (_pathToReference.path != property.Path)
				_pathToReference.reference = property.ToContentReference();

			var reference = _pathToReference.reference;

			if (reference.IsEmpty())
				return false;

			var scriptableEntry = scriptableObject.ScriptableContentEntry;
			if (scriptableEntry == null)
				return false;

			var key = (asset, reference);
			var hasTrackedGuid = ContentEntryEditorUtility.TryGet(key, out var trackedGuid);
			var changed = false;
			var nestedChanged = false;
			var handled = false;

			var isArrayElement = reference.steps.Length > 0
				&& reference.steps[^1].IsArrayElement;
			var arrayLengthChanged = isArrayElement && property.TrackArrayLength(asset);

			if (!scriptableObject.Remember(in entry.Guid))
			{
				ForceRegenerate(reference);
				changed = true;
				handled = true;
			}

			if (!handled && scriptableEntry.TryGetNestedEntryReference(in entry.Guid, out var registeredReference))
			{
				if (!IsSameReference(registeredReference, reference))
				{
					if (IsGuidStillRegisteredAtReference(scriptableEntry, registeredReference, in entry.Guid))
					{
						changed = ResolveDuplicate();
					}
					else
					{
						scriptableEntry.SetNestedEntryReference(in entry.Guid, reference);
						nestedChanged = true;
					}
				}
			}
			else if (!handled)
			{
				if (hasTrackedGuid && trackedGuid != entry.Guid && CanRestore(in trackedGuid))
				{
					Restore(in trackedGuid);
					scriptableEntry.SetNestedEntryReference(in entry.Guid, reference);
					changed       = true;
					nestedChanged = true;
				}
				else if (scriptableEntry.RegisterNestedEntry(in entry.Guid, reference))
				{
					nestedChanged = true;
				}
				else
				{
					changed = ResolveDuplicate();
				}
			}

			if (nestedChanged)
				EditorUtility.SetDirty(asset);

			ContentEntryEditorUtility.Track(key, in entry.Guid);

			if (changed || nestedChanged)
				ContentEntryEditorUtility.ScheduleNestedRefresh(asset);

			return changed || nestedChanged;

			bool ResolveDuplicate()
			{
				if (hasTrackedGuid && trackedGuid != entry.Guid && CanRestore(in trackedGuid))
				{
					Restore(in trackedGuid);
					scriptableEntry.SetNestedEntryReference(in entry.Guid, reference);
					return true;
				}

				ForceRegenerate(in reference);
				return true;
			}

			bool CanRestore(in SerializableGuid guid)
			{
				if (guid == SerializableGuid.Empty || arrayLengthChanged)
					return false;

				return !scriptableEntry.TryGetNestedEntryReference(in guid, out var trackedReference)
					|| IsSameReference(trackedReference, reference);
			}

			void Restore(in SerializableGuid guid)
			{
				if (entry.Guid != guid)
					property.RestoreGuid(entry, in guid);
			}

			void ForceRegenerate(in MemberReflectionReference<IUniqueContentEntry> reference)
			{
				var key = (asset, reference);
				ContentEntryEditorUtility.Untrack(in key);
				ContentEntryEditorUtility.RegenerateGuid(entry, property.UnityPropertyPath, asset, false);
				property.MarkSerializationRootDirty();

				if (!scriptableEntry.RegisterNestedEntry(in entry.Guid, reference))
					throw new Exception("Can't register nested entry...");

				ContentEntryEditorUtility.Track(in key, in entry.Guid);
			}
		}

		private static bool IsSameReference(MemberReflectionReference<IUniqueContentEntry> left,
			MemberReflectionReference<IUniqueContentEntry> right)
			=> !left.IsEmpty() && !right.IsEmpty() && left.Path == right.Path;

		private static bool IsGuidStillRegisteredAtReference(IScriptableContentEntry scriptableEntry,
			MemberReflectionReference<IUniqueContentEntry> reference,
			in SerializableGuid guid)
		{
			var registeredEntry = reference.ResolveSafe(scriptableEntry);
			return registeredEntry != null && registeredEntry.Guid == guid;
		}

		private bool CanRegister(InspectorProperty property, out IUniqueContentEntry entry, out UnityObject targetObject)
		{
			entry        = null;
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

				if (entry.Guid != SerializableGuid.Empty)
					return true;

				ContentDebug.LogError($"Guid is empty by property path [ {property.UnityPropertyPath} ]!", targetObject);
				return false;
			}

			return true;
		}

		public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
		{
			genericMenu.AddSeparator("");
			genericMenu.AddItem(new GUIContent("Set None"), false, () => property.ValueEntry.WeakSmartValue = null);
		}
	}
}
