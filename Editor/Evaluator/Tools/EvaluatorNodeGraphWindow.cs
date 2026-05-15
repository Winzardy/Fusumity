using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sapientia;
using Sapientia.Conditions;
using Sapientia.Evaluators;
using Sapientia.Extensions;
using Sirenix.Config;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fusumity.Editor
{
	using UnityDirection = UnityEditor.Experimental.GraphView.Direction;
	using UnityObject = UnityEngine.Object;

	public class EvaluatorNodeGraphWindow : OdinEditorWindow
	{
		private const string TITLE = "Evaluator Node Graph";
		private const string OPEN_MENU_PATH = "Edit in Node Graph";

		private const float NODE_WIDTH = 270f;
		private const float ROOT_NODE_WIDTH = 120f;
		private const float LEVEL_WIDTH = 360f;
		private const float ROW_HEIGHT = 22f;
		private const float NODE_VERTICAL_SPACE = 26f;
		private const double AUTO_BAKE_INTERVAL = 1d;
		private const double SELECTION_CLOSE_GRACE_INTERVAL = 1d;
		private const string SAVE_BUTTON_NAME = "save";
		private const string AUTO_SAVE_PREFS_KEY = "Fusumity.EvaluatorNodeGraphWindow.AutoSave";

		private static readonly Dictionary<Type, TypePresentation> _typePresentationCache = new();
		private static int _inlineNodeRenderDepth;

		private readonly EvaluatorGraphBuilder _builder = new();

		private EvaluatorGraphView _graphView;
		private InspectorProperty _targetProperty;
		private IEvaluator _root;
		private Type _rootTargetType;
		private UnityObject _owner;
		private string _sourceLabel;
		private Label _sourceInfoLabel;
		private Toggle _autoSaveToggle;
		private bool _hasRootSelection;
		private bool _dirty;
		private double _lastAutoBakeTime;
		private double _selectionCloseSuppressedUntil;
		private Button _saveButton;

		public static void AddOpenAttributes(List<Attribute> attributes)
		{
			if (IsInlineNodeRendering)
				return;

			attributes.Add(new CustomContextMenuAttribute(
				OPEN_MENU_PATH,
				$"@{nameof(EvaluatorNodeGraphWindow)}.{nameof(Open)}($property)"));
			attributes.Add(new OnInspectorGUIAttribute(
				$"@{nameof(EvaluatorNodeGraphWindow)}.{nameof(DrawButtonRaycast)}($property)",
				append: false));
			attributes.Add(new OnInspectorGUIAttribute(
				$"@{nameof(EvaluatorNodeGraphWindow)}.{nameof(DrawButtonIcon)}($property)",
				append: true));
		}

		public static void Open(InspectorProperty property)
		{
			if (IsInlineNodeRendering)
				return;

			if (property?.ValueEntry == null || !typeof(IEvaluator).IsAssignableFrom(property.ValueEntry.TypeOfValue))
			{
				EditorUtility.DisplayDialog(TITLE, "Selected property is not an evaluator.", "OK");
				return;
			}

			var owner = property.SerializationRoot?.ValueEntry?.WeakSmartValue as UnityObject;
			var evaluator = property.ValueEntry.WeakSmartValue as IEvaluator;
			var createdEvaluator = false;
			if (evaluator == null)
			{
				evaluator = CreateDefaultEvaluator(property.ValueEntry.BaseValueType ?? property.ValueEntry.TypeOfValue);

				if (evaluator != null)
				{
					createdEvaluator = true;
					if (owner)
						Undo.RecordObject(owner, "Create Evaluator");

					property.ValueEntry.WeakSmartValue = evaluator;
					property.MarkSerializationRootDirty();
					property.ValueEntry.ApplyChanges();

					if (owner)
						EditorUtility.SetDirty(owner);
				}
			}

			if (evaluator == null)
			{
				EditorUtility.DisplayDialog(TITLE, $"Can't create default evaluator for {property.ValueEntry.TypeOfValue.GetNiceName()}.", "OK");
				return;
			}

			var sourceLabel = property.Path;
			if (createdEvaluator)
			{
				EditorApplication.delayCall += () => OpenWindow(evaluator, owner, sourceLabel, property);
				return;
			}

			OpenWindow(evaluator, owner, sourceLabel, property);
		}

		private static void OpenWindow(IEvaluator evaluator, UnityObject owner, string sourceLabel, InspectorProperty property)
		{
			if (evaluator == null)
				return;

			var window = GetWindow<EvaluatorNodeGraphWindow>(TITLE);
			window._hasRootSelection              = false;
			window._selectionCloseSuppressedUntil = EditorApplication.timeSinceStartup + SELECTION_CLOSE_GRACE_INTERVAL;
			window.Show();
			window.SetRoot(evaluator, owner, sourceLabel, property);
		}

		public static void DrawButtonRaycast(InspectorProperty property)
		{
			if (!ShouldDrawOpenButton(property))
				return;

			var iconRect = GetOpenButtonIconRect(property);
			var content = new GUIContent(string.Empty, "Открыть нодовый редактор");

			if (GUI.Button(iconRect, content, GUIStyle.none))
			{
				Open(property);
			}
		}

		public static void DrawButtonIcon(InspectorProperty property)
		{
			if (!ShouldDrawOpenButton(property))
				return;

			var iconRect = GetOpenButtonIconRect(property);
			var color = GUI.color;
			GUI.color = iconRect.Contains(Event.current.mousePosition) ? Color.white : color * 0.75f;
			SdfIcons.DrawIcon(iconRect, SdfIconType.Diagram2Fill);
			GUI.color = color;
		}

		private static Rect GetOpenButtonIconRect(InspectorProperty property)
		{
			var rect = property.LastDrawnValueRect;
			var buttonRect = rect.AlignRight(32.5f);
			var iconRect = new Rect(buttonRect.x + 4f, buttonRect.y + 3.5f, 11f, 11f);
			if (typeof(IConstantEvaluator).IsAssignableFrom(property.ValueEntry.TypeOfValue))
				iconRect.x -= 5f;

			return iconRect;
		}

		private static bool ShouldDrawOpenButton(InspectorProperty property)
		{
			if (IsInlineNodeRendering)
				return false;

			if (property?.ValueEntry == null || !typeof(IEvaluator).IsAssignableFrom(property.ValueEntry.TypeOfValue))
				return false;

			var parentType = property.Parent?.ValueEntry?.BaseValueType;
			return parentType == null || !typeof(IEvaluator).IsAssignableFrom(parentType);
		}

		internal static bool IsInlineNodeRendering => _inlineNodeRenderDepth > 0;

		private static void BeginInlineNodeRendering()
			=> _inlineNodeRenderDepth++;

		private static void EndInlineNodeRendering()
		{
			if (_inlineNodeRenderDepth > 0)
				_inlineNodeRenderDepth--;
		}

		private void CreateGUI()
		{
			titleContent = new GUIContent(TITLE);

			var header = new VisualElement
			{
				style =
				{
					flexDirection   = FlexDirection.Row,
					alignItems      = Align.Center,
					paddingLeft     = 6,
					paddingRight    = 6,
					paddingTop      = 4,
					paddingBottom   = 4,
					backgroundColor = new Color(0.16f, 0.16f, 0.16f)
				}
			};

			header.Add(new Button(Rebuild)
			{
				text    = "Refresh",
				tooltip = "Перестроить граф по текущим данным evaluator"
			});

			header.Add(new Button(() => _graphView?.FrameAll())
			{
				text    = "Focus",
				tooltip = "Показать все ноды"
			});

			_sourceInfoLabel = new Label
			{
				style =
				{
					flexGrow       = 1,
					marginLeft     = 8,
					color          = new Color(0.78f, 0.78f, 0.78f),
					unityTextAlign = TextAnchor.MiddleLeft
				}
			};
			header.Add(_sourceInfoLabel);

			_autoSaveToggle = new Toggle("Auto Save")
			{
				value   = EditorPrefs.GetBool(AUTO_SAVE_PREFS_KEY, true),
				tooltip = "Автоматически выполнять Save примерно раз в секунду после изменений графа",
				style =
				{
					marginLeft  = 8,
					marginRight = 4,
				}
			};
			_autoSaveToggle.Q<Label>().style.unityTextAlign = TextAnchor.MiddleRight;
			_autoSaveToggle.RegisterValueChangedCallback(evt =>
			{
				EditorPrefs.SetBool(AUTO_SAVE_PREFS_KEY, evt.newValue);
				_saveButton?.SetEnabled(!evt.newValue && _dirty);
			});
			header.Add(_autoSaveToggle);

			_saveButton = new Button(Save)
			{
				name    = SAVE_BUTTON_NAME,
				text    = "Save",
				tooltip = "Записать текущие связи графа обратно в evaluator-поле"
			};
			header.Add(_saveButton);
			_saveButton.SetEnabled(false);

			rootVisualElement.Add(header);

			_graphView = new EvaluatorGraphView(_builder, MarkDirty)
			{
				style =
				{
					flexGrow = 1
				}
			};
			rootVisualElement.Add(_graphView);

			Rebuild();
		}

		private void Update()
		{
			_saveButton.SetEnabled(!_autoSaveToggle.value && _dirty);

			if (CloseIfNoSelectedEvaluator())
				return;

			if (!_dirty)
				return;

			if (_autoSaveToggle?.value != true)
				return;

			if (EditorApplication.timeSinceStartup - _lastAutoBakeTime < AUTO_BAKE_INTERVAL)
				return;

			Save(false);
		}

		private void SetRoot(IEvaluator root, UnityObject owner, string sourceLabel, InspectorProperty targetProperty)
		{
			var targetType = ResolveRootTargetType(root, targetProperty);
			_root                          = root;
			_owner                         = owner;
			_sourceLabel                   = sourceLabel;
			_targetProperty                = targetProperty;
			_rootTargetType                = targetType;
			_hasRootSelection              = true;
			_dirty                         = false;
			_selectionCloseSuppressedUntil = EditorApplication.timeSinceStartup + SELECTION_CLOSE_GRACE_INTERVAL;

			Rebuild();
		}

		private void Rebuild()
		{
			if (_graphView == null)
				return;

			if (CloseIfNoSelectedEvaluator())
				return;

			_graphView.ClearGraph();

			var graph = _builder.Build(_root, _rootTargetType);
			_graphView.Draw(graph);

			if (_dirty)
			{
				_dirty = false;
				UpdateSourceInfo(MakeSourceText());
				titleContent.text = titleContent.text[..^1];
			}
		}

		private bool CloseIfNoSelectedEvaluator()
		{
			if (_dirty)
				return false;

			if (!_hasRootSelection)
				return false;

			if (EditorApplication.timeSinceStartup < _selectionCloseSuppressedUntil)
				return false;

			if (HasSelectedEvaluator())
				return false;

			Close();
			return true;
		}

		private bool HasSelectedEvaluator()
		{
			if (_root == null)
				return false;

			if (_targetProperty?.ValueEntry == null)
				return true;

			var targetType = _targetProperty.ValueEntry.BaseValueType ?? _targetProperty.ValueEntry.TypeOfValue;
			if (typeof(IEvaluator).IsAssignableFrom(targetType))
			{
				var selectedValue = _targetProperty.ValueEntry.WeakSmartValue;
				return selectedValue is IEvaluator || IsNullNoneConditionSelection(targetType, selectedValue);
			}

			if (typeof(IEvaluatedValue).IsAssignableFrom(targetType))
			{
				var boxedValue = _targetProperty.ValueEntry.WeakSmartValue;
				if (boxedValue == null)
					return false;

				return GetFieldInHierarchy(boxedValue.GetType(), "evaluator")?.GetValue(boxedValue) is IEvaluator;
			}

			return true;
		}

		private bool IsNullNoneConditionSelection(Type targetType, object selectedValue)
		{
			if (selectedValue != null || _root == null)
				return false;

			var rootType = _root.GetType();
			return IsNoneConditionType(rootType) &&
				(targetType == null || targetType.IsAssignableFrom(rootType));
		}

		private static bool IsNoneConditionType(Type type)
			=> type?.IsGenericType == true && type.GetGenericTypeDefinition() == typeof(NoneCondition<>);

		private void Save()
			=> Save(true);

		private bool Save(bool showWarnings)
		{
			if (_graphView == null || _root == null)
				return false;

			if (_graphView.TryGetEmptyGraphWarning(out var warning))
			{
				if (!showWarnings)
				{
					_lastAutoBakeTime = EditorApplication.timeSinceStartup;
					UpdateSourceInfo(MakeSourceText() + " !");
					return false;
				}

				if (!EditorUtility.DisplayDialog(TITLE, $"{warning}\n\nSave anyway?", "Save anyway", "Cancel"))
					return false;
			}

			var bakedRoot = _graphView.Bake();
			if (bakedRoot == null)
				return false;

			_root = bakedRoot;

			if (_targetProperty?.ValueEntry != null)
			{
				if (_owner)
					Undo.RecordObject(_owner, "Save Evaluator Graph");

				var targetType = _targetProperty.ValueEntry.BaseValueType ?? _targetProperty.ValueEntry.TypeOfValue;
				if (typeof(IEvaluator).IsAssignableFrom(targetType))
				{
					_targetProperty.ValueEntry.WeakSmartValue = bakedRoot;
				}
				else if (typeof(IEvaluatedValue).IsAssignableFrom(targetType))
				{
					var boxedValue = _targetProperty.ValueEntry.WeakSmartValue;
					if (boxedValue != null)
					{
						GetFieldInHierarchy(boxedValue.GetType(), "evaluator")?.SetValue(boxedValue, bakedRoot);
						_targetProperty.ValueEntry.WeakSmartValue = boxedValue;
					}
				}

				_targetProperty.MarkSerializationRootDirty();
				_targetProperty.ValueEntry.ApplyChanges();

				if (_owner)
					EditorUtility.SetDirty(_owner);
			}

			if (_dirty)
			{
				_dirty            = false;
				_lastAutoBakeTime = EditorApplication.timeSinceStartup;
				UpdateSourceInfo(MakeSourceText());
				titleContent.text = titleContent.text[..^1];
			}

			return true;
		}

		private void MarkDirty()
		{
			if (!_dirty)
			{
				_dirty = true;
				UpdateSourceInfo(MakeSourceText() + "*");
				titleContent.text += "*";
			}
		}

		private string MakeSourceText()
		{
			var ownerName = _owner ? _owner.name : "runtime object";
			if (string.IsNullOrEmpty(_sourceLabel))
				return ownerName;

			return $"Selected: {ownerName} / {_sourceLabel}";
		}

		private void UpdateSourceInfo(string text)
		{
			if (_sourceInfoLabel != null)
				_sourceInfoLabel.text = text;
		}

		private static IEvaluator CreateDefaultEvaluator(Type targetType)
		{
			if (TryGetConditionContextType(targetType, out var conditionContextType))
				return (IEvaluator) Activator.CreateInstance(typeof(NoneCondition<>).MakeGenericType(conditionContextType));

			if (TryGetEvaluatorArgumentTypes(targetType, out var contextType, out var valueType))
				return (IEvaluator) Activator.CreateInstance(typeof(ConstantEvaluator<,>).MakeGenericType(contextType, valueType));

			if (!targetType.IsAbstract && !targetType.IsInterface && targetType.GetConstructor(Type.EmptyTypes) != null)
				return Activator.CreateInstance(targetType) as IEvaluator;

			return null;
		}

		private static Type ResolveRootTargetType(IEvaluator root, InspectorProperty targetProperty)
		{
			var targetType = targetProperty?.ValueEntry?.BaseValueType ?? targetProperty?.ValueEntry?.TypeOfValue;
			if (targetType == null)
				return root?.GetType() ?? typeof(IEvaluator);

			if (typeof(IEvaluatedValue).IsAssignableFrom(targetType))
				return GetEvaluatedValueTargetType(targetType);

			return targetType;
		}

		private static Type GetEvaluatedValueTargetType(Type type)
		{
			if (type?.IsGenericType != true || type.GetGenericArguments().Length < 2)
				return typeof(IEvaluator);

			var args = type.GetGenericArguments();
			return typeof(Evaluator<,>).MakeGenericType(args[0], args[1]);
		}

		private static bool TryGetConditionContextType(Type type, out Type contextType)
		{
			contextType = null;

			if (type == null)
				return false;

			if (type.IsGenericType &&
				(type.GetGenericTypeDefinition() == typeof(Condition<>) ||
					type.GetGenericTypeDefinition() == typeof(ICondition<>)))
			{
				contextType = type.GetGenericArguments()[0];
				return true;
			}

			foreach (var interfaceType in type.GetInterfaces())
			{
				if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != typeof(ICondition<>))
					continue;

				contextType = interfaceType.GetGenericArguments()[0];
				return true;
			}

			return false;
		}

		private static bool TryGetEvaluatorArgumentTypes(Type type, out Type contextType, out Type valueType)
		{
			contextType = null;
			valueType   = null;

			if (type == null)
				return false;

			if (type.IsGenericType &&
				(type.GetGenericTypeDefinition() == typeof(Evaluator<,>) ||
					type.GetGenericTypeDefinition() == typeof(IEvaluator<,>)))
			{
				var args = type.GetGenericArguments();
				contextType = args[0];
				valueType   = args[1];
				return true;
			}

			foreach (var interfaceType in type.GetInterfaces())
			{
				if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != typeof(IEvaluator<,>))
					continue;

				var args = interfaceType.GetGenericArguments();
				contextType = args[0];
				valueType   = args[1];
				return true;
			}

			return false;
		}

		private static string GetTypeName(Type type)
		{
			if (type == null)
				return "Unknown";

			return GetTypePresentation(type).Name;
		}

		private static TypePresentation GetTypePresentation(Type type)
		{
			if (_typePresentationCache.TryGetValue(type, out var cached))
				return cached;

			var name = type.GetNiceName();
			var icon = SdfIconType.None;
			var iconColor = typeof(ICondition).IsAssignableFrom(type)
				? EvaluatorTypeRegistryConstants.CONDITION_COLOR
				: EvaluatorTypeRegistryConstants.EVALUATOR_COLOR;

			ApplyTypeRegistryAttribute(type, ref name, ref icon);
			ApplyKnownGenericPresentation(type, ref name, ref icon, ref iconColor);

			try
			{
				foreach (var candidate in GetTypeRegistryCandidates(type).Reverse())
				{
					var settings = TypeRegistryUserConfig.Instance.TryGetSettings(candidate);
					if (settings == null)
						continue;

					if (!string.IsNullOrWhiteSpace(settings.Name))
						name = settings.Name;

					if (settings.Icon != SdfIconType.None)
						icon = settings.Icon;
				}
			}
			catch
			{
				// Odin config can be unavailable during domain reload; the graph still works with reflection names.
			}

			var presentation = new TypePresentation(CleanTypeName(name), icon, iconColor);
			_typePresentationCache[type] = presentation;
			return presentation;
		}

		private static void ApplyKnownGenericPresentation(
			Type type,
			ref string name,
			ref SdfIconType icon,
			ref Color iconColor)
		{
			if (!type.IsGenericType)
				return;

			var genericDefinition = type.GetGenericTypeDefinition();
			if (genericDefinition == typeof(IfElseEvaluator<,>))
			{
				name      = "If / else";
				icon      = SdfIconType.Alt;
				iconColor = EvaluatorTypeRegistryConstants.EVALUATOR_COLOR;
			}
			else if (genericDefinition == typeof(ConstantEvaluator<,>))
			{
				name      = "Constant";
				icon      = SdfIconType.DiamondFill;
				iconColor = EvaluatorTypeRegistryConstants.EVALUATOR_COLOR;
			}
			else if (genericDefinition == typeof(IfElseCondition<>))
			{
				name      = "If / else";
				icon      = SdfIconType.Alt;
				iconColor = EvaluatorTypeRegistryConstants.CONDITION_COLOR;
			}
			else if (genericDefinition == typeof(NoneCondition<>))
			{
				name      = EvaluatorTypeRegistryConstants.NONE_CONDITION_LABEL;
				icon      = EvaluatorTypeRegistryConstants.NONE_CONDITION_SDF_ICON;
				iconColor = EvaluatorTypeRegistryConstants.CONDITION_COLOR;
			}
			else if (genericDefinition == typeof(RejectCondition<>))
			{
				name      = EvaluatorTypeRegistryConstants.REJECT_CONDITION_LABEL;
				icon      = EvaluatorTypeRegistryConstants.REJECT_CONDITION_SDF_ICON;
				iconColor = EvaluatorTypeRegistryConstants.CONDITION_COLOR;
			}
		}

		private static IEnumerable<Type> GetTypeRegistryCandidates(Type type)
		{
			var used = new HashSet<Type>();
			for (var current = type; current != null && current != typeof(object); current = current.BaseType)
			{
				if (used.Add(current))
					yield return current;

				if (current.IsGenericType)
				{
					var genericDefinition = current.GetGenericTypeDefinition();
					if (used.Add(genericDefinition))
						yield return genericDefinition;
				}
			}
		}

		private static void ApplyTypeRegistryAttribute(Type type, ref string name, ref SdfIconType icon)
		{
			for (var current = type; current != null && current != typeof(object); current = current.BaseType)
			{
				foreach (var attribute in current.GetCustomAttributesData())
				{
					if (attribute.AttributeType.FullName != "Sirenix.OdinInspector.TypeRegistryItemAttribute")
						continue;

					if (attribute.ConstructorArguments.Count > 0 &&
						attribute.ConstructorArguments[0].Value is string attributeName &&
						!string.IsNullOrWhiteSpace(attributeName))
					{
						name = attributeName;
					}

					if (attribute.ConstructorArguments.Count > 2 &&
						TryReadIcon(attribute.ConstructorArguments[2].Value, out var constructorIcon))
					{
						icon = constructorIcon;
					}

					foreach (var argument in attribute.NamedArguments)
					{
						if (argument.MemberName == "Name" &&
							argument.TypedValue.Value is string namedName &&
							!string.IsNullOrWhiteSpace(namedName))
						{
							name = namedName;
						}
						else if (argument.MemberName == "Icon" &&
							TryReadIcon(argument.TypedValue.Value, out var namedIcon))
						{
							icon = namedIcon;
						}
					}

					return;
				}
			}
		}

		private static bool TryReadIcon(object value, out SdfIconType icon)
		{
			if (value is SdfIconType sdfIcon)
			{
				icon = sdfIcon;
				return true;
			}

			if (value is int intValue)
			{
				icon = (SdfIconType) intValue;
				return true;
			}

			icon = SdfIconType.None;
			return false;
		}

		private static string CleanTypeName(string value)
		{
			value = value?.Trim();
			return string.IsNullOrEmpty(value) ? "Unknown" : value;
		}

		private static string FormatCollectionPortLabel(int index)
			=> $"{index}";

		private static string FormatFieldPortLabel(string label)
		{
			if (string.IsNullOrEmpty(label))
				return label;

			return label.Length == 1
				? label
				: ObjectNames.NicifyVariableName(label);
		}

		private readonly struct TypePresentation
		{
			public readonly string Name;
			public readonly SdfIconType Icon;
			public readonly Color IconColor;

			public TypePresentation(string name, SdfIconType icon, Color iconColor)
			{
				Name      = name;
				Icon      = icon;
				IconColor = iconColor;
			}
		}

		private sealed class EvaluatorGraphView : GraphView
		{
			private readonly EvaluatorGraphBuilder _builder;
			private readonly Action _onChanged;
			private readonly EvaluatorEdgeConnectorListener _edgeConnectorListener;
			private readonly Dictionary<EvaluatorGraphNodeModel, EvaluatorNodeView> _modelToView = new();
			private readonly Dictionary<EvaluatorGraphPortModel, Port> _portToView = new();
			private static EvaluatorClipboard _clipboard;

			private EvaluatorGraphModel _graph;
			private VisualElement _typePickerOverlay;
			private IMGUIContainer _spacePanCursorOverlay;
			private Vector2 _mousePosition;
			private Vector2 _spacePanLastMousePosition;
			private bool _isClearingGraph;
			private bool _spacePanActive;
			private bool _isSpacePanning;

			public EvaluatorGraphView(EvaluatorGraphBuilder builder, Action onChanged)
			{
				_builder               = builder;
				_onChanged             = onChanged;
				_edgeConnectorListener = new EvaluatorEdgeConnectorListener(this);
				focusable              = true;

				Insert(0, new GridBackground());
				SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
				this.AddManipulator(new ContentDragger());
				this.AddManipulator(new SelectionDragger());
				this.AddManipulator(new RectangleSelector());
				this.AddManipulator(new ContextualMenuManipulator(evt => PopulateGraphMenu(evt)));
				CreateSpacePanCursorOverlay();
				RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
				RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
				RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
				RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
				RegisterCallback<KeyUpEvent>(OnKeyUp, TrickleDown.TrickleDown);
				RegisterCallback<BlurEvent>(_ => StopSpacePan());
				graphViewChanged = OnGraphViewChanged;
			}

			private void CreateSpacePanCursorOverlay()
			{
				_spacePanCursorOverlay = new IMGUIContainer(() =>
				{
					if (!_spacePanActive)
						return;

					var width = _spacePanCursorOverlay.resolvedStyle.width;
					var height = _spacePanCursorOverlay.resolvedStyle.height;
					if (width <= 0f || height <= 0f)
						return;

					EditorGUIUtility.AddCursorRect(new Rect(0f, 0f, width, height), MouseCursor.Pan);
				})
				{
					pickingMode = PickingMode.Ignore,
					style =
					{
						position = Position.Absolute,
						left     = 0,
						top      = 0,
						right    = 0,
						bottom   = 0,
						display  = DisplayStyle.None
					}
				};
				Add(_spacePanCursorOverlay);
			}

			public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
			{
				var result = new List<Port>();
				foreach (var port in ports.ToList())
				{
					var portNode = port.GetFirstAncestorOfType<Node>();
					var startNode = startPort.GetFirstAncestorOfType<Node>();
					if (port == startPort || portNode == startNode || port.direction == startPort.direction)
						continue;

					var output = startPort.direction == UnityDirection.Output ? startPort : port;
					var input = startPort.direction == UnityDirection.Input ? startPort : port;

					if (output.userData is not EvaluatorGraphPortModel outputModel ||
						input.userData is not EvaluatorGraphNodeModel inputModel)
						continue;

					if (CanConnect(outputModel, inputModel))
						result.Add(port);
				}

				return result;
			}

			private static bool CanConnect(EvaluatorGraphPortModel outputModel, EvaluatorGraphNodeModel inputModel)
			{
				if (outputModel?.TargetType == null || inputModel == null)
					return false;

				return inputModel.Kind switch
				{
					EvaluatorGraphNodeKind.Evaluator => inputModel.EvaluatorType != null &&
						outputModel.TargetType.IsAssignableFrom(inputModel.EvaluatorType),
					EvaluatorGraphNodeKind.ConstantValue => inputModel.ConstantTargetType != null &&
						outputModel.TargetType.IsAssignableFrom(inputModel.ConstantTargetType),
					_ => false
				};
			}

			public void ClearGraph()
			{
				HideTypePickerOverlay();

				var elements = graphElements.ToList();
				if (elements.Count > 0)
				{
					_isClearingGraph = true;
					try
					{
						DeleteElements(elements);
					}
					finally
					{
						_isClearingGraph = false;
					}
				}

				_modelToView.Clear();
				_portToView.Clear();
				_graph = null;
			}

			public void Draw(EvaluatorGraphModel graph)
			{
				ClearGraph();
				_graph = graph;

				if (graph?.Root == null)
					return;

				Layout(graph.Root);

				foreach (var model in graph.Nodes)
					CreateNodeView(model, model.Position);

				foreach (var model in graph.Nodes)
				{
					foreach (var edgeModel in model.Edges)
						Connect(edgeModel.Port, edgeModel.Child, false);

					if (_modelToView.TryGetValue(model, out var view))
						view.RefreshNodeState();
				}

				schedule.Execute(() => FrameAll()).StartingIn(80);
			}

			public IEvaluator Bake()
			{
				if (_graph?.Root == null)
					return null;

				var connections = CollectConnections();
				var rootPort = _graph.RootPort;
				if (rootPort == null)
					return null;

				connections.TryGetValue(rootPort, out var rootModel);
				return BakeChildEvaluator(rootModel, _graph.RootTargetType, connections, new HashSet<EvaluatorGraphNodeModel>());
			}

			public bool TryGetEmptyGraphWarning(out string warning)
			{
				warning = null;
				if (_graph?.Root == null)
					return false;

				var connections = CollectConnections();
				var missingConnections = new List<string>();

				var rootPort = _graph.RootPort;
				if (rootPort == null || !connections.ContainsKey(rootPort))
					missingConnections.Add("Root output is not connected.");

				var reachableNodes = CollectReachableNodes(connections);
				foreach (var model in reachableNodes)
				{
					foreach (var port in model.Ports)
					{
						if (port.IsCollectionAppend || connections.ContainsKey(port))
							continue;

						missingConnections.Add($"{FormatNodeName(model)} / {FormatPortName(port)}");
					}
				}

				var disconnectedNodes = _graph.Nodes
					.Where(x => x != _graph.Root && !reachableNodes.Contains(x))
					.Select(FormatNodeName)
					.ToList();

				if (missingConnections.Count == 0 && disconnectedNodes.Count == 0)
					return false;

				var lines = new List<string> {"Graph has empty or disconnected nodes:"};
				AppendLimitedLines(lines, "Empty slots", missingConnections);
				AppendLimitedLines(lines, "Disconnected nodes", disconnectedNodes);
				warning = string.Join("\n", lines);
				return true;
			}

			private HashSet<EvaluatorGraphNodeModel> CollectReachableNodes(
				IReadOnlyDictionary<EvaluatorGraphPortModel, EvaluatorGraphNodeModel> connections)
			{
				var result = new HashSet<EvaluatorGraphNodeModel>();
				var stack = new Stack<EvaluatorGraphNodeModel>();
				if (_graph?.RootPort != null && connections.TryGetValue(_graph.RootPort, out var rootModel) && rootModel != null)
					stack.Push(rootModel);

				while (stack.Count > 0)
				{
					var model = stack.Pop();
					if (model == null || !result.Add(model))
						continue;

					foreach (var port in model.Ports)
					{
						if (connections.TryGetValue(port, out var childModel) && childModel != null)
							stack.Push(childModel);
					}
				}

				return result;
			}

			private static void AppendLimitedLines(List<string> lines, string title, IReadOnlyList<string> values)
			{
				if (values.Count == 0)
					return;

				lines.Add($"{title}:");
				var count = Mathf.Min(values.Count, 8);
				for (var i = 0; i < count; i++)
					lines.Add($"- {values[i]}");

				if (values.Count > count)
					lines.Add($"- ... and {values.Count - count} more");
			}

			private static string FormatNodeName(EvaluatorGraphNodeModel model)
			{
				if (model == null)
					return "Unknown";

				if (model.Kind == EvaluatorGraphNodeKind.Root)
					return "Root";

				return string.IsNullOrWhiteSpace(model.SourceLabel)
					? model.Title
					: $"{model.Title} ({model.SourceLabel})";
			}

			private static string FormatPortName(EvaluatorGraphPortModel port)
			{
				if (port == null)
					return "Unknown";

				if (!string.IsNullOrWhiteSpace(port.Label))
					return port.Label;

				return port.Field?.Name ?? "output";
			}

			private void PopulateGraphMenu(ContextualMenuPopulateEvent evt)
			{
				if (evt.target is VisualElement target &&
					(target is Port ||
						target.GetFirstAncestorOfType<Port>() != null ||
						target is Node ||
						target.GetFirstAncestorOfType<Node>() != null))
				{
					return;
				}

				var rootTargetType = _graph?.RootTargetType ?? typeof(IEvaluator);
				evt.menu.AppendAction("Create Node...", _ =>
				{
					var position = _mousePosition;
					OpenTypePicker(rootTargetType, evaluator => CreateStandaloneNode(evaluator, position));
				});

				if (_clipboard != null)
					evt.menu.AppendAction("Paste Node", _ => PasteNode(_mousePosition));
			}

			private GraphViewChange OnGraphViewChanged(GraphViewChange change)
			{
				if (_isClearingGraph)
					return change;

				var changed = false;
				var collectionsToNormalize = new List<EvaluatorGraphPortModel>();

				if (change.edgesToCreate != null)
				{
					foreach (var edge in change.edgesToCreate)
					{
						RemoveOutputEdges(edge.output, edge, edge.input);
						if (edge.output?.userData is EvaluatorGraphPortModel portModel)
							PromoteCollectionAppendPort(portModel);
					}

					changed = true;
				}

				if (change.elementsToRemove != null)
				{
					var blockedRootDelete = false;
					EvaluatorNodeView rootView = null;
					if (_graph?.Root != null &&
						_modelToView.TryGetValue(_graph.Root, out rootView) &&
						change.elementsToRemove.Contains(rootView))
					{
						change.elementsToRemove.Remove(rootView);
						blockedRootDelete = true;
					}

					if (blockedRootDelete)
					{
						var removedNodes = new HashSet<Node>(change.elementsToRemove
							.OfType<EvaluatorNodeView>()
							.Where(x => x != rootView)
							.Cast<Node>());

						change.elementsToRemove.RemoveAll(x =>
						{
							if (x is not Edge edge)
								return false;

							var inputNode = edge.input?.GetFirstAncestorOfType<Node>();
							var outputNode = edge.output?.GetFirstAncestorOfType<Node>();
							if (inputNode != rootView && outputNode != rootView)
								return false;

							var otherNode = inputNode == rootView ? outputNode : inputNode;
							return otherNode == null || !removedNodes.Contains(otherNode);
						});
					}

					foreach (var edge in change.elementsToRemove.OfType<Edge>())
					{
						if (edge.output?.userData is EvaluatorGraphPortModel portModel)
							collectionsToNormalize.Add(portModel);
					}

					foreach (var nodeView in change.elementsToRemove.OfType<EvaluatorNodeView>())
					{
						foreach (var edge in edges.ToList())
						{
							if (edge.input?.GetFirstAncestorOfType<Node>() == nodeView &&
								edge.output?.userData is EvaluatorGraphPortModel portModel)
							{
								collectionsToNormalize.Add(portModel);
							}
						}
					}

					foreach (var nodeView in change.elementsToRemove.OfType<EvaluatorNodeView>().ToArray())
					{
						if (nodeView.userData is EvaluatorGraphNodeModel model)
							RemoveNodeModel(model);
					}

					if (change.elementsToRemove.Any(x => x is Edge || x is EvaluatorNodeView))
						changed = true;
				}

				if (changed)
					_onChanged?.Invoke();

				ScheduleNormalizeCollectionPorts(collectionsToNormalize);

				return change;
			}

			private void OnMouseMove(MouseMoveEvent evt)
			{
				_mousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);

				if (!_isSpacePanning)
					return;

				var delta = evt.mousePosition - _spacePanLastMousePosition;
				_spacePanLastMousePosition = evt.mousePosition;
				UpdateViewTransform(
					contentViewContainer.transform.position + (Vector3) delta,
					contentViewContainer.transform.scale);
				evt.StopImmediatePropagation();
			}

			private void OnMouseDown(MouseDownEvent evt)
			{
				_mousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
				var insideTypePicker = IsEventTargetInsideTypePicker(evt);
				if (insideTypePicker)
					return;

				if (_typePickerOverlay != null)
					HideTypePickerOverlay();

				Focus();

				if (!_spacePanActive || evt.button != 0)
					return;

				_isSpacePanning            = true;
				_spacePanLastMousePosition = evt.mousePosition;
				this.CaptureMouse();
				evt.StopImmediatePropagation();
			}

			private void OnMouseUp(MouseUpEvent evt)
			{
				if (!_isSpacePanning)
					return;

				_isSpacePanning = false;
				this.ReleaseMouse();
				evt.StopImmediatePropagation();
			}

			private void OnKeyDown(KeyDownEvent evt)
			{
				if (evt.keyCode == KeyCode.Space)
				{
					SetSpacePanActive(true);
					evt.StopImmediatePropagation();
					return;
				}

				if (evt.target is IMGUIContainer)
					return;

				if (evt.keyCode == KeyCode.Escape && _typePickerOverlay != null)
				{
					HideTypePickerOverlay();
					evt.StopImmediatePropagation();
					return;
				}

				if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
				{
					if (DeleteSelectedElements())
						evt.StopImmediatePropagation();
					return;
				}

				var actionKey = (evt.modifiers & EventModifiers.Command) != 0 ||
					(evt.modifiers & EventModifiers.Control) != 0;
				if (!actionKey)
					return;

				switch (evt.keyCode)
				{
					case KeyCode.C:
						if (CopySelectedNode())
							evt.StopImmediatePropagation();
						break;
					case KeyCode.D:
						if (DuplicateSelectedNode())
							evt.StopImmediatePropagation();
						break;
					case KeyCode.V:
						if (_clipboard != null)
						{
							PasteNode(_mousePosition);
							evt.StopImmediatePropagation();
						}

						break;
				}
			}

			private void OnKeyUp(KeyUpEvent evt)
			{
				if (evt.keyCode != KeyCode.Space)
					return;

				StopSpacePan();
				evt.StopImmediatePropagation();
			}

			private void SetSpacePanActive(bool active)
			{
				if (_spacePanActive == active)
					return;

				_spacePanActive = active;
				if (_spacePanCursorOverlay == null)
					return;

				_spacePanCursorOverlay.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
				if (active)
					_spacePanCursorOverlay.BringToFront();

				_spacePanCursorOverlay.MarkDirtyRepaint();
			}

			private void StopSpacePan()
			{
				SetSpacePanActive(false);
				_isSpacePanning = false;
				if (this.HasMouseCapture())
					this.ReleaseMouse();
			}

			private bool IsEventTargetInsideTypePicker(EventBase evt)
			{
				if (_typePickerOverlay == null || evt.target is not VisualElement target)
					return false;

				for (var current = target; current != null; current = current.parent)
				{
					if (current == _typePickerOverlay)
						return true;
				}

				return false;
			}

			private void CreateNodeView(EvaluatorGraphNodeModel model, Vector2 position)
			{
				if (_modelToView.ContainsKey(model))
					return;

				var view = new EvaluatorNodeView(model, model == _graph?.Root, _edgeConnectorListener, PopulatePortMenu, _onChanged);
				var width = GetNodeWidth(model);
				view.SetPosition(new Rect(position, new Vector2(width, model.Height)));
				view.style.width    = width;
				view.style.minWidth = width;
				view.AddManipulator(new ContextualMenuManipulator(evt => PopulateNodeMenu(model, evt.menu)));
				_modelToView[model] = view;
				AddElement(view);

				foreach (var portModel in model.Ports)
				{
					var output = view.AddOutput(portModel);
					_portToView[portModel] = output;
				}

				view.RefreshNodeState();
			}

			private static float GetNodeWidth(EvaluatorGraphNodeModel model)
				=> model?.Kind == EvaluatorGraphNodeKind.Root ? ROOT_NODE_WIDTH : NODE_WIDTH;

			private void PopulatePortMenu(Port output, EvaluatorGraphPortModel portModel, DropdownMenu menu)
			{
				menu.AppendAction("Clear", _ =>
				{
					RemoveOutputEdges(output);
					_onChanged?.Invoke();
				});
				menu.AppendAction("Create...", _ => OpenTypePicker(portModel.TargetType, evaluator => CreateConnectedNode(output, portModel, evaluator)));

				if (_clipboard != null && _clipboard.IsAssignableTo(portModel.TargetType))
					menu.AppendAction("Paste Connected", _ => PasteConnected(output, portModel));
			}

			private void PopulateNodeMenu(EvaluatorGraphNodeModel model, DropdownMenu menu)
			{
				if (model == _graph?.Root)
				{
					menu.AppendAction("Copy", _ => { }, DropdownMenuAction.Status.Disabled);
					menu.AppendAction("Duplicate", _ => { }, DropdownMenuAction.Status.Disabled);
					menu.AppendAction("Delete", _ => { }, DropdownMenuAction.Status.Disabled);
					return;
				}

				menu.AppendAction("Copy", _ => CopyNode(model));
				menu.AppendAction("Duplicate", _ => DuplicateNode(model));
				menu.AppendAction("Delete", _ => DeleteNode(model));
			}

			private void OpenTypePicker(Type targetType, Action<IEvaluator> onSelected)
				=> ShowTypePickerOverlay(targetType, onSelected, _mousePosition);

			private void ShowTypePickerOverlay(Type targetType, Action<IEvaluator> onSelected, Vector2 graphPosition)
			{
				HideTypePickerOverlay();

				targetType ??= typeof(IEvaluator);
				var state = new TypePickerState();
				var localPosition = this.WorldToLocal(contentViewContainer.LocalToWorld(graphPosition));

				var overlay = new VisualElement
				{
					focusable = true,
					style =
					{
						position          = Position.Absolute,
						left              = Mathf.Max(8f, localPosition.x),
						top               = Mathf.Max(8f, localPosition.y),
						width             = 340,
						paddingLeft       = 6,
						paddingRight      = 6,
						paddingTop        = 6,
						paddingBottom     = 6,
						backgroundColor   = new Color(0.16f, 0.16f, 0.16f, 0.96f),
						borderTopWidth    = 1,
						borderRightWidth  = 1,
						borderBottomWidth = 1,
						borderLeftWidth   = 1,
						borderTopColor    = new Color(0.35f, 0.35f, 0.35f),
						borderRightColor  = new Color(0.35f, 0.35f, 0.35f),
						borderBottomColor = new Color(0.35f, 0.35f, 0.35f),
						borderLeftColor   = new Color(0.35f, 0.35f, 0.35f)
					}
				};

				overlay.Add(new IMGUIContainer(() =>
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Select Evaluator", EditorStyles.boldLabel);
					GUILayout.FlexibleSpace();
					if (DrawCloseIconButton())
						HideTypePickerOverlay();
					EditorGUILayout.EndHorizontal();

					GUILayout.Space(8);
					EditorGUI.BeginChangeCheck();
					var picked = SirenixEditorFields.PolymorphicObjectField(
						GUIContent.none,
						state.Value,
						targetType,
						false);

					if (!EditorGUI.EndChangeCheck())
						return;

					state.Value = picked;
					if (picked is not IEvaluator evaluator)
						return;

					if (!targetType.IsAssignableFrom(evaluator.GetType()))
						return;

					EditorApplication.delayCall += () =>
					{
						HideTypePickerOverlay();
						onSelected?.Invoke(evaluator);
					};
				}));

				_typePickerOverlay = overlay;
				Add(_typePickerOverlay);
				_typePickerOverlay.BringToFront();
				_typePickerOverlay.Focus();
			}

			private static bool DrawCloseIconButton()
			{
				var rect = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f), GUILayout.Height(14f));
				var hovered = rect.Contains(Event.current.mousePosition);
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

				var clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
				var iconRect = new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f);
				var color = hovered ? Color.white : new Color(0.68f, 0.68f, 0.68f);
				SdfIcons.DrawIcon(iconRect, SdfIconType.X, color);
				return clicked;
			}

			private void HideTypePickerOverlay()
			{
				_typePickerOverlay?.RemoveFromHierarchy();
				_typePickerOverlay = null;
			}

			private void CreateStandaloneNode(IEvaluator evaluator, Vector2 position)
			{
				var model = CreateEvaluatorNodeModel(evaluator, "node", position);
				if (model == null)
					return;

				_onChanged?.Invoke();
			}

			private void CreateConnectedNode(Port output, EvaluatorGraphPortModel portModel, IEvaluator evaluator)
			{
				var sourceNode = output.GetFirstAncestorOfType<Node>();
				var sourcePosition = sourceNode?.GetPosition().position ?? Vector2.zero;
				var model = CreateEvaluatorNodeModel(evaluator, GetConnectionLabel(portModel), sourcePosition + new Vector2(LEVEL_WIDTH, 0f));
				if (model == null)
					return;

				Connect(output, model, true);
				_onChanged?.Invoke();
			}

			private void PasteConnected(Port output, EvaluatorGraphPortModel portModel)
			{
				if (_clipboard == null)
					return;

				var sourceNode = output.GetFirstAncestorOfType<Node>();
				var sourcePosition = sourceNode?.GetPosition().position ?? Vector2.zero;
				var model = CreateNodeFromClipboard(_clipboard, GetConnectionLabel(portModel), sourcePosition + new Vector2(LEVEL_WIDTH, 0f));
				if (model == null)
					return;

				Connect(output, model, true);
				_onChanged?.Invoke();
			}

			private void PasteNode(Vector2 position)
			{
				if (_clipboard == null || CreateNodeFromClipboard(_clipboard, "node", position) == null)
					return;

				_onChanged?.Invoke();
			}

			private void DuplicateNode(EvaluatorGraphNodeModel model)
			{
				var viewPosition = _modelToView.TryGetValue(model, out var view)
					? view.GetPosition().position + new Vector2(32f, 32f)
					: _mousePosition + new Vector2(32f, 32f);

				var clipboard = EvaluatorClipboard.From(model);
				if (CreateNodeFromClipboard(clipboard, model.SourceLabel, viewPosition) == null)
					return;

				_onChanged?.Invoke();
			}

			private void CopyNode(EvaluatorGraphNodeModel model)
				=> _clipboard = EvaluatorClipboard.From(model);

			private bool CopySelectedNode()
			{
				var model = GetSelectedNodeModel();
				if (model == null || model == _graph?.Root)
					return false;

				CopyNode(model);
				return true;
			}

			private bool DuplicateSelectedNode()
			{
				var model = GetSelectedNodeModel();
				if (model == null || model == _graph?.Root)
					return false;

				DuplicateNode(model);
				return true;
			}

			private bool DeleteSelectedElements()
			{
				var selectedEdges = selection
					.OfType<Edge>()
					.Cast<GraphElement>()
					.ToList();

				var models = selection
					.OfType<EvaluatorNodeView>()
					.Select(view => view.userData as EvaluatorGraphNodeModel)
					.Where(model => model != null && model != _graph?.Root)
					.ToArray();

				if (selectedEdges.Count == 0 && models.Length == 0)
					return false;

				if (selectedEdges.Count > 0)
					DeleteElements(selectedEdges);

				foreach (var model in models)
					DeleteNode(model);

				if (selectedEdges.Count > 0)
					_onChanged?.Invoke();

				return true;
			}

			private EvaluatorGraphNodeModel GetSelectedNodeModel()
				=> selection
					.OfType<EvaluatorNodeView>()
					.Select(view => view.userData as EvaluatorGraphNodeModel)
					.FirstOrDefault(model => model != null);

			private void DeleteNode(EvaluatorGraphNodeModel model)
			{
				if (model == _graph?.Root || !_modelToView.TryGetValue(model, out var view))
					return;

				var elements = edges.ToList()
					.Where(edge =>
						edge.input?.GetFirstAncestorOfType<Node>() == view ||
						edge.output?.GetFirstAncestorOfType<Node>() == view)
					.Cast<GraphElement>()
					.ToList();
				elements.Add(view);
				DeleteElements(elements);

				foreach (var port in model.Ports)
					_portToView.Remove(port);

				RemoveNodeModel(model);

				_onChanged?.Invoke();
			}

			private void RemoveNodeModel(EvaluatorGraphNodeModel model)
			{
				if (model == null || model == _graph?.Root)
					return;

				foreach (var port in model.Ports)
					_portToView.Remove(port);

				_modelToView.Remove(model);

				if (_graph != null)
				{
					foreach (var node in _graph.Nodes)
						node.Edges.RemoveAll(x => x.Child == model || x.Port.Owner == model);
				}

				if (_graph?.Nodes is IList<EvaluatorGraphNodeModel> nodes)
					nodes.Remove(model);
			}

			private EvaluatorGraphNodeModel CreateNodeFromClipboard(EvaluatorClipboard clipboard, string label, Vector2 position)
			{
				if (clipboard.TryCreateEvaluator(out var evaluator))
					return CreateEvaluatorNodeModel(evaluator, label, position);

				if (clipboard.TryCreateConstant(out var constant))
				{
					var model = _builder.AddConstant(label, constant.Value, constant.ValueType, constant.TargetType);
					CreateNodeView(model, position);
					return model;
				}

				return null;
			}

			private static string GetConnectionLabel(EvaluatorGraphPortModel portModel)
			{
				if (portModel?.IsCollectionAppend == true && portModel.Field != null)
					return FormatCollectionPortLabel(portModel.CollectionIndex);

				return portModel?.Label ?? "node";
			}

			private EvaluatorGraphNodeModel CreateEvaluatorNodeModel(IEvaluator evaluator, string label, Vector2 position)
			{
				var oldNodeCount = _graph?.Nodes.Count ?? 0;
				var model = _builder.AddEvaluator(evaluator, label);

				var createdModels = _graph?.Nodes.Skip(oldNodeCount).ToArray() ?? new[] {model};
				for (var i = 0; i < createdModels.Length; i++)
					CreateNodeView(createdModels[i], position + new Vector2(i == 0 ? 0f : LEVEL_WIDTH, i * 110f));

				foreach (var createdModel in createdModels)
					foreach (var edgeModel in createdModel.Edges)
						Connect(edgeModel.Port, edgeModel.Child, false);

				return model;
			}

			private void Connect(EvaluatorGraphPortModel portModel, EvaluatorGraphNodeModel childModel, bool exclusive)
			{
				if (!_portToView.TryGetValue(portModel, out var output))
					return;

				Connect(output, childModel, exclusive);
			}

			private void Connect(Port output, EvaluatorGraphNodeModel childModel, bool exclusive)
			{
				if (!_modelToView.TryGetValue(childModel, out var childView))
					return;

				if (childView.Input == null)
					return;

				if (exclusive)
					RemoveOutputEdges(output);

				var edge = output.ConnectTo(childView.Input);
				AddEdge(edge);

				if (output.userData is EvaluatorGraphPortModel portModel)
					PromoteCollectionAppendPort(portModel);
			}

			private void ConnectDroppedEdge(Edge edge)
			{
				if (edge?.output?.userData is not EvaluatorGraphPortModel portModel ||
					edge.input?.userData is not EvaluatorGraphNodeModel childModel ||
					!CanConnect(portModel, childModel))
				{
					return;
				}

				RemoveOutputEdges(edge.output, edge, edge.input);
				AddEdge(edge);
				PromoteCollectionAppendPort(portModel);
				_onChanged?.Invoke();
			}

			private void AddEdge(Edge edge)
			{
				if (edge?.output == null || edge.input == null)
					return;

				ConnectPort(edge.output, edge);
				ConnectPort(edge.input, edge);
				AddElement(edge);
				RefreshPort(edge.output);
				RefreshPort(edge.input);
			}

			private static void ConnectPort(Port port, Edge edge)
			{
				if (port == null || edge == null || port.connections.Contains(edge))
					return;

				port.Connect(edge);
			}

			private static void DisconnectPort(Port port, Edge edge)
			{
				if (port == null || edge == null || !port.connections.Contains(edge))
					return;

				port.Disconnect(edge);
			}

			private static void RefreshPort(Port port)
			{
				port?.MarkDirtyRepaint();
				port?.contentContainer?.MarkDirtyRepaint();
			}

			private void CreateNodeFromDroppedEdge(Edge edge, Vector2 worldPosition)
			{
				if (edge?.output?.userData is not EvaluatorGraphPortModel portModel)
					return;

				var output = edge.output;
				var graphPosition = this.ChangeCoordinatesTo(contentViewContainer, worldPosition);
				ShowTypePickerOverlay(
					portModel.TargetType,
					evaluator => CreateConnectedNodeAt(output, portModel, evaluator, graphPosition),
					graphPosition);
			}

			private void CreateConnectedNodeAt(Port output, EvaluatorGraphPortModel portModel, IEvaluator evaluator, Vector2 position)
			{
				var model = CreateEvaluatorNodeModel(evaluator, GetConnectionLabel(portModel), position);
				if (model == null)
					return;

				Connect(output, model, true);
				_onChanged?.Invoke();
			}

			private void PromoteCollectionAppendPort(EvaluatorGraphPortModel portModel)
			{
				if (portModel == null ||
					portModel.Kind != EvaluatorGraphPortKind.EvaluatorCollectionElement ||
					!portModel.IsCollectionAppend ||
					portModel.Field == null)
				{
					return;
				}

				portModel.IsCollectionAppend = false;
				portModel.Label              = FormatCollectionPortLabel(portModel.CollectionIndex);

				if (_portToView.TryGetValue(portModel, out var output))
					output.portName = portModel.Label;

				var nextPort = portModel.Owner.AddPort(
					"[+]",
					portModel.Field,
					portModel.TargetType,
					EvaluatorGraphPortKind.EvaluatorCollectionElement,
					portModel.CollectionIndex + 1,
					true);

				if (!_modelToView.TryGetValue(portModel.Owner, out var ownerView))
					return;

				_portToView[nextPort] = ownerView.AddOutput(nextPort);
				var position = ownerView.GetPosition();
				position.height = portModel.Owner.Height;
				ownerView.SetPosition(position);
				ownerView.RefreshNodeState();
			}

			private void ScheduleNormalizeCollectionPorts(IEnumerable<EvaluatorGraphPortModel> portModels)
			{
				var groups = portModels
					.Where(IsCollectionPort)
					.Select(x => new CollectionPortGroup(x.Owner, x.Field))
					.Distinct()
					.ToArray();

				if (groups.Length == 0)
					return;

				schedule.Execute(() =>
				{
					foreach (var group in groups)
						NormalizeCollectionPorts(group.Owner, group.Field);
				}).StartingIn(0);
			}

			private void NormalizeCollectionPorts(EvaluatorGraphNodeModel owner, FieldInfo field)
			{
				if (owner == null || field == null || !_modelToView.TryGetValue(owner, out var ownerView))
					return;

				var collectionPorts = owner.Ports
					.Where(x => x.Kind == EvaluatorGraphPortKind.EvaluatorCollectionElement && x.Field == field)
					.OrderBy(x => x.CollectionIndex)
					.ToList();
				if (collectionPorts.Count == 0)
					return;

				var connectedPorts = collectionPorts
					.Where(x => !x.IsCollectionAppend && IsConnected(x))
					.OrderBy(x => x.CollectionIndex)
					.ToList();
				var connectedSet = new HashSet<EvaluatorGraphPortModel>(connectedPorts);
				var appendPort = collectionPorts.FirstOrDefault(x => x.IsCollectionAppend);

				foreach (var port in collectionPorts)
				{
					if (port == appendPort || connectedSet.Contains(port))
						continue;

					RemovePortView(port);
					owner.Ports.Remove(port);
				}

				for (var i = 0; i < connectedPorts.Count; i++)
					SetCollectionPortLabel(connectedPorts[i], i, false);

				if (appendPort == null)
				{
					var targetType = collectionPorts.FirstOrDefault()?.TargetType ?? GetEnumerableElementType(field.FieldType);
					appendPort = owner.AddPort(
						"[+]",
						field,
						targetType,
						EvaluatorGraphPortKind.EvaluatorCollectionElement,
						connectedPorts.Count,
						true);
					_portToView[appendPort] = ownerView.AddOutput(appendPort);
				}
				else
				{
					SetCollectionPortLabel(appendPort, connectedPorts.Count, true);
				}

				var position = ownerView.GetPosition();
				position.height = owner.Height;
				ownerView.SetPosition(position);
				ownerView.RefreshNodeState();
			}

			private bool IsConnected(EvaluatorGraphPortModel portModel)
			{
				return _portToView.TryGetValue(portModel, out var output) &&
					edges.ToList().Any(edge => edge.output == output);
			}

			private void RemovePortView(EvaluatorGraphPortModel portModel)
			{
				if (!_portToView.TryGetValue(portModel, out var port))
					return;

				port.RemoveFromHierarchy();
				_portToView.Remove(portModel);
			}

			private void SetCollectionPortLabel(EvaluatorGraphPortModel portModel, int index, bool isAppend)
			{
				portModel.CollectionIndex    = index;
				portModel.IsCollectionAppend = isAppend;
				portModel.Label              = isAppend ? "[+]" : FormatCollectionPortLabel(index);

				if (_portToView.TryGetValue(portModel, out var port))
					port.portName = portModel.Label;
			}

			private static bool IsCollectionPort(EvaluatorGraphPortModel portModel)
				=> portModel?.Kind == EvaluatorGraphPortKind.EvaluatorCollectionElement && portModel.Field != null;

			private void RemoveOutputEdges(Port output, Edge except = null, Port exceptInput = null)
			{
				if (output == null)
					return;

				var existingEdges = edges.ToList()
					.Where(x => x.output == output && x != except && (exceptInput == null || x.input != exceptInput))
					.ToList();

				if (existingEdges.Count > 0)
				{
					foreach (var edge in existingEdges)
					{
						DisconnectPort(edge.output, edge);
						DisconnectPort(edge.input, edge);
						RefreshPort(edge.output);
						RefreshPort(edge.input);
					}

					DeleteElements(existingEdges.Cast<GraphElement>().ToList());
				}
			}

			private Dictionary<EvaluatorGraphPortModel, EvaluatorGraphNodeModel> CollectConnections()
			{
				var result = new Dictionary<EvaluatorGraphPortModel, EvaluatorGraphNodeModel>();
				foreach (var edge in edges.ToList())
				{
					if (edge.output?.userData is not EvaluatorGraphPortModel portModel ||
						edge.input?.userData is not EvaluatorGraphNodeModel childModel)
						continue;

					result[portModel] = childModel;
				}

				return result;
			}

			private IEvaluator BakeNode(
				EvaluatorGraphNodeModel model,
				IReadOnlyDictionary<EvaluatorGraphPortModel, EvaluatorGraphNodeModel> connections,
				HashSet<EvaluatorGraphNodeModel> activePath)
			{
				if (model.Kind == EvaluatorGraphNodeKind.ConstantValue)
					return CreateConstantEvaluator(model.ConstantTargetType, model.ConstantValue);

				var evaluator = model.Evaluator;
				if (evaluator == null || !activePath.Add(model))
					return evaluator;

				var bakedCollectionFields = new HashSet<FieldInfo>();
				foreach (var port in model.Ports)
				{
					connections.TryGetValue(port, out var childModel);

					switch (port.Kind)
					{
						case EvaluatorGraphPortKind.EvaluatorField:
							port.Field.SetValue(evaluator, BakeChildEvaluator(childModel, port.TargetType, connections, activePath));
							break;

						case EvaluatorGraphPortKind.EvaluatedValueField:
							SetEvaluatedValueField(evaluator, port, childModel, connections, activePath);
							break;

						case EvaluatorGraphPortKind.EvaluatorCollectionElement:
							if (bakedCollectionFields.Add(port.Field))
								SetCollectionField(evaluator, model, port.Field, connections, activePath);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				activePath.Remove(model);
				return evaluator;
			}

			private IEvaluator BakeChildEvaluator(
				EvaluatorGraphNodeModel childModel,
				Type targetType,
				IReadOnlyDictionary<EvaluatorGraphPortModel, EvaluatorGraphNodeModel> connections,
				HashSet<EvaluatorGraphNodeModel> activePath)
			{
				if (childModel == null)
					return null;

				if (childModel.Kind == EvaluatorGraphNodeKind.ConstantValue)
					return CreateConstantEvaluator(targetType, childModel.ConstantValue);

				return BakeNode(childModel, connections, activePath);
			}

			private void SetEvaluatedValueField(
				IEvaluator evaluator,
				EvaluatorGraphPortModel port,
				EvaluatorGraphNodeModel childModel,
				IReadOnlyDictionary<EvaluatorGraphPortModel, EvaluatorGraphNodeModel> connections,
				HashSet<EvaluatorGraphNodeModel> activePath)
			{
				var boxedValue = port.Field.GetValue(evaluator);
				var evaluatorField = GetFieldInHierarchy(port.Field.FieldType, "evaluator");
				var valueField = GetFieldInHierarchy(port.Field.FieldType, "value");

				if (childModel == null)
				{
					evaluatorField?.SetValue(boxedValue, null);
				}
				else if (childModel.Kind == EvaluatorGraphNodeKind.ConstantValue)
				{
					valueField?.SetValue(boxedValue, childModel.ConstantValue);
					evaluatorField?.SetValue(boxedValue, null);
				}
				else
				{
					evaluatorField?.SetValue(boxedValue, BakeNode(childModel, connections, activePath));
				}

				port.Field.SetValue(evaluator, boxedValue);
			}

			private void SetCollectionField(
				IEvaluator evaluator,
				EvaluatorGraphNodeModel model,
				FieldInfo field,
				IReadOnlyDictionary<EvaluatorGraphPortModel, EvaluatorGraphNodeModel> connections,
				HashSet<EvaluatorGraphNodeModel> activePath)
			{
				if (field == null)
					return;

				var ports = model.Ports
					.Where(x => x.Kind == EvaluatorGraphPortKind.EvaluatorCollectionElement &&
						x.Field == field &&
						!x.IsCollectionAppend)
					.OrderBy(x => x.CollectionIndex);
				var values = new List<IEvaluator>();
				foreach (var port in ports)
				{
					if (!connections.TryGetValue(port, out var childModel))
						continue;

					var value = BakeChildEvaluator(childModel, port.TargetType, connections, activePath);
					if (value != null)
						values.Add(value);
				}

				var collection = field.GetValue(evaluator);
				if (collection is Array array)
				{
					var elementType = array.GetType().GetElementType();
					var resized = Array.CreateInstance(elementType, values.Count);
					for (var i = 0; i < values.Count; i++)
						resized.SetValue(values[i], i);
					field.SetValue(evaluator, resized);
					return;
				}

				if (collection is IList list)
				{
					if (list.IsFixedSize || list.IsReadOnly)
						return;

					list.Clear();
					foreach (var value in values)
						list.Add(value);
					return;
				}

				var elementType2 = GetEnumerableElementType(field.FieldType);
				if (elementType2 == null)
					return;

				if (field.FieldType.IsArray)
				{
					var array2 = Array.CreateInstance(elementType2, values.Count);
					for (var i = 0; i < values.Count; i++)
						array2.SetValue(values[i], i);
					field.SetValue(evaluator, array2);
					return;
				}

				if (TryCreateList(field.FieldType, elementType2, out var createdList))
				{
					foreach (var value in values)
						createdList.Add(value);
					field.SetValue(evaluator, createdList);
				}
			}

			private static IEvaluator CreateConstantEvaluator(Type targetType, object value)
			{
				if (!TryGetEvaluatorArgumentTypes(targetType, out var contextType, out var valueType))
					return null;

				var constantType = typeof(ConstantEvaluator<,>).MakeGenericType(contextType, valueType);
				var constant = (IEvaluator) Activator.CreateInstance(constantType);
				GetFieldInHierarchy(constantType, "value")?.SetValue(constant, value);
				return constant;
			}

			private static bool TryCreateList(Type collectionType, Type elementType, out IList list)
			{
				list = null;

				var listType = collectionType.IsInterface || collectionType.IsAbstract
					? typeof(List<>).MakeGenericType(elementType)
					: collectionType;

				if (!typeof(IList).IsAssignableFrom(listType) || !collectionType.IsAssignableFrom(listType))
					return false;

				try
				{
					list = Activator.CreateInstance(listType) as IList;
				}
				catch
				{
					return false;
				}

				return list != null;
			}

			private sealed class TypePickerState
			{
				public object Value;
			}

			private readonly struct CollectionPortGroup : IEquatable<CollectionPortGroup>
			{
				public readonly EvaluatorGraphNodeModel Owner;
				public readonly FieldInfo Field;

				public CollectionPortGroup(EvaluatorGraphNodeModel owner, FieldInfo field)
				{
					Owner = owner;
					Field = field;
				}

				public bool Equals(CollectionPortGroup other)
					=> ReferenceEquals(Owner, other.Owner) && Equals(Field, other.Field);

				public override bool Equals(object obj)
					=> obj is CollectionPortGroup other && Equals(other);

				public override int GetHashCode()
				{
					unchecked
					{
						return ((Owner != null ? Owner.GetHashCode() : 0) * 397) ^
							(Field != null ? Field.GetHashCode() : 0);
					}
				}
			}

			private sealed class EvaluatorEdgeConnectorListener : IEdgeConnectorListener
			{
				private readonly EvaluatorGraphView _graphView;

				public EvaluatorEdgeConnectorListener(EvaluatorGraphView graphView)
				{
					_graphView = graphView;
				}

				public void OnDrop(GraphView graphView, Edge edge)
					=> _graphView.ConnectDroppedEdge(edge);

				public void OnDropOutsidePort(Edge edge, Vector2 position)
					=> _graphView.CreateNodeFromDroppedEdge(edge, position);
			}

			private sealed class EvaluatorClipboard
			{
				private readonly IEvaluator _evaluator;
				private readonly ConstantData _constant;

				private EvaluatorClipboard(IEvaluator evaluator, ConstantData constant)
				{
					_evaluator = evaluator;
					_constant  = constant;
				}

				public static EvaluatorClipboard From(EvaluatorGraphNodeModel model)
				{
					if (model == null)
						return null;

					if (model.Kind == EvaluatorGraphNodeKind.ConstantValue)
					{
						return new EvaluatorClipboard(null, new ConstantData
						{
							Value      = ClonePlainValue(model.ConstantValue),
							ValueType  = model.ConstantValueType,
							TargetType = model.ConstantTargetType
						});
					}

					return new EvaluatorClipboard(CloneEvaluator(model.Evaluator), null);
				}

				public bool IsAssignableTo(Type targetType)
				{
					if (targetType == null)
						return false;

					if (_evaluator != null)
						return targetType.IsAssignableFrom(_evaluator.GetType());

					return _constant?.TargetType != null && targetType.IsAssignableFrom(_constant.TargetType);
				}

				public bool TryCreateEvaluator(out IEvaluator evaluator)
				{
					evaluator = CloneEvaluator(_evaluator);
					return evaluator != null;
				}

				public bool TryCreateConstant(out ConstantData constant)
				{
					if (_constant == null)
					{
						constant = null;
						return false;
					}

					constant = new ConstantData
					{
						Value      = ClonePlainValue(_constant.Value),
						ValueType  = _constant.ValueType,
						TargetType = _constant.TargetType
					};
					return true;
				}

				private static IEvaluator CloneEvaluator(IEvaluator source)
				{
					if (source == null)
						return null;

					var clone = Activator.CreateInstance(source.GetType()) as IEvaluator;
					if (clone == null)
						return null;

					try
					{
						EditorUtility.CopySerializedManagedFieldsOnly(source, clone);
					}
					catch
					{
						return null;
					}

					return clone;
				}

				private static object ClonePlainValue(object source)
				{
					if (source == null)
						return null;

					var type = source.GetType();
					if (type.IsValueType || source is string || typeof(UnityObject).IsAssignableFrom(type))
						return source;

					try
					{
						var clone = Activator.CreateInstance(type);
						EditorUtility.CopySerializedManagedFieldsOnly(source, clone);
						return clone;
					}
					catch
					{
						return source;
					}
				}

				public sealed class ConstantData
				{
					public object Value;
					public Type ValueType;
					public Type TargetType;
				}
			}

			private static void Layout(EvaluatorGraphNodeModel root)
			{
				var y = 20f;
				LayoutNode(root, 0, ref y, new HashSet<EvaluatorGraphNodeModel>());
			}

			private static float LayoutNode(
				EvaluatorGraphNodeModel model,
				int depth,
				ref float y,
				HashSet<EvaluatorGraphNodeModel> activePath)
			{
				if (!activePath.Add(model))
				{
					model.Position =  new Vector2(depth * LEVEL_WIDTH + 20f, y);
					y              += model.Height + NODE_VERTICAL_SPACE;
					return model.Position.y;
				}

				var treeChildren = model.Edges
					.Select(x => x.Child)
					.Distinct()
					.Where(x => !activePath.Contains(x))
					.ToArray();

				if (treeChildren.Length == 0)
				{
					model.Position =  new Vector2(depth * LEVEL_WIDTH + 20f, y);
					y              += model.Height + NODE_VERTICAL_SPACE;
					activePath.Remove(model);
					return model.Position.y;
				}

				var firstChildY = 0f;
				var lastChildY = 0f;
				for (var i = 0; i < treeChildren.Length; i++)
				{
					var childY = LayoutNode(treeChildren[i], depth + 1, ref y, activePath);
					if (i == 0)
						firstChildY = childY;
					lastChildY = childY;
				}

				model.Position = new Vector2(depth * LEVEL_WIDTH + 20f, (firstChildY + lastChildY) * 0.5f);
				activePath.Remove(model);
				return model.Position.y;
			}
		}

		private sealed class EvaluatorNodeView : Node
		{
			private static Color NODE_COLOR = new Color(0.19f, 0.19f, 0.19f);
			private static Color ROOT_NODE_COLOR = new Color(0.5f, 0.5f, 0.5f);

			private readonly SdfIconType _icon;
			private readonly Color _iconColor;

			private readonly IEdgeConnectorListener _edgeConnectorListener;
			private readonly Action<Port, EvaluatorGraphPortModel, DropdownMenu> _populatePortMenu;
			private readonly Action _onChanged;
			private PropertyTree _tree;
			private object _treeTarget;
			private FieldInfo _constantValueField;

			public Port Input { get; }

			public EvaluatorNodeView(
				EvaluatorGraphNodeModel model,
				bool isRoot,
				IEdgeConnectorListener edgeConnectorListener,
				Action<Port, EvaluatorGraphPortModel, DropdownMenu> populatePortMenu,
				Action onChanged)
			{
				_edgeConnectorListener = edgeConnectorListener;
				_populatePortMenu      = populatePortMenu;
				_onChanged             = onChanged;
				_icon                  = model.Icon;
				_iconColor             = model.IconColor;
				title                  = model.Title;
				userData               = model;
				capabilities           = Capabilities.Movable | Capabilities.Selectable;
				if (!isRoot)
					capabilities |= Capabilities.Deletable;

				titleContainer.style.borderBottomWidth = 2;
				titleContainer.style.borderBottomColor = model.TitleColor;

				titleContainer.style.backgroundColor = isRoot ? GetRootTitleColor(model.TitleColor) : NODE_COLOR;
				mainContainer.style.backgroundColor  = isRoot ? ROOT_NODE_COLOR : NODE_COLOR;

				if (!isRoot)
				{
					Input           = EvaluatorPort.Create(Orientation.Horizontal, UnityDirection.Input, Port.Capacity.Multi, typeof(object), _edgeConnectorListener);
					Input.userData  = model;
					Input.portName  = string.Empty;
					Input.portColor = model.TitleColor;
					inputContainer.Add(Input);
				}

				if (!model.Subtitle.IsNullOrEmpty())
				{
					var subtitle = MakeLabel(model.Subtitle, true);
					subtitle.style.marginBottom = 4;
					subtitle.style.marginTop    = 4;
					mainContainer.Add(subtitle);
				}

				foreach (var row in model.Rows)
					mainContainer.Add(MakeLabel(row, false));

				if (model.Rows.Count == 0 && model.Ports.Count == 0 && model.Kind != EvaluatorGraphNodeKind.Evaluator &&
					model.Kind != EvaluatorGraphNodeKind.ConstantValue)
					mainContainer.Add(MakeLabel("empty", true));

				SetupInlineEditor(model);
				RefreshNodeState();
			}

			public void RefreshNodeState()
			{
				expanded     =  true;
				capabilities &= ~Capabilities.Collapsible;
				RefreshExpandedState();
				RefreshPorts();
				RefreshTitleIcon();
			}

			private static Color GetRootTitleColor(Color typeColor)
				=> Color.Lerp(NODE_COLOR, typeColor, 0.18f);

			public Port AddOutput(EvaluatorGraphPortModel portModel)
			{
				var output = EvaluatorPort.Create(Orientation.Horizontal, UnityDirection.Output, Port.Capacity.Single, portModel.TargetType, _edgeConnectorListener);
				output.userData  = portModel;
				output.portName  = portModel.Label;
				output.portColor = GetPortColor(portModel.TargetType);
				output.tooltip   = MakePortTooltip(portModel);
				output.AddManipulator(new ContextualMenuManipulator(evt => _populatePortMenu(output, portModel, evt.menu)));
				outputContainer.Add(output);
				return output;
			}

			private static Color GetPortColor(Type targetType)
			{
				return IsConditionTargetType(targetType)
					? EvaluatorTypeRegistryConstants.CONDITION_COLOR
					: EvaluatorTypeRegistryConstants.EVALUATOR_COLOR;
			}

			private static bool IsConditionTargetType(Type targetType)
			{
				return targetType != null &&
					(typeof(ICondition).IsAssignableFrom(targetType) ||
						TryGetConditionContextType(targetType, out _));
			}

			private static string MakePortTooltip(EvaluatorGraphPortModel portModel)
			{
				var fieldName = portModel.Field == null ? null : $"{portModel.Field.Name}\n";
				return $"{fieldName}Right click: create/clear\n{portModel.TargetType.GetNiceName()}";
			}

			private void RefreshTitleIcon()
			{
				titleButtonContainer.Clear();
				titleButtonContainer.style.display        = _icon == SdfIconType.None ? DisplayStyle.None : DisplayStyle.Flex;
				titleButtonContainer.style.alignSelf      = Align.Stretch;
				titleButtonContainer.style.alignItems     = Align.Center;
				titleButtonContainer.style.justifyContent = Justify.Center;
				titleButtonContainer.style.minWidth       = 24;

				if (_icon != SdfIconType.None)
					titleButtonContainer.Add(MakeIcon(_icon, _iconColor));
			}

			private static IMGUIContainer MakeIcon(SdfIconType icon, Color color)
			{
				var container = new IMGUIContainer(() =>
				{
					var rect = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
					rect.y -= 1;
					rect.x -= 2;
					SdfIcons.DrawIcon(rect, icon, color);
				});
				container.style.width       = 12;
				container.style.height      = 12;
				container.style.alignSelf   = Align.Center;
				container.style.marginLeft  = 4;
				container.style.marginRight = 4;
				return container;
			}

			private void SetupInlineEditor(EvaluatorGraphNodeModel model)
			{
				_treeTarget = GetInlineEditorTarget(model);
				if (_treeTarget == null)
					return;

				BeginInlineNodeRendering();
				try
				{
					_tree = PropertyTree.Create(_treeTarget);
				}
				finally
				{
					EndInlineNodeRendering();
				}

				IMGUIContainer container = null;
				container = new IMGUIContainer(() =>
				{
					BeginInlineNodeRendering();
					try
					{
						_tree.UpdateTree();
						EditorGUI.BeginChangeCheck();

						DrawInlineProperties(_tree.RootProperty, model.Kind == EvaluatorGraphNodeKind.ConstantValue);

						var changed = EditorGUI.EndChangeCheck();
						if (model.Kind == EvaluatorGraphNodeKind.ConstantValue && _constantValueField != null)
						{
							_tree.ApplyChanges();
							var constantValue = _constantValueField.GetValue(_treeTarget);
							if (!ConstantValuesEqual(model.ConstantValue, constantValue))
							{
								model.ConstantValue = constantValue;
								changed             = true;
							}
						}
						else if (changed)
						{
							_tree.ApplyChanges();
						}

						if (changed)
						{
							ScheduleInlineEditorResize(container, model);
							_onChanged?.Invoke();
						}
					}
					finally
					{
						EndInlineNodeRendering();
					}
				});

				extensionContainer.Add(container);
				ScheduleInlineEditorResize(container, model);
				RegisterCallback<DetachFromPanelEvent>(_ =>
				{
					_tree?.Dispose();
					_tree = null;
				});
			}

			private void ScheduleInlineEditorResize(IMGUIContainer container, EvaluatorGraphNodeModel model)
			{
				if (container == null || model == null)
					return;

				schedule.Execute(() =>
				{
					var inlineHeight = container.layout.height;
					if (float.IsNaN(inlineHeight) || inlineHeight <= 0f)
						return;

					var position = GetPosition();
					var height = Mathf.Max(model.Height, inlineHeight + 96f);
					if (Mathf.Abs(position.height - height) <= 1f)
						return;

					position.height = height;
					SetPosition(position);
					RefreshExpandedState();
					RefreshPorts();
				}).StartingIn(0);
			}

			private static bool ConstantValuesEqual(object left, object right)
			{
				if (ReferenceEquals(left, right))
					return true;

				if (left == null || right == null)
					return false;

				return left.Equals(right);
			}

			private object GetInlineEditorTarget(EvaluatorGraphNodeModel model)
			{
				if (model.Kind == EvaluatorGraphNodeKind.Evaluator)
					return model.Evaluator;

				if (model.Kind != EvaluatorGraphNodeKind.ConstantValue ||
					!TryGetEvaluatorArgumentTypes(model.ConstantTargetType, out var contextType, out var valueType))
					return null;

				var constantType = typeof(ConstantEvaluator<,>).MakeGenericType(contextType, valueType);
				var constant = Activator.CreateInstance(constantType);
				_constantValueField = GetFieldInHierarchy(constantType, "value");
				_constantValueField?.SetValue(constant, model.ConstantValue);
				return constant;
			}

			private static void DrawInlineProperties(InspectorProperty rootProperty, bool emptyLabel)
			{
				if (rootProperty == null)
					return;

				var useSpace = rootProperty.Children.Count >= 1;
				if (useSpace)
				{
					GUILayout.Space(2);
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(2);
					EditorGUILayout.BeginVertical();
				}

				for (var i = 0; i < rootProperty.Children.Count; i++)
				{
					var child = rootProperty.Children[i];
					if (IsGraphProperty(child))
						continue;

					if (emptyLabel)
						child.Draw(GUIContent.none);
					else
						child.Draw();
				}

				if (useSpace)
				{
					EditorGUILayout.EndVertical();
					GUILayout.Space(2);
					EditorGUILayout.EndHorizontal();
				}
			}

			private static bool IsGraphProperty(InspectorProperty property)
			{
				if (property == null)
					return false;

				if (IsGraphPropertyType(property.ValueEntry?.BaseValueType) ||
					IsGraphPropertyType(property.ValueEntry?.TypeOfValue) ||
					IsGraphPropertyType(property.Info.TypeOfValue))
					return true;

				var value = property.ValueEntry?.WeakSmartValue;
				if (value is IEvaluator || value is IEvaluatedValue)
					return true;

				if (value is IEnumerable enumerable && value is not string)
				{
					foreach (var item in enumerable)
					{
						if (item is IEvaluator)
							return true;
					}
				}

				return false;
			}

			private static bool IsGraphPropertyType(Type type)
			{
				if (type == null)
					return false;

				if (typeof(IEvaluator).IsAssignableFrom(type) || typeof(IEvaluatedValue).IsAssignableFrom(type))
					return true;

				var elementType = GetEnumerableElementType(type);
				return elementType != null && typeof(IEvaluator).IsAssignableFrom(elementType);
			}

			private static Label MakeLabel(string text, bool muted)
			{
				var label = new Label(text);
				label.style.whiteSpace    = WhiteSpace.Normal;
				label.style.color         = NODE_COLOR;
				label.style.fontSize      = muted ? 9 : 10;
				label.style.marginLeft    = 4;
				label.style.marginRight   = 4;
				label.style.marginTop     = 1;
				label.style.marginBottom  = 1;
				label.style.paddingLeft   = 4;
				label.style.paddingRight  = 4;
				label.style.paddingTop    = 1;
				label.style.paddingBottom = 1;
				label.style.backgroundColor =
					muted
						? new Color(0.68f, 0.68f, 0.68f)
						: new Color(0.9f, 0.9f, 0.9f);
				label.style.borderTopLeftRadius     = 4;
				label.style.borderTopRightRadius    = 4;
				label.style.borderBottomLeftRadius  = 4;
				label.style.borderBottomRightRadius = 4;
				label.style.alignSelf               = Align.FlexStart;
				return label;
			}
		}

		private sealed class EvaluatorPort : Port
		{
			private EvaluatorPort(
				Orientation portOrientation,
				UnityDirection portDirection,
				Capacity portCapacity,
				Type type)
				: base(portOrientation, portDirection, portCapacity, type)
			{
			}

			public static EvaluatorPort Create(
				Orientation orientation,
				UnityDirection direction,
				Capacity capacity,
				Type type,
				IEdgeConnectorListener connectorListener)
			{
				var port = new EvaluatorPort(orientation, direction, capacity, type)
				{
					m_EdgeConnector = new EdgeConnector<Edge>(connectorListener)
				};
				port.AddManipulator(port.m_EdgeConnector);
				return port;
			}
		}

		private sealed class EvaluatorGraphBuilder
		{
			private readonly Dictionary<object, EvaluatorGraphNodeModel> _objectToNode = new(ReferenceComparer.Instance);
			private readonly List<EvaluatorGraphNodeModel> _nodes = new();
			private int _nextId;

			public EvaluatorGraphModel Build(IEvaluator root, Type rootTargetType)
			{
				_objectToNode.Clear();
				_nodes.Clear();
				_nextId = 0;

				var rootAnchor = BuildRootAnchor(root, rootTargetType);
				var rootModel = BuildEvaluator(root, "root");
				if (rootModel != null)
					rootAnchor.Edges.Add(new EvaluatorGraphEdgeModel(rootAnchor.Ports[0], rootModel));

				return new EvaluatorGraphModel(rootAnchor, rootTargetType, _nodes);
			}

			public EvaluatorGraphNodeModel AddEvaluator(IEvaluator evaluator, string label)
				=> BuildEvaluator(evaluator, label);

			public EvaluatorGraphNodeModel AddConstant(string label, object value, Type valueType, Type targetType)
				=> BuildConstant(label, value, valueType, targetType);

			private const string DEFAULT_ROOT_TITLE = "";

			private EvaluatorGraphNodeModel BuildRootAnchor(IEvaluator root, Type rootTargetType)
			{
				var color = root is ICondition ? EvaluatorTypeRegistryConstants.CONDITION_COLOR : EvaluatorTypeRegistryConstants.EVALUATOR_COLOR;
				var targetType = rootTargetType ?? typeof(IEvaluator);
				var model = new EvaluatorGraphNodeModel(
					_nextId++,
					EvaluatorGraphNodeKind.Root,
					null,
					null,
					DEFAULT_ROOT_TITLE,
					"",
					color,
					"root",
					SdfIconType.Diagram2,
					color);
				model.AddPort("", null, targetType, EvaluatorGraphPortKind.Root);
				_nodes.Add(model);
				return model;
			}

			private EvaluatorGraphNodeModel BuildEvaluator(IEvaluator evaluator, string label)
			{
				if (evaluator == null)
					return null;

				if (_objectToNode.TryGetValue(evaluator, out var existing))
					return existing;

				var type = evaluator.GetType();
				var presentation = GetTypePresentation(type);
				var color = presentation.IconColor;
				var model = CreateEvaluatorNode(
					evaluator,
					type,
					presentation.Name,
					GetEvaluatorSubtitle(type),
					color,
					label,
					presentation.Icon,
					color);
				_objectToNode[evaluator] = model;

				foreach (var field in GetSerializableFields(type))
				{
					var value = field.GetValue(evaluator);
					TryAddGraphValue(model, field, value);
				}

				return model;
			}

			private EvaluatorGraphNodeModel BuildConstant(string label, object value, Type valueType, Type targetType)
			{
				var model = new EvaluatorGraphNodeModel(
					_nextId++,
					EvaluatorGraphNodeKind.ConstantValue,
					null,
					null,
					"Constant",
					GetTypeName(valueType),
					EvaluatorTypeRegistryConstants.EVALUATOR_COLOR,
					label,
					SdfIconType.DiamondFill,
					EvaluatorTypeRegistryConstants.EVALUATOR_COLOR)
				{
					ConstantValue      = value,
					ConstantValueType  = valueType,
					ConstantTargetType = targetType
				};
				_nodes.Add(model);
				return model;
			}

			private EvaluatorGraphNodeModel CreateEvaluatorNode(
				IEvaluator evaluator,
				Type evaluatorType,
				string title,
				string subtitle,
				Color titleColor,
				string sourceLabel,
				SdfIconType icon,
				Color iconColor)
			{
				var model = new EvaluatorGraphNodeModel(
					_nextId++,
					EvaluatorGraphNodeKind.Evaluator,
					evaluator,
					evaluatorType,
					title,
					subtitle,
					titleColor,
					sourceLabel,
					icon,
					iconColor);
				_nodes.Add(model);
				return model;
			}

			private bool TryAddGraphValue(EvaluatorGraphNodeModel parent, FieldInfo field, object value)
			{
				if (IsEvaluatorType(field.FieldType))
				{
					var port = parent.AddPort(field.Name, field, field.FieldType, EvaluatorGraphPortKind.EvaluatorField);
					if (value is IEvaluator evaluator)
						parent.Edges.Add(new EvaluatorGraphEdgeModel(port, BuildEvaluator(evaluator, field.Name)));
					else if (TryCreateNoneCondition(field.FieldType, out var noneCondition))
						parent.Edges.Add(new EvaluatorGraphEdgeModel(port, BuildEvaluator(noneCondition, field.Name)));
					return true;
				}

				if (value is IEvaluatedValue evaluatedValue)
				{
					var targetType = GetEvaluatedValueTargetType(field.FieldType);
					var port = parent.AddPort(field.Name, field, targetType, EvaluatorGraphPortKind.EvaluatedValueField);
					var child = !evaluatedValue.IsConstant
						? BuildEvaluator(evaluatedValue.Evaluator, field.Name)
						: BuildConstant(
							field.Name,
							GetEvaluatedValueConstant(evaluatedValue),
							GetEvaluatedValueType(field.FieldType),
							targetType);
					parent.Edges.Add(new EvaluatorGraphEdgeModel(port, child));
					return true;
				}

				if (TryAddEnumerable(parent, field, value))
					return true;

				return false;
			}

			private bool TryAddEnumerable(EvaluatorGraphNodeModel parent, FieldInfo field, object value)
			{
				if (value is not IEnumerable enumerable || value is string)
					return false;

				var elementType = GetEnumerableElementType(field.FieldType);
				if (!IsEvaluatorType(elementType))
					return false;

				var index = 0;
				foreach (var item in enumerable)
				{
					if (item is not IEvaluator evaluator)
						continue;

					var label = FormatCollectionPortLabel(index);
					var port = parent.AddPort(label, field, elementType, EvaluatorGraphPortKind.EvaluatorCollectionElement, index);
					parent.Edges.Add(new EvaluatorGraphEdgeModel(port, BuildEvaluator(evaluator, label)));
					index++;
				}

				parent.AddPort("Add", field, elementType, EvaluatorGraphPortKind.EvaluatorCollectionElement, index, true);
				return true;
			}

			private static bool TryCreateNoneCondition(Type targetType, out IEvaluator evaluator)
			{
				evaluator = null;
				if (!TryGetConditionContextType(targetType, out var contextType))
					return false;

				var noneType = typeof(NoneCondition<>).MakeGenericType(contextType);
				if (!targetType.IsAssignableFrom(noneType))
					return false;

				evaluator = Activator.CreateInstance(noneType) as IEvaluator;
				return evaluator != null;
			}

			private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
			{
				for (var current = type; current != null && current != typeof(object); current = current.BaseType)
				{
					var fields = current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
					foreach (var field in fields)
					{
						if (field.IsStatic || field.IsNotSerialized || field.Name.Contains("k__BackingField"))
							continue;

						if (!field.IsPublic &&
							field.GetCustomAttribute<SerializeField>() == null &&
							field.GetCustomAttribute<SerializeReference>() == null)
							continue;

						if (field.GetCustomAttribute<HideInInspector>() != null)
							continue;

						yield return field;
					}
				}
			}

			private static bool IsEvaluatorType(Type type)
				=> type != null && typeof(IEvaluator).IsAssignableFrom(type);

			private static Type GetEnumerableElementType(Type type)
			{
				if (type == null || type == typeof(string))
					return null;

				if (type.IsArray)
					return type.GetElementType();

				if (type.IsGenericType)
					return type.GetGenericArguments().FirstOrDefault();

				return type.GetInterfaces()
					.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
					?.GetGenericArguments()
					.FirstOrDefault();
			}

			private static object GetEvaluatedValueConstant(IEvaluatedValue evaluatedValue)
			{
				var valueField = GetFieldInHierarchy(evaluatedValue.GetType(), "value");
				return valueField?.GetValue(evaluatedValue);
			}

			private static Type GetEvaluatedValueType(Type type)
			{
				return type?.IsGenericType == true && type.GetGenericArguments().Length > 1
					? type.GetGenericArguments()[1]
					: null;
			}

			private static Type GetEvaluatedValueTargetType(Type type)
			{
				if (type?.IsGenericType != true || type.GetGenericArguments().Length < 2)
					return typeof(IEvaluator);

				var args = type.GetGenericArguments();
				return typeof(Evaluator<,>).MakeGenericType(args[0], args[1]);
			}

			private static string GetEvaluatorSubtitle(Type type)
			{
				var valueType = GetEvaluatorValueType(type);
				if (valueType == null || valueType == typeof(bool))
					return null;

				return GetTypeName(valueType);
			}

			private static Type GetEvaluatorValueType(Type type)
			{
				foreach (var interfaceType in type.GetInterfaces())
				{
					if (!interfaceType.IsGenericType ||
						interfaceType.GetGenericTypeDefinition() != typeof(IEvaluator<,>))
						continue;

					return interfaceType.GetGenericArguments()[1];
				}

				return null;
			}

			private static string FormatValue(object value)
			{
				if (value == null)
					return "null";

				if (value is string text)
					return $"\"{Trim(text, 72)}\"";

				if (value is UnityObject unityObject)
					return unityObject ? unityObject.name : "Missing Unity Object";

				if (value is IEnumerable enumerable)
				{
					var items = new List<string>();
					var count = 0;
					foreach (var item in enumerable)
					{
						if (count < 4)
							items.Add(item?.ToString() ?? "null");
						count++;
					}

					var suffix = count > items.Count ? ", ..." : string.Empty;
					return $"[{string.Join(", ", items)}{suffix}]";
				}

				return Trim(value.ToString(), 96);
			}

			private static string Trim(string value, int maxLength)
			{
				if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
					return value;

				return value.Substring(0, maxLength - 1) + "…";
			}
		}

		private static FieldInfo GetFieldInHierarchy(Type type, string fieldName)
		{
			for (var current = type; current != null && current != typeof(object); current = current.BaseType)
			{
				var field = current.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				if (field != null)
					return field;
			}

			return null;
		}

		private static Type GetEnumerableElementType(Type type)
		{
			if (type == null || type == typeof(string))
				return null;

			if (type.IsArray)
				return type.GetElementType();

			if (type.IsGenericType)
				return type.GetGenericArguments().FirstOrDefault();

			return type.GetInterfaces()
				.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				?.GetGenericArguments()
				.FirstOrDefault();
		}

		private sealed class EvaluatorGraphModel
		{
			public readonly EvaluatorGraphNodeModel Root;
			public readonly Type RootTargetType;
			public readonly IReadOnlyList<EvaluatorGraphNodeModel> Nodes;
			public EvaluatorGraphPortModel RootPort => Root?.Ports.FirstOrDefault();

			public EvaluatorGraphModel(
				EvaluatorGraphNodeModel root,
				Type rootTargetType,
				IReadOnlyList<EvaluatorGraphNodeModel> nodes)
			{
				Root           = root;
				RootTargetType = rootTargetType;
				Nodes          = nodes;
			}
		}

		private sealed class EvaluatorGraphNodeModel
		{
			public readonly int Id;
			public readonly EvaluatorGraphNodeKind Kind;
			public readonly IEvaluator Evaluator;
			public readonly Type EvaluatorType;
			public readonly string Title;
			public readonly string Subtitle;
			public readonly Color TitleColor;
			public readonly string SourceLabel;
			public readonly SdfIconType Icon;
			public readonly Color IconColor;
			public readonly List<string> Rows = new();
			public readonly List<EvaluatorGraphPortModel> Ports = new();
			public readonly List<EvaluatorGraphEdgeModel> Edges = new();
			public object ConstantValue;
			public Type ConstantValueType;
			public Type ConstantTargetType;
			public Vector2 Position;

			public EvaluatorGraphNodeModel(
				int id,
				EvaluatorGraphNodeKind kind,
				IEvaluator evaluator,
				Type evaluatorType,
				string title,
				string subtitle,
				Color titleColor,
				string sourceLabel,
				SdfIconType icon,
				Color iconColor)
			{
				Id            = id;
				Kind          = kind;
				Evaluator     = evaluator;
				EvaluatorType = evaluatorType;
				Title         = title;
				Subtitle      = subtitle;
				TitleColor    = titleColor;
				SourceLabel   = sourceLabel;
				Icon          = icon;
				IconColor     = iconColor;
			}

			public float Height => Mathf.Max(86f, 54f + Mathf.Max(1, Rows.Count) * ROW_HEIGHT + Ports.Count * 5f +
				(Kind == EvaluatorGraphNodeKind.Evaluator || Kind == EvaluatorGraphNodeKind.ConstantValue ? 160f : 0f));

			public EvaluatorGraphPortModel AddPort(
				string label,
				FieldInfo field,
				Type targetType,
				EvaluatorGraphPortKind kind,
				int collectionIndex = -1,
				bool isCollectionAppend = false)
			{
				if (field != null &&
					kind is EvaluatorGraphPortKind.EvaluatorField or EvaluatorGraphPortKind.EvaluatedValueField)
				{
					label = FormatFieldPortLabel(label);
				}

				var port = new EvaluatorGraphPortModel(this, label, field, targetType, kind, collectionIndex, isCollectionAppend);
				Ports.Add(port);
				return port;
			}
		}

		private sealed class EvaluatorGraphPortModel
		{
			public readonly EvaluatorGraphNodeModel Owner;
			public string Label;
			public readonly FieldInfo Field;
			public readonly Type TargetType;
			public readonly EvaluatorGraphPortKind Kind;
			public int CollectionIndex;
			public bool IsCollectionAppend;

			public EvaluatorGraphPortModel(
				EvaluatorGraphNodeModel owner,
				string label,
				FieldInfo field,
				Type targetType,
				EvaluatorGraphPortKind kind,
				int collectionIndex,
				bool isCollectionAppend)
			{
				Owner              = owner;
				Label              = label;
				Field              = field;
				TargetType         = targetType;
				Kind               = kind;
				CollectionIndex    = collectionIndex;
				IsCollectionAppend = isCollectionAppend;
			}
		}

		private sealed class EvaluatorGraphEdgeModel
		{
			public readonly EvaluatorGraphPortModel Port;
			public readonly EvaluatorGraphNodeModel Child;

			public EvaluatorGraphEdgeModel(EvaluatorGraphPortModel port, EvaluatorGraphNodeModel child)
			{
				Port  = port;
				Child = child;
			}
		}

		private enum EvaluatorGraphNodeKind
		{
			Root,
			Evaluator,
			ConstantValue
		}

		private enum EvaluatorGraphPortKind
		{
			Root,
			EvaluatorField,
			EvaluatedValueField,
			EvaluatorCollectionElement
		}

		private sealed class ReferenceComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceComparer Instance = new();

			public new bool Equals(object x, object y) => ReferenceEquals(x, y);

			public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
		}
	}
}
