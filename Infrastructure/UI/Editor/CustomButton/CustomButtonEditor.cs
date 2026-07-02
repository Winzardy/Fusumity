using System.Collections.Generic;
using Fusumity.Utility;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Editor
{
	[CustomEditor(typeof(CustomButton), false)]
	public class CustomButtonEditor : OdinEditor
	{
		private ButtonEditor _buttonEditor;

		protected override void OnEnable()
		{
			_buttonEditor = (ButtonEditor) CreateEditor(target, typeof(ButtonEditor));
		}

		protected override void OnDisable()
		{
			if (_buttonEditor != null)
				DestroyImmediate(_buttonEditor);
		}

		public override void OnInspectorGUI()
		{
			_buttonEditor.OnInspectorGUI();

			var origin = ForceHideMonoScriptInEditor;
			ForceHideMonoScriptInEditor = true;
			base.OnInspectorGUI();
			ForceHideMonoScriptInEditor = origin;
		}
	}

	/// <summary>
	/// Рисует в Scene View дропдаун выбора состояния для каждого <see cref="CustomButton"/> внутри выделения.
	/// Через глобальный хук (а не OnSceneGUI редактора), чтобы кнопка показывалась, даже когда напрямую
	/// выделен родитель, а сам компонент висит на дочернем объекте.
	/// </summary>
	[InitializeOnLoad]
	internal static class CustomButtonSceneGUI
	{
		// Базовые размеры (при scale = 1).
		private const float BASE_DROPDOWN_WIDTH = 96f;
		private const float BASE_CLICK_WIDTH = 52f;
		private const float BASE_SPACING = 4f;
		private const float BASE_HEIGHT = 26f;
		private const float BASE_OFFSET = 4f;
		private const float BASE_FONT_SIZE = 11f;
		private const float BASE_GROUP_WIDTH = BASE_DROPDOWN_WIDTH + BASE_SPACING + BASE_CLICK_WIDTH;

		// Группа контролов масштабируется относительно ширины кнопки на экране (зависит от зума viewport'а).
		private const float GROUP_WIDTH_RATIO = 0.4f;
		private const float MIN_GROUP_WIDTH = 40f;
		private const float MAX_GROUP_WIDTH = 130f;
		private const int MIN_FONT_SIZE = 6;
		private const int MAX_FONT_SIZE = 20;

		// Сколько держим кнопку в Pressed перед отпусканием, чтобы нажатие было заметно (сек).
		private const double PRESS_DURATION = 0.2;

		private static readonly Color BACKGROUND_COLOR = new(0.3f, 0.3f, 0.3f, 1f);

		private static readonly Vector3[] _corners = new Vector3[4];
		private static readonly List<CustomButton> _buffer = new();
		private static readonly HashSet<CustomButton> _buttons = new();

		private static CustomButton _pressedButton;
		private static double _releaseTime;

		private static GUIStyle _style;

		private static GUIStyle Style
		{
			get => _style ??= new GUIStyle(EditorStyles.miniPullDown)
			{
				// miniPullDown задаёт fixedHeight (~18px) и игнорирует высоту Rect'а — сбрасываем, чтобы тянулся.
				fixedHeight = 0f,
				alignment = TextAnchor.MiddleCenter,
				fontStyle = FontStyle.Bold,
				normal = {textColor = Color.white},
				hover = {textColor = Color.white},
				active = {textColor = Color.white},
				focused = {textColor = Color.white},
			};
		}

		private static GUIStyle _buttonStyle;

		private static GUIStyle ButtonStyle
		{
			get => _buttonStyle ??= new GUIStyle(EditorStyles.miniButton)
			{
				fixedHeight = 0f,
				alignment = TextAnchor.MiddleCenter,
				fontStyle = FontStyle.Bold,
				normal = {textColor = Color.white},
				hover = {textColor = Color.white},
				active = {textColor = Color.white},
				focused = {textColor = Color.white},
			};
		}

		private const string MENU_PATH = "Tools/Other/Custom Button/Scene State Controls";
		private const string ENABLED_PREF_KEY = "CustomButtonSceneGUI.Enabled";

		private static bool Enabled { get => EditorPrefs.GetBool(ENABLED_PREF_KEY, true); set => EditorPrefs.SetBool(ENABLED_PREF_KEY, value); }

		static CustomButtonSceneGUI()
		{
			SceneView.duringSceneGui += OnSceneGUI;
		}

		[MenuItem(MENU_PATH)]
		private static void ToggleEnabled()
		{
			Enabled = !Enabled;
			SceneView.RepaintAll();
		}

		[MenuItem(MENU_PATH, true)]
		private static bool ToggleEnabledValidate()
		{
			Menu.SetChecked(MENU_PATH, Enabled);
			return true;
		}

		private static void OnSceneGUI(SceneView sceneView)
		{
			// Скрываем, если контролы выключены в меню (Tools/Custom Button) или в Scene View отключены Gizmos.
			if (!Enabled || !sceneView.drawGizmos)
				return;

			var selection = Selection.gameObjects;
			if (selection == null || selection.Length == 0)
				return;

			_buttons.Clear();
			foreach (var gameObject in selection)
			{
				if (gameObject == null)
					continue;

				// Пропускаем ассеты-префабы, выбранные в Project: они не открыты для редактирования
				// в сцене, у их RectTransform нет реальных мировых координат.
				if (EditorUtility.IsPersistent(gameObject))
					continue;

				// Вниз по иерархии (компонент на самом объекте или на детях).
				gameObject.transform.GetComponentsInChildren(true, _buffer);
				foreach (var button in _buffer)
				{
					if (button.isActiveAndEnabled)
						_buttons.Add(button);
				}

				// Вверх по иерархии (компонент на родителе выделенного объекта).
				gameObject.transform.GetComponentsInParent(true, _buffer);
				foreach (var button in _buffer)
				{
					if (button.isActiveAndEnabled)
						_buttons.Add(button);
				}
			}

			if (_buttons.Count == 0)
				return;

			Handles.BeginGUI();
			foreach (var button in _buttons)
				Draw(button);
			Handles.EndGUI();
		}

		private static void Draw(CustomButton button)
		{
			if (button.transform is not RectTransform rectTransform)
				return;

			rectTransform.GetWorldCorners(_corners);

			// Верхняя сторона элемента в координатах GUI Scene View.
			var topLeft = HandleUtility.WorldToGUIPoint(_corners[1]);
			var topRight = HandleUtility.WorldToGUIPoint(_corners[2]);
			var topCenter = (topLeft + topRight) * 0.5f;

			// Размер контролов адаптируется к ширине кнопки на экране: при отдалении не разрастаются
			// на весь экран, при приближении не остаются крошечными (с клампом по краям).
			var buttonScreenWidth = Vector2.Distance(topLeft, topRight);
			var groupWidth = Mathf.Clamp(buttonScreenWidth * GROUP_WIDTH_RATIO, MIN_GROUP_WIDTH, MAX_GROUP_WIDTH);
			var scale = groupWidth / BASE_GROUP_WIDTH;

			var height = BASE_HEIGHT * scale;
			var spacing = BASE_SPACING * scale;
			var dropdownWidth = BASE_DROPDOWN_WIDTH * scale;
			var clickWidth = BASE_CLICK_WIDTH * scale;

			// Dropdown + кнопка Click рисуются группой по центру над элементом.
			var x = topCenter.x - groupWidth * 0.5f;
			var y = topCenter.y - height - BASE_OFFSET * scale;

			var dropdownRect = new Rect(x, y, dropdownWidth, height);
			var clickRect = new Rect(dropdownRect.xMax + spacing, y, clickWidth, height);

			var fontSize = Mathf.Clamp(Mathf.RoundToInt(BASE_FONT_SIZE * scale), MIN_FONT_SIZE, MAX_FONT_SIZE);
			Style.fontSize = fontSize;
			ButtonStyle.fontSize = fontSize;

			var content = new GUIContent(button.TargetState.ToLabel());

			var originColor = GUI.backgroundColor;
			GUI.backgroundColor = BACKGROUND_COLOR;

			if (EditorGUI.DropdownButton(dropdownRect, content, FocusType.Passive, Style))
				ShowMenu(button);

			if (GUI.Button(clickRect, "Click", ButtonStyle))
				SimulateClick(button);

			GUI.backgroundColor = originColor;
		}

		private static void ShowMenu(CustomButton button)
		{
			var menu = new GenericMenu();
			var current = button.TargetState.type;

			foreach (var state in ButtonTransitionUtility.GetAll())
			{
				var stateType = state.type;
				menu.AddItem(new GUIContent(state.ToLabel()), current == stateType, () => Select(button, stateType));
			}

			menu.ShowAsContext();
		}

		private static void Select(CustomButton button, int state)
		{
			if (button.TargetState.type == state)
				return;

			// DoStateTransition тоглит, но из любого отличного состояния он переводит ровно в выбранное.
			button.DoStateTransition(state);
		}

		private static void SimulateClick(CustomButton button)
		{
			// Завершаем предыдущее нажатие, если оно ещё держится.
			Release();

			// Нажимаем (кнопка уходит в Pressed) и держим PRESS_DURATION, чтобы нажатие было заметно.
			// Отпускание и сам клик (onClick) выполняются отложенно в Release.
			var eventData = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerDownHandler);

			_pressedButton = button;
			_releaseTime = EditorApplication.timeSinceStartup + PRESS_DURATION;
			EditorApplication.update += UpdatePress;
			SceneView.RepaintAll();
		}

		private static void UpdatePress()
		{
			if (_pressedButton != null && EditorApplication.timeSinceStartup < _releaseTime)
				return;

			Release();
		}

		private static void Release()
		{
			EditorApplication.update -= UpdatePress;

			var button = _pressedButton;
			_pressedButton = null;

			if (button == null)
				return;

			// Отпускаем (возврат из Pressed) и шлём клик — здесь срабатывает onClick.
			// В Play Mode — как настоящий клик; в Edit Mode сработают только persistent-листенеры onClick.
			var eventData = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerUpHandler);
			ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerClickHandler);
			SceneView.RepaintAll();
		}
	}
}
