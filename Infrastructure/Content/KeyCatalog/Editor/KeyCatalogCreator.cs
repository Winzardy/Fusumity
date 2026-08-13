using System;
using System.Collections.Generic;
using Content.Editor;
using Content.ScriptableObjects;
using Content.ScriptableObjects.Editor;
using Fusumity.Editor;
using Fusumity.Editor.Utility;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Keys.Editor
{
	/// <summary>
	/// Каталог, на который ссылается поле, может ещё не существовать — например ключ завели
	/// в коде раньше контента. Вместо тупика «нет такого id» дровер предлагает создать каталог
	/// на месте, рядом с уже существующими каталогами того же типа значения: чужая папка
	/// увела бы ассет в другую контент-базу, а это двойная регистрация на бейке
	/// </summary>
	internal static class KeyCatalogCreator
	{
		private const string ASSET_PREFIX = "KeyCatalog_";

		/// <summary>
		/// Плашка «каталога нет» с кнопкой создания. Кнопка появляется только если в проекте
		/// есть тип-обёртка под это значение — иначе создавать нечего и обещать нечего
		/// </summary>
		public static void DrawMissingCatalogBox(string catalogId, string keyTypeName, Type valueType, Action created)
		{
			var message = $"Not found catalog ({keyTypeName}) by id [ {catalogId} ]";

			if (catalogId.IsNullOrEmpty() || !TryResolveAssetType(valueType, out var assetType))
			{
				SirenixEditorGUI.WarningMessageBox($"{message} ");
				return;
			}

			// Папка в тексте — до нажатия видно, куда ляжет ассет: соседняя папка увела бы
			// каталог в другую контент-базу
			var folder = ResolveFolder(valueType);
			if (!folder.IsNullOrEmpty())
				message += $"\nWill be created in {folder}";

			if (!FusumityEditorGUILayout.MessageBoxButton(message, "Create"))
				return;

			if (Create(catalogId, assetType, folder) != null)
				created?.Invoke();
		}

		/// <summary>
		/// Тип-обёртка берётся у уже существующих каталогов: проект мог объявить свой наследник,
		/// и создавать надо ровно его. Каталогов ещё нет — ищем по типу значения среди наследников
		/// </summary>
		private static bool TryResolveAssetType(Type valueType, out Type assetType)
		{
			foreach (var source in ContentEditorCache.GetAllSourceByValueType(valueType))
			{
				if (source is not ScriptableObject scriptableObject)
					continue;

				assetType = scriptableObject.GetType();
				return true;
			}

			foreach (var type in TypeCache.GetTypesDerivedFrom<ContentEntryScriptableObject>())
			{
				if (type.IsAbstract || EntryValueType(type) != valueType)
					continue;

				assetType = type;
				return true;
			}

			assetType = null;
			return false;
		}

		private static Type EntryValueType(Type assetType)
		{
			for (var type = assetType; type != null; type = type.BaseType)
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ContentEntryScriptableObject<>))
					return type.GetGenericArguments()[0];
			}

			return null;
		}

		/// <summary>
		/// Папка — та, где лежит большинство существующих каталогов этого типа
		/// </summary>
		private static string ResolveFolder(Type valueType)
		{
			using var _ = DictionaryPool<string, int>.Get(out var countByFolder);

			foreach (var source in ContentEditorCache.GetAllSourceByValueType(valueType))
			{
				if (source is not ScriptableObject scriptableObject)
					continue;

				var path = AssetDatabase.GetAssetPath(scriptableObject);
				var separatorIndex = path.LastIndexOf('/');
				if (separatorIndex <= 0)
					continue;

				var folder = path[..separatorIndex];
				countByFolder[folder] = countByFolder.GetValueOrDefault(folder) + 1;
			}

			var best = string.Empty;
			var bestCount = 0;

			foreach (var (folder, count) in countByFolder)
			{
				if (count <= bestCount)
					continue;

				best = folder;
				bestCount = count;
			}

			return best;
		}

		private static ScriptableObject Create(string catalogId, Type assetType, string folder)
		{
			var assetName = ASSET_PREFIX + AssetNamePart(catalogId);

			if (folder.IsNullOrEmpty())
			{
				var chosen = EditorUtility.SaveFilePanelInProject("Create key catalog", assetName, "asset",
					$"Where to create catalog [ {catalogId} ]?");

				if (chosen.IsNullOrEmpty())
					return null;

				return CreateAt(catalogId, assetType, chosen);
			}

			AssetDatabaseUtility.EnsureOrCreateFolder(folder);
			return CreateAt(catalogId, assetType, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{assetName}.asset"));
		}

		private static ScriptableObject CreateAt(string catalogId, Type assetType, string assetPath)
		{
			var asset = ScriptableObject.CreateInstance(assetType);
			if (asset == null)
				return null;

			AssetDatabase.CreateAsset(asset, assetPath);
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
			AssetDatabase.SaveAssets();

			var created = AssetDatabase.LoadAssetAtPath<ContentEntryScriptableObject>(assetPath);
			if (created == null)
				return null;

			// Свежесозданной записи guid проставляет синк — без него ссылка на каталог
			// не резолвится, и поле останется с той же ошибкой
			if (created.NeedSync())
			{
				created.Sync(true);
				AssetDatabase.SaveAssetIfDirty(created);
			}

			created.SetId(catalogId);
			EditorUtility.SetDirty(created);
			AssetDatabase.SaveAssetIfDirty(created);

			ContentEditorCache.RefreshByValueType(created.ValueType);
			ContentAutoConstantsGenerator.ForceInvokeWithDelay(created.GetType());

			EditorGUIUtility.PingObject(created);
			return created;
		}

		/// <summary>
		/// Id может нести иерархию через "/" — в имя файла идёт лист
		/// </summary>
		private static string AssetNamePart(string catalogId)
		{
			var separatorIndex = catalogId.LastIndexOf('/');
			var leaf = separatorIndex >= 0 ? catalogId[(separatorIndex + 1)..] : catalogId;

			using var _ = ListPool<char>.Get(out var characters);
			foreach (var character in leaf)
			{
				if (char.IsLetterOrDigit(character) || character == '_')
					characters.Add(character);
			}

			return characters.Count > 0 ? new string(characters.ToArray()) : "New";
		}
	}
}
