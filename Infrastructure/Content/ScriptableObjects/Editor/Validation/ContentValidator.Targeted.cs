using System.Collections.Generic;
using Sapientia.Pooling;
using Content.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	using UnityObject = UnityEngine.Object;

	/// <summary>
	/// Точечная валидация: конкретные конфиги и префабы вместо всех баз разом. Поднимается
	/// из контекстного меню Project-окна и из Content Browser, пишет в тот же отчёт
	/// </summary>
	public static partial class ContentValidator
	{
		private const string TARGETED_TITLE = "Validate Selected Content";

		// Приоритет 31 — сразу под верхним блоком Open/Open C# Project, не в хвосте меню
		private const string ASSETS_VALIDATE_MENU = "Assets/Content/Validate";
		private const int ASSETS_VALIDATE_PRIORITY = 31;

		[MenuItem(ASSETS_VALIDATE_MENU, priority = ASSETS_VALIDATE_PRIORITY)]
		private static void ValidateSelectionMenu()
		{
			using (ListPool<UnityObject>.Get(out var targets))
			{
				CollectSelectionTargets(targets);
				ValidateAssets(targets, out _);
			}
		}

		[MenuItem(ASSETS_VALIDATE_MENU, true)]
		private static bool ValidateSelectionMenuValidate()
		{
			using (ListPool<UnityObject>.Get(out var targets))
			{
				CollectSelectionTargets(targets);
				return targets.Count > 0;
			}
		}

		/// <summary>Из выделения берём только то, что умеем проверять точечно</summary>
		private static void CollectSelectionTargets(List<UnityObject> targets)
		{
			var selection = Selection.objects;

			for (var i = 0; i < selection.Length; i++)
			{
				if (IsValidatableAsset(selection[i]))
					targets.Add(selection[i]);
			}
		}

		/// <summary>Контент-SO или префаб — то, что видит и полная валидация</summary>
		public static bool IsValidatableAsset(UnityObject asset)
		{
			if (!asset)
				return false;

			if (asset is ContentScriptableObject)
				return true;

			return asset is GameObject gameObject &&
				PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab;
		}

		public static bool ValidateAsset(UnityObject asset)
		{
			using (ListPool<UnityObject>.Get(out var targets))
			{
				targets.Add(asset);
				return ValidateAssets(targets, out _);
			}
		}

		public static bool ValidateAssets(IReadOnlyList<UnityObject> assets, out string errorOrMessage)
		{
			// Вложенный вызов из уже идущей валидации пишет в её активный отчёт
			if (_activeReport != null)
				return ValidateAssetsInternal(assets, out errorOrMessage);

			ClearLastReport();
			var report = Pool<ContentValidationReport>.Get();
			LastReport = report;
			_activeReport = report;

			var result = false;
			try
			{
				result = ValidateAssetsInternal(assets, out errorOrMessage);
				return result;
			}
			finally
			{
				report.Complete(result && !report.WasCanceled);
				_activeReport = null;

				if (!Application.isBatchMode)
					ContentValidationReportWindow.ShowAfterValidation();
			}
		}

		private static bool ValidateAssetsInternal(IReadOnlyList<UnityObject> assets, out string errorOrMessage)
		{
			_cancelRequested = false;

			var errorCount = 0;
			var warningCount = 0;
			var valueValidators = GetEnabledValidators();

			try
			{
				using (StringBuilderPool.Get(out var errStringBuilder))
				{
					var previousErrorMessageBuilder = _activeErrorMessageBuilder;
					var previousErrorMessageNumber = _activeErrorMessageNumber;
					_activeErrorMessageBuilder = errStringBuilder;
					_activeErrorMessageNumber = 0;

					try
					{
						for (var i = 0; i < assets.Count; i++)
						{
							var asset = assets[i];

							if (!asset)
								continue;

							if (!Application.isBatchMode)
							{
								var progress = assets.Count > 0 ? (float) i / assets.Count : 1f;
								if (EditorUtility.DisplayCancelableProgressBar(TARGETED_TITLE, asset.name, progress))
								{
									errorOrMessage = null;
									return CancelValidation("user");
								}
							}

							if (IsValidationCancellationRequested())
							{
								errorOrMessage = null;
								return CancelValidation();
							}

							if (asset is ContentScriptableObject scriptableObject)
							{
								// Точечная проверка идёт по явному действию пользователя —
								// disabled и SkipValidation здесь не отфильтровываются
								errorCount += ValidateScriptableObjectCore(scriptableObject,
									valueValidators,
									ref warningCount,
									errStringBuilder);
								continue;
							}

							// Префабы и прочие ассеты идут через включённые валидаторы значений —
							// ровно так их видит и полная валидация, встречая ссылку на ассет
							errorCount += ValidateContentValue(asset,
								asset.GetType(),
								asset.name,
								null,
								asset,
								true,
								valueValidators,
								null,
								errStringBuilder);
						}

						if (errorCount > 0)
						{
							var str = $"failed (errors: {errorCount}, warnings️: {warningCount})"
								+ (errStringBuilder.Length > 0
									? ", errors:\n" + errStringBuilder
									: string.Empty);
							errorOrMessage = "Content validation " + str;
							return false;
						}

						errorOrMessage = warningCount > 0
							? $"Validation passed (warnings️: {warningCount})"
							: "Validation passed";
					}
					finally
					{
						_activeErrorMessageBuilder = previousErrorMessageBuilder;
						_activeErrorMessageNumber = previousErrorMessageNumber;
					}
				}
			}
			finally
			{
				if (!Application.isBatchMode)
					EditorUtility.ClearProgressBar();
			}

			return true;
		}
	}
}
