using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Content.Editor
{
	/// <summary>
	/// Быстрая валидация из префаб-мода: transient-оверлей SceneView, появляется только
	/// пока открыт Prefab Stage. Тулбар-раскладка — просто кнопка с иконкой, без панели
	/// с заголовком. Проверяется сохранённый ассет префаба
	/// </summary>
	[Overlay(typeof(SceneView),
		"content-prefab-validation",
		"Content Validation",
		true,
		defaultDockZone = DockZone.Floating,
		defaultLayout = Layout.HorizontalToolbar)]
	public sealed class ContentPrefabStageValidationOverlay : Overlay, ITransientOverlay, ICreateHorizontalToolbar
	{
		private const string LABEL = "Validate";
		private const string VALIDATING_LABEL = "Validating…";
		private const string TOOLTIP = "Провалидировать открытый префаб (сохранённый ассет)";

		private const int SPINNER_FRAMES = 12;
		private const long SPINNER_INTERVAL_MS = 60;

		private static Texture2D _icon;
		private static Texture2D[] _spinner;

		private static bool _validating;

		public bool visible => PrefabStageUtility.GetCurrentPrefabStage() != null;

		public OverlayToolbar CreateHorizontalToolbarContent()
		{
			var toolbar = new OverlayToolbar();
			toolbar.Add(CreateButton());
			return toolbar;
		}

		public override VisualElement CreatePanelContent() => CreateButton();

		private static EditorToolbarButton CreateButton()
		{
			var button = new EditorToolbarButton
			{
				text = LABEL,
				tooltip = TOOLTIP,
				icon = ValidateIcon()
			};

			button.clicked += () => StartValidation(button);
			return button;
		}

		private static void StartValidation(EditorToolbarButton button)
		{
			if (_validating)
				return;

			var stage = PrefabStageUtility.GetCurrentPrefabStage();

			if (stage == null)
				return;

			// Несохранённые правки в ассете не живут — честно предупреждаем, а не валидируем их молча
			if (stage.scene.isDirty &&
				!EditorUtility.DisplayDialog("Validate Prefab",
					"The prefab has unsaved changes — the saved asset will be validated. Continue?",
					"Validate", "Cancel"))
				return;

			var assetPath = stage.assetPath;

			_validating = true;
			button.SetEnabled(false);
			button.text = VALIDATING_LABEL;

			// Спиннер поверх иконки; сам запуск — со следующего кадра, чтобы кнопка успела
			// перерисоваться в состояние «идёт проверка»
			var frame = 0;
			var spin = button.schedule
				.Execute(() => button.icon = SpinnerFrame(frame++))
				.Every(SPINNER_INTERVAL_MS);

			EditorApplication.delayCall += () =>
			{
				try
				{
					var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

					if (prefab)
						ContentValidator.ValidateAsset(prefab);
				}
				finally
				{
					_validating = false;
					spin.Pause();
					button.icon = ValidateIcon();
					button.text = LABEL;
					button.SetEnabled(true);
				}
			};
		}

		private static Texture2D ValidateIcon()
		{
			if (_icon != null)
				return _icon;

			// 32px и почти белый: текстура 16px на retina масштабируется и иконка тонет в фоне
			_icon = SdfIcons.CreateTransparentIconTexture(SdfIconType.ClipboardCheck,
				new Color(0.95f, 0.95f, 0.95f), 32, 32, 2);
			_icon.hideFlags |= HideFlags.DontUnloadUnusedAsset;

			return _icon;
		}

		private static Texture2D SpinnerFrame(int frame)
		{
			if (_spinner == null)
			{
				_spinner = new Texture2D[SPINNER_FRAMES];

				for (var i = 0; i < SPINNER_FRAMES; i++)
					_spinner[i] = EditorGUIUtility.IconContent($"WaitSpin{i:00}").image as Texture2D;
			}

			return _spinner[frame % SPINNER_FRAMES];
		}
	}
}
