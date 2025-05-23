using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fusumity.Editor.Utility;
using Sapientia.Extensions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AssetManagement.AddressableAssets.Editor
{
	public static class AssetManagementEditorUtility
	{
		public static List<T> LoadAddressableAssets<T>() where T : Object
		{
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

			List<AddressableAssetEntry> allEntries = new List<AddressableAssetEntry>(settings.groups.SelectMany(g => g.entries));
			List<T> assets = new List<T>();

			for (int i = 0; i < allEntries.Count; i++)
			{
				AddressableAssetEntry entry = allEntries[i];
				assets.Add(AssetDatabase.LoadAssetAtPath<T>(entry.AssetPath));
			}

			return assets;
		}

		public static AddressableAssetGroup GetGroup(string groupName)
		{
			if (!TryGetAddressableGroup(groupName, out AddressableAssetGroup group))
			{
				Debug.LogWarning($"Could not find addressable group: [ {groupName} ]");
			}

			return group;
		}

		public static bool TryGetAddressableGroup(string groupName, out AddressableAssetGroup group)
		{
			var addressableGroups = AddressableAssetSettingsDefaultObject.Settings.groups;

			for (int i = 0; i < addressableGroups.Count; i++)
			{
				var nextGroup = addressableGroups[i];
				if (nextGroup.name == groupName)
				{
					group = nextGroup;
					return true;
				}
			}

			group = null;
			return false;
		}

		public static List<AddressableAssetEntry> GetEntriesWithLabels(params string[] labels)
		{
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
			List<AddressableAssetEntry> allEntries = new List<AddressableAssetEntry>(settings.groups.SelectMany(g => g.entries));

			List<AddressableAssetEntry> selected = new List<AddressableAssetEntry>();

			for (int i = 0; i < allEntries.Count; i++)
			{
				AddressableAssetEntry entry = allEntries[i];

				if (ConainsAnyLabel(entry, labels))
				{
					selected.Add(entry);
				}
			}

			return selected;

			bool ConainsAnyLabel(AddressableAssetEntry entry, string[] labels)
			{
				for (int i = 0; i < labels.Length; i++)
				{
					if (entry.labels.Contains(labels[i])) return true;
				}

				return false;
			}
		}

		public static bool IsAddressable(Object asset)
		{
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
			AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabaseUtility.GetGUID(asset));
			return entry != null;
		}

		public static AssetReference CreateReference(AddressableAssetEntry entry)
		{
			if (entry == null)
				return null;

			return new AssetReference(entry.guid);
		}

		public static AddressableAssetEntry CreateAddressable(Object obj, AddressableEditorArgs args)
		{
			return CreateAddressable(obj, args.groupName, args.labelName, args.addressName, args.createGroupIfNonExistent);
		}

		public static bool TryFindGroup(string name, out AddressableAssetGroup group)
		{
			group = null;

			var assets = AssetDatabaseUtility.GetAssets<AddressableAssetGroup>();
			for (int i = 0; i < assets.Length; i++)
			{
				var s = assets[i].Name;
				if (s == name)
				{
					group = assets[i];
					return true;
				}
			}

			return false;
		}

		public static AddressableAssetEntry CreateAddressable(Object obj,
			string groupName = null,
			string addressName = null,
			string labelName = null,
			bool createGroupIfNonExistent = false)
		{
			var settings = AddressableAssetSettingsDefaultObject.Settings;

			if (settings == null)
			{
				Debug.LogError($"Could not find valid settings group while creating addressable.");
				return null;
			}

			var group = string.IsNullOrWhiteSpace(groupName) ? settings.DefaultGroup : settings.FindGroup(groupName);

			if (group == null)
			{
				if (createGroupIfNonExistent)
				{
					group = settings.CreateGroup(groupName,
						false, false, true, null,
						typeof(ContentUpdateGroupSchema),
						typeof(BundledAssetGroupSchema));
				}
				else
				{
					Debug.LogError($"Could not find addressable group: [{groupName}]");
					return null;
				}
			}

			var assetpath = AssetDatabase.GetAssetPath(obj);
			var guid = AssetDatabase.AssetPathToGUID(assetpath);

			var entry = settings.CreateOrMoveEntry(guid, group, false, false);

			var address = string.IsNullOrWhiteSpace(addressName) ? Path.GetFileNameWithoutExtension(entry.address) : addressName;

			entry.SetAddress(address);

			if (!string.IsNullOrWhiteSpace(labelName))
			{
				entry.SetLabel(labelName, true);
			}

			var entriesAdded = new List<AddressableAssetEntry> {entry};

			group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, false, true);
			settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true, false);

			return entry;
		}
	}

	public static class AddressablesEditorExtensions
	{
		public static void RemoveFromAddressables(this Object obj)
		{
			var settings = AddressableAssetSettingsDefaultObject.Settings;

			if (settings != null)
			{
				var guid = AssetDatabaseUtility.GetGUID(obj);
				settings.RemoveAssetEntry(guid);
			}
		}

		public static AddressableAssetEntry MakeAddressable(this Object obj,
			string groupName = null,
			string addressName = null,
			string labelName = null,
			bool createGroupIfNonExistent = false)
		{
			return AssetManagementEditorUtility.CreateAddressable(obj, groupName, addressName, labelName, createGroupIfNonExistent);
		}

		public static AssetReference MakeReference(this AddressableAssetEntry entry) => AssetManagementEditorUtility.CreateReference(entry);

		public static AssetReference MakeReference(this Object asset)
		{
			var guid = AssetDatabaseUtility.GetGUID(asset);

			if (AddressableAssetSettingsDefaultObject.Settings.TryFindEntry(guid, out var entry))
				return entry.MakeReference();

			Debug.LogError(
				$"Could not create asset reference for asset: " +
				$"[ {asset.name} ] - it is not marked as addressable!");
			return null;
		}

		public static AssetReference MakeReference(this Object asset, string groupName,
			string addressName = null,
			string labelName = null,
			bool createGroupIfNonExistent = false)
		{
			return asset
			   .MakeAddressable(groupName, addressName, labelName, createGroupIfNonExistent)
			   .MakeReference();
		}

		public static bool TryGetAddressableEntry(this AssetReference reference, out AddressableAssetEntry entry)
		{
			var asset = reference.editorAsset;

			if (asset == null)
			{
				entry = null;
				return false;
			}

			var guid = AssetDatabaseUtility.GetGUID(asset);
			return AddressableAssetSettingsDefaultObject.Settings.TryFindEntry(guid, out entry);
		}

		public static bool TryGetAddressableEntry(this Object asset, out AddressableAssetEntry entry)
		{
			var guid = AssetDatabaseUtility.GetGUID(asset);
			return AddressableAssetSettingsDefaultObject.Settings.TryFindEntry(guid, out entry);
		}

		public static bool TryGetAddressableEntry(this Object asset, string groupName, out AddressableAssetEntry entry,
			bool nameAsAddress = true)
		{
			if (AssetManagementEditorUtility.TryGetAddressableGroup(groupName, out var group))
			{
				return asset.TryGetAddressableEntry(group, out entry, nameAsAddress);
			}

			entry = null;
			return false;
		}

		public static bool TryGetAddressableEntry(this Object asset, AddressableAssetGroup group, out AddressableAssetEntry entry,
			bool nameAsAddress = true)
		{
			if (nameAsAddress)
			{
				foreach (var nextEntry in group.entries)
				{
					if (nextEntry.address == asset.name)
					{
						entry = nextEntry;
						return true;
					}
				}
			}
			else
			{
				var guid = AssetDatabaseUtility.GetGUID(asset);
				foreach (var nextEntry in group.entries)
				{
					if (nextEntry.guid == guid)
					{
						entry = nextEntry;
						return true;
					}
				}
			}

			entry = null;
			return false;
		}

		public static bool HasAddressableEntry(this AssetReference reference) => reference.TryGetAddressableEntry(out _);

		public static bool HasAddressableEntry(this Object asset) => asset.TryGetAddressableEntry(out _);

		public static bool HasAddressableEntry(this Object asset, string groupName, bool nameAsAddress = true) =>
			asset.TryGetAddressableEntry(groupName, out _, nameAsAddress);

		public static bool HasAddressableEntry(this Object asset, AddressableAssetGroup group, bool nameAsAddress = true) =>
			asset.TryGetAddressableEntry(group, out _, nameAsAddress);

		public static bool TryFindEntry(this AddressableAssetSettings settings, string guid, out AddressableAssetEntry entry)
		{
			entry = settings.FindAssetEntry(guid);
			return entry != null;
		}

		public static bool IsAddressable(this Object asset)
		{
			return AssetManagementEditorUtility.IsAddressable(asset);
		}

		/// <summary>
		/// Checks for empty reference, existing asset
		/// and that asset being an addressable.
		/// <br></br>
		/// Editor only.
		/// </summary>
		public static bool IsPopulated(this AssetReference reference, string groupName = null, bool nameAsAddress = false)
		{
			if (reference.IsEmpty()) return false;

			var asset = reference.editorAsset;

			if (asset != null)
			{
				return groupName.IsNullOrEmpty() ? asset.HasAddressableEntry() : asset.HasAddressableEntry(groupName, nameAsAddress);
			}

			return false;
		}

		public static bool IsEmpty(this AssetReference assetReference)
		{
			if (assetReference == null || string.IsNullOrEmpty(assetReference.AssetGUID))
				return true;

			return false;
		}

		public static bool AssetGuidEquals(this AssetReference assetReference, AssetReference otherReference)
		{
			if (assetReference.IsEmpty())
			{
				return otherReference.IsEmpty();
			}

			if (otherReference.IsEmpty())
			{
				return false;
			}

			return assetReference.AssetGUID == otherReference.AssetGUID;
		}
	}

	public struct AddressableEditorArgs
	{
		public string groupName;
		public string labelName;
		public string addressName;
		public bool createGroupIfNonExistent;
	}
}
