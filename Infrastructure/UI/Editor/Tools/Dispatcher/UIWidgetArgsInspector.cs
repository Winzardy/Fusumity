using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Utility;
using Sapientia.Extensions.Reflection;
using Sapientia.Reflection;
using Sapientia.ServiceManagement;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
	using UnityObject = UnityEngine.Object;

	[Serializable]
	public struct UIWidgetArgsInspector
	{
		public static UIWidgetArgsInspector Empty = default;

		private object _args;

		private bool _useLabel;

		private Type _argsType;
		private Type _concreteArgsType;
		private Type[] _concreteArgsTypes;
		private ConstructorInfo _constructor;
		private ConstructorParameterInspector[] _constructorParameterInspectors;
		private (Type type, PropertyTree tree) _typeArgsToTree;

		private bool _shouldTryCreateFirstArgs;

		public void SetType(Type argsType, bool useLabel = false)
		{
			if (_argsType == argsType)
			{
				_useLabel = useLabel;
				return;
			}

			Clear();

			_argsType = argsType;
			_useLabel = useLabel;
		}

		public void Clear()
		{
			_args = null;
			_argsType = null;
			_concreteArgsType = null;
			_concreteArgsTypes = null;
			_constructor = null;
			_constructorParameterInspectors = null;
			_typeArgsToTree = default;
			_useLabel = false;
			_shouldTryCreateFirstArgs = true;
		}

		public object GetArgs(bool autoClearOnDispose = true)
		{
			EnsureConcreteArgsType();
			var type = GetTargetArgsType();

			var editableType = ResolveEditableViewModelType(type);
			if (editableType != null)
			{
				if (_args == null || !editableType.IsInstanceOfType(_args))
					TryCreateArgs(editableType);

				return GetCurrentArgs(autoClearOnDispose);
			}

			EnsureConstructor(type);

			if (ShouldCreateByConstructor(type))
				TryCreateByConstructor();
			else
				TryCreateArgs(type);

			return GetCurrentArgs(autoClearOnDispose);
		}

		private object GetCurrentArgs(bool autoClearOnDispose = true)
		{
			var args = _args;

			if (autoClearOnDispose && args is IDisposable)
				_args = null;

			return args;
		}

		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			if (_argsType == null)
			{
				TryClearTree();
				return;
			}

			var treeType = GetTargetArgsType() ?? _argsType;
			if (_typeArgsToTree.type != null && treeType != _typeArgsToTree.type)
			{
				TryClearTree();
			}

			if (_useLabel)
			{
				GUILayout.Label("Args", SirenixGUIStyles.CenteredGreyMiniLabel);
				GUILayout.Space(-3);
				SirenixEditorGUI.HorizontalLineSeparator(Color.gray.WithAlpha(0.1f));
			}

			if (typeof(UnityObject).IsAssignableFrom(_argsType))
			{
				var newObj = SirenixEditorFields.UnityObjectField(_args as UnityObject, _argsType, allowSceneObjects: false);
				if (!ReferenceEquals(newObj, _args))
				{
					_args = newObj;
					_typeArgsToTree.tree = null;
					_typeArgsToTree.type = _argsType;
				}

				return;
			}

			if (_argsType.IsPolymorphic())
			{
				EnsureConcreteArgsType();
				DrawPolymorphicTypeSelector();

				if (_concreteArgsType == null)
					return;
			}
			else
			{
				_concreteArgsType = _argsType;
			}

			if (TryDrawEditableViewModelArgs(_concreteArgsType))
				return;

			DrawArgsObject(_concreteArgsType);
		}

		private void DrawPolymorphicTypeSelector()
		{
			var concreteArgsType = _concreteArgsType;
			var concreteTypes = CollectConcreteTypes();

			var selected = GenericSelector<Type>.DrawSelectorDropdown(
				GUIContent.none,
				new GUIContent(concreteArgsType != null ? GetTypeName(concreteArgsType) : "None"),
				rect =>
				{
					var selector = new GenericSelector<Type>(
						"Select Args Type",
						concreteTypes,
						false,
						GetTypeName);

					selector.EnableSingleClickToSelect();
					if (concreteArgsType != null)
						selector.SetSelection(concreteArgsType);
					selector.SelectionTree.Config.DrawSearchToolbar = true;
					selector.ShowInPopup(rect);

					return selector;
				});

			if (selected == null || !selected.Any())
				return;

			var selectedType = selected.FirstOrDefault();
			if (selectedType == null || selectedType == _concreteArgsType)
				return;

			SetConcreteArgsType(selectedType);
		}

		private IEnumerable<Type> CollectConcreteTypes()
		{
			return _concreteArgsTypes ??= _argsType.GetAllTypes()
				.Where(x => !x.IsGenericTypeDefinition)
				.OrderBy(GetTypeName)
				.ToArray();
		}

		private void EnsureConcreteArgsType()
		{
			if (_argsType == null || !_argsType.IsPolymorphic() || _concreteArgsType != null)
				return;

			var type = CollectConcreteTypes().FirstOrDefault();
			if (type != null)
				SetConcreteArgsType(type);
		}

		private void SetConcreteArgsType(Type type)
		{
			_args = null;
			_concreteArgsType = type;
			_constructor = null;
			_constructorParameterInspectors = null;
			_typeArgsToTree.tree = null;
			_typeArgsToTree.type = type;
			_shouldTryCreateFirstArgs = true;
		}

		private Type GetTargetArgsType() => _argsType == null ? null : _argsType.IsPolymorphic() ? _concreteArgsType : _argsType;

		private void EnsureConstructor(Type type)
		{
			if (type == null || !CanUseConstructor(type))
				return;

			if (_constructor?.DeclaringType == type)
				return;

			SetConstructor(SelectDefaultConstructor(type));
		}

		private bool DrawConstructorParameters()
		{
			var type = GetTargetArgsType();
			if (type == null || !CanUseConstructor(type))
				return false;

			DrawConstructorSelector(type);

			if (!ShouldCreateByConstructor(type))
				return false;

			var parameters = _constructor.GetParameters();
			for (int i = 0; i < parameters.Length; i++)
			{
				_constructorParameterInspectors[i].Draw();
			}

			return true;
		}

		private void DrawConstructorSelector(Type type)
		{
			var constructors = GetConstructors(type);
			if (constructors.Length <= 1)
				return;

			var constructor = _constructor;
			var selected = GenericSelector<ConstructorInfo>.DrawSelectorDropdown(
				GUIContent.none,
				new GUIContent(GetConstructorName(constructor)),
				rect =>
				{
					var selector = new GenericSelector<ConstructorInfo>(
						"Select Constructor",
						constructors,
						false,
						GetConstructorName);

					selector.EnableSingleClickToSelect();
					if (constructor != null)
						selector.SetSelection(constructor);
					selector.ShowInPopup(rect);

					return selector;
				});

			if (selected == null || !selected.Any())
				return;

			var selectedConstructor = selected.FirstOrDefault();
			if (selectedConstructor == null || selectedConstructor == _constructor)
				return;

			SetConstructor(selectedConstructor);
		}

		private void SetConstructor(ConstructorInfo constructor)
		{
			_constructor = constructor;
			_args = null;
			_typeArgsToTree.tree = null;

			var parameters = _constructor?.GetParameters();
			if (parameters == null || parameters.Length == 0)
			{
				_constructorParameterInspectors = null;
				return;
			}

			_constructorParameterInspectors = new ConstructorParameterInspector[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				var parameter = parameters[i];
				_constructorParameterInspectors[i] = new ConstructorParameterInspector(parameter);
			}
		}

		private bool TryDrawEditableViewModelArgs(Type type)
		{
			var editableType = ResolveEditableViewModelType(type);
			if (editableType == null)
				return false;

			if (_args == null || !editableType.IsInstanceOfType(_args))
			{
				_args = null;
				TryCreateArgs(editableType);
				_typeArgsToTree.tree = null;
				_typeArgsToTree.type = editableType;
			}

			if (_args == null)
			{
				GUILayout.Label($"{GetTypeName(editableType)} не удалось создать", EditorStyles.miniLabel);
				return true;
			}

			DrawEditableViewModelMembers(_args, editableType);
			return true;
		}

		private void DrawArgsObject(Type type)
		{
			EnsureConstructor(type);
			if (DrawConstructorParameters())
				return;

			if (TryDrawSimpleField(type))
				return;

			if (_argsType.IsPolymorphic())
			{
				if (_shouldTryCreateFirstArgs)
				{
					TryCreateArgs(type);

					_shouldTryCreateFirstArgs = false;
				}

				if (_args == null)
					return;
			}
			else
			{
				if (_args == null || _args.GetType() != type)
				{
					TryCreateArgs(type);
					_typeArgsToTree.tree = null;
				}
			}

			_typeArgsToTree.type ??= type;
			_typeArgsToTree.tree ??= PropertyTree.Create(_args, SerializationBackend.Odin);

			_typeArgsToTree.tree.Draw(false);
			_typeArgsToTree.tree.ApplyChanges();

			var ve = _typeArgsToTree.tree.RootProperty?.ValueEntry;
			if (ve == null)
				return;

			var newValue = ve.WeakSmartValue;
			if (ReferenceEquals(newValue, _args))
				return;

			_args = newValue;
			if (newValue != null && newValue.GetType() != _argsType && !_argsType.IsAssignableFrom(newValue.GetType()))
				_typeArgsToTree.tree = null;
		}

		private bool TryDrawSimpleField(Type type)
		{
			if (type == null)
				return false;

			var nullableType = Nullable.GetUnderlyingType(type);
			if (nullableType != null)
			{
				_args = null;
				GUILayout.Label($"Nullable<{GetTypeName(nullableType)}> аргумент будет передан как null", EditorStyles.miniLabel);
				return true;
			}

			if (typeof(Delegate).IsAssignableFrom(type))
			{
				_args = null;
				GUILayout.Label("Delegate аргумент будет передан как null", EditorStyles.miniLabel);
				return true;
			}

			if (type == typeof(string))
			{
				_args = EditorGUILayout.TextField(_args as string ?? string.Empty);
				return true;
			}

			if (type == typeof(bool))
			{
				_args = EditorGUILayout.Toggle(_args is bool value && value);
				return true;
			}

			if (type.IsEnum)
			{
				_args ??= Activator.CreateInstance(type);
				_args = EditorGUILayout.EnumPopup((Enum) _args);
				return true;
			}

			if (type == typeof(int))
			{
				_args = EditorGUILayout.IntField(_args is int value ? value : 0);
				return true;
			}

			if (type == typeof(long))
			{
				_args = EditorGUILayout.LongField(_args is long value ? value : 0L);
				return true;
			}

			if (type == typeof(float))
			{
				_args = EditorGUILayout.FloatField(_args is float value ? value : 0f);
				return true;
			}

			if (type == typeof(double))
			{
				_args = EditorGUILayout.DoubleField(_args is double value ? value : 0d);
				return true;
			}

			return false;
		}

		private void TryCreateArgs(Type type)
		{
			if (type == null || _args != null && _args.GetType() == type)
				return;

			var defaultValue = CreateDefaultValue(type);
			if (defaultValue != null)
			{
				_args = defaultValue;
				return;
			}

			_args = type.CreateInstanceSafe(out var exception);
			if (exception != null)
				GUIDebug.LogException(exception);
		}

		private void TryCreateByConstructor()
		{
			if (_constructor == null)
				return;

			var parameters = _constructor.GetParameters();
			var args = new object[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				args[i] = _constructorParameterInspectors[i].GetValue();
			}

			_args = null;
			try
			{
				_args = _constructor.Invoke(args);
			}
			catch (Exception e)
			{
				GUIDebug.LogException(e);
			}
		}

		private static bool CanUseConstructor(Type type)
		{
			return type != null &&
				!type.IsValueType &&
				type != typeof(string) &&
				!type.IsArray &&
				!typeof(Delegate).IsAssignableFrom(type);
		}

		private bool ShouldCreateByConstructor(Type type)
		{
			return CanUseConstructor(type) &&
				_constructor != null &&
				_constructor.GetParameters().Length > 0;
		}

		private static ConstructorInfo SelectDefaultConstructor(Type type)
		{
			return GetConstructors(type)
				.OrderBy(x => x.GetParameters().Length == 0 ? 0 : 1)
				.ThenBy(x => x.GetParameters().Count(p => !p.IsOptional && !p.HasDefaultValue))
				.ThenBy(x => x.GetParameters().Length)
				.FirstOrDefault();
		}

		private static ConstructorInfo[] GetConstructors(Type type)
		{
			return type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(x => !x.IsPrivate)
				.ToArray();
		}

		private static Type GetParameterType(ParameterInfo parameter)
		{
			var type = parameter.ParameterType;
			return type.IsByRef ? type.GetElementType() : type;
		}

		private static object CreateDefaultValue(Type type)
		{
			if (type == null)
				return null;

			if (type == typeof(string))
				return string.Empty;

			if (type.IsValueType)
				return Activator.CreateInstance(type);

			if (type.IsArray)
			{
				var elementType = type.GetElementType();
				return elementType == null ? null : Array.CreateInstance(elementType, 0);
			}

			return null;
		}

		private static bool TryGetDefaultParameterValue(ParameterInfo parameter, out object value)
		{
			if (!parameter.HasDefaultValue)
			{
				value = CreateDefaultValue(GetParameterType(parameter));
				return value != null;
			}

			value = parameter.DefaultValue;
			if (value == DBNull.Value || value == Missing.Value)
				value = null;

			return true;
		}

		private static string GetConstructorName(ConstructorInfo constructor)
		{
			if (constructor == null)
				return "None";

			var parameters = constructor.GetParameters();
			if (parameters.Length == 0)
				return "ctor()";

			return $"ctor({string.Join(", ", parameters.Select(GetParameterName))})";
		}

		private static string GetParameterName(ParameterInfo parameter)
			=> $"{GetTypeName(GetParameterType(parameter))} {parameter.Name}";

		private static string GetTypeName(Type type)
		{
			if (type == null)
				return "null";

			if (!type.IsGenericType)
				return type.Name;

			var name = type.Name;
			var index = name.IndexOf('`');
			if (index >= 0)
				name = name[..index];

			return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(GetTypeName))}>";
		}

		private static Type ResolveEditableViewModelType(Type type)
		{
			if (IsEditableViewModelType(type))
				return type;

			if (type == null || !type.IsPolymorphic())
				return null;

			return type.GetAllTypes()
				.Where(IsEditableViewModelType)
				.OrderBy(GetTypeName)
				.FirstOrDefault();
		}

		private static bool IsEditableViewModelType(Type type)
		{
			if (type == null ||
			    type.IsInterface ||
			    type.IsAbstract ||
			    type.IsGenericTypeDefinition ||
			    !type.Name.Contains("ViewModel", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return type.Name.Contains("Animic", StringComparison.OrdinalIgnoreCase) ||
				type.Name.Contains("Anemic", StringComparison.OrdinalIgnoreCase);
		}

		private static readonly Dictionary<Type, EditableMember[]> _editableViewModelMembersByType = new();

		private static void DrawEditableViewModelMembers(object target, Type type)
		{
			var members = GetEditableViewModelMembers(type);
			if (members.Length == 0)
			{
				GUILayout.Label("Нет редактируемых полей", EditorStyles.miniLabel);
				return;
			}

			for (int i = 0; i < members.Length; i++)
				DrawEditableViewModelMember(target, members[i]);
		}

		private static EditableMember[] GetEditableViewModelMembers(Type type)
		{
			if (_editableViewModelMembersByType.TryGetValue(type, out var members))
				return members;

			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Where(x => !x.IsInitOnly && CanDrawEditableMemberType(x.FieldType))
				.Select(x => new EditableMember(x));

			var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.Where(x =>
					x.CanRead &&
					x.CanWrite &&
					x.GetIndexParameters().Length == 0 &&
					CanDrawEditableMemberType(x.PropertyType))
				.Select(x => new EditableMember(x));

			members = fields
				.Concat(properties)
				.OrderBy(x => x.MetadataToken)
				.ToArray();

			_editableViewModelMembersByType[type] = members;
			return members;
		}

		private static bool CanDrawEditableMemberType(Type type)
		{
			return type == typeof(string) ||
				type == typeof(bool) ||
				type.IsEnum ||
				type == typeof(int) ||
				type == typeof(long) ||
				type == typeof(float) ||
				type == typeof(double) ||
				type == typeof(Vector2) ||
				type == typeof(Vector3) ||
				type == typeof(Vector4) ||
				type == typeof(Color) ||
				typeof(UnityObject).IsAssignableFrom(type);
		}

		private static void DrawEditableViewModelMember(object target, EditableMember member)
		{
			var value = member.GetValue(target);
			var type = member.Type;
			object newValue;

			if (type == typeof(string))
				newValue = EditorGUILayout.TextField(member.Label, value as string ?? string.Empty);
			else if (type == typeof(bool))
				newValue = EditorGUILayout.Toggle(member.Label, value is bool boolValue && boolValue);
			else if (type.IsEnum)
				newValue = EditorGUILayout.EnumPopup(member.Label, (Enum) (value ?? Activator.CreateInstance(type)));
			else if (type == typeof(int))
				newValue = EditorGUILayout.IntField(member.Label, value is int intValue ? intValue : 0);
			else if (type == typeof(long))
				newValue = EditorGUILayout.LongField(member.Label, value is long longValue ? longValue : 0L);
			else if (type == typeof(float))
				newValue = EditorGUILayout.FloatField(member.Label, value is float floatValue ? floatValue : 0f);
			else if (type == typeof(double))
				newValue = EditorGUILayout.DoubleField(member.Label, value is double doubleValue ? doubleValue : 0d);
			else if (type == typeof(Vector2))
				newValue = EditorGUILayout.Vector2Field(member.Label.text, value is Vector2 vector2Value ? vector2Value : default);
			else if (type == typeof(Vector3))
				newValue = EditorGUILayout.Vector3Field(member.Label.text, value is Vector3 vector3Value ? vector3Value : default);
			else if (type == typeof(Vector4))
				newValue = EditorGUILayout.Vector4Field(member.Label.text, value is Vector4 vector4Value ? vector4Value : default);
			else if (type == typeof(Color))
				newValue = EditorGUILayout.ColorField(member.Label, value is Color colorValue ? colorValue : default);
			else
				newValue = EditorGUILayout.ObjectField(member.Label, value as UnityObject, type, true);

			if (!Equals(value, newValue))
				member.SetValue(target, newValue);
		}

		private class EditableMember
		{
			private readonly FieldInfo _field;
			private readonly PropertyInfo _property;

			public int MetadataToken { get; }
			public Type Type { get; }
			public GUIContent Label { get; }

			public EditableMember(FieldInfo field)
			{
				_field = field;

				MetadataToken = field.MetadataToken;
				Type = field.FieldType;
				Label = new GUIContent(field.Name.TrimStart('_').NicifyText());
			}

			public EditableMember(PropertyInfo property)
			{
				_property = property;

				MetadataToken = property.MetadataToken;
				Type = property.PropertyType;
				Label = new GUIContent(property.Name.NicifyText());
			}

			public object GetValue(object target)
				=> _field != null ? _field.GetValue(target) : _property.GetValue(target);

			public void SetValue(object target, object value)
			{
				if (_field != null)
					_field.SetValue(target, value);
				else
					_property.SetValue(target, value);
			}
		}

		private class ConstructorParameterInspector
		{
			private static readonly MethodInfo _tryGetServiceMethod = typeof(ServiceLocator)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.First(x =>
					x.Name == nameof(ServiceLocator.TryGet) &&
					x.IsGenericMethodDefinition &&
					x.GetParameters().Length == 1);

			private static readonly Dictionary<Type, Type[]> _parameterTypeToEnumerableServiceTypes = new();

			private readonly ParameterInfo _parameter;
			private readonly Type _parameterType;
			private readonly Type _editableViewModelType;
			private readonly GUIContent _label;

				private object _value;
				private bool _editableViewModelCreationFailed;
				private object[] _serviceItems;
				private GUIContent[] _serviceItemNames;

			public ConstructorParameterInspector(ParameterInfo parameter)
			{
				_parameter = parameter;
				_parameterType = GetParameterType(parameter);
				_editableViewModelType = ResolveEditableViewModelType(_parameterType);
				_label = new GUIContent(_parameter.Name.NicifyText());

				if (TryGetDefaultParameterValue(parameter, out var defaultValue))
					_value = defaultValue;
			}

			public void Draw()
			{
				var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

				if (_parameterType == null)
				{
					EditorGUI.LabelField(rect, _label, new GUIContent("null"));
					return;
				}

				if (typeof(Delegate).IsAssignableFrom(_parameterType))
				{
					_value = null;
					EditorGUI.LabelField(rect, _label, new GUIContent($"{GetTypeName(_parameterType)} будет передан как null"));
					return;
				}

				if (TryDrawEditableViewModel(rect))
					return;

				if (TryDrawServiceParameter(rect))
					return;

				if (Event.current.type == EventType.Layout || _serviceItems == null)
					RefreshEnumerableServiceItems();

				if (TryDrawEnumerableServiceDropdown(rect))
					return;

				if (TryDrawSimpleField(rect))
					return;

				if (typeof(UnityObject).IsAssignableFrom(_parameterType))
				{
					_value = EditorGUI.ObjectField(rect, _label, _value as UnityObject, _parameterType, false);
					return;
				}

				var nullableType = Nullable.GetUnderlyingType(_parameterType);
				if (nullableType != null)
				{
					_value = null;
					EditorGUI.LabelField(rect, _label, new GUIContent($"Nullable<{GetTypeName(nullableType)}> будет передан как null"));
					return;
				}

				_value = null;
				EditorGUI.LabelField(rect, _label, new GUIContent($"{GetTypeName(_parameterType)} будет передан как null"));
			}

			public object GetValue()
			{
				if (_value == null && _parameterType?.IsValueType == true && Nullable.GetUnderlyingType(_parameterType) == null)
					return Activator.CreateInstance(_parameterType);

				return _value;
			}

			private bool TryDrawEditableViewModel(Rect rect)
			{
				if (_editableViewModelType == null)
					return false;

				EnsureEditableViewModelValue();
				if (_value == null)
				{
					EditorGUI.LabelField(rect, _label, new GUIContent($"{GetTypeName(_editableViewModelType)} не удалось создать"));
					return true;
					}

					EditorGUI.LabelField(rect, _label, new GUIContent(GetTypeName(_editableViewModelType)));
					DrawEditableViewModelMembers(_value, _editableViewModelType);
					return true;
				}

			private void EnsureEditableViewModelValue()
			{
				if (_value != null && _editableViewModelType.IsInstanceOfType(_value))
					return;

				if (_editableViewModelCreationFailed)
					return;

				_value = _editableViewModelType.CreateInstanceSafe(out var exception);
				if (exception == null)
					return;

				_editableViewModelCreationFailed = true;
				GUIDebug.LogException(exception);
			}

			private bool TryDrawServiceParameter(Rect rect)
			{
				if (!TryGetService(_parameterType, out var service))
					return false;

				_value = service;
				EditorGUI.LabelField(rect, _label, new GUIContent($"ServiceLocator: {GetTypeName(service.GetType())}"));
				return true;
			}

			private bool TryDrawEnumerableServiceDropdown(Rect rect)
			{
				var items = _serviceItems;
				if (items == null || items.Length == 0)
					return false;

				var current = GetValue();
				var index = Array.IndexOf(items, current);
				if (index < 0)
					index = 0;

				_value = items[index];

				var selected = EditorGUI.Popup(rect, _label, index, _serviceItemNames);
				if (selected != index)
					_value = items[selected];

				return true;
			}

			private bool TryDrawSimpleField(Rect rect)
			{
				if (_parameterType == typeof(string))
				{
					_value = EditorGUI.TextField(rect, _label, _value as string ?? string.Empty);
					return true;
				}

				if (_parameterType == typeof(bool))
				{
					_value = EditorGUI.Toggle(rect, _label, _value is bool value && value);
					return true;
				}

				if (_parameterType.IsEnum)
				{
					_value ??= Activator.CreateInstance(_parameterType);
					_value = EditorGUI.EnumPopup(rect, _label, (Enum) _value);
					return true;
				}

				if (_parameterType == typeof(int))
				{
					_value = EditorGUI.IntField(rect, _label, _value is int value ? value : 0);
					return true;
				}

				if (_parameterType == typeof(long))
				{
					_value = EditorGUI.LongField(rect, _label, _value is long value ? value : 0L);
					return true;
				}

				if (_parameterType == typeof(float))
				{
					_value = EditorGUI.FloatField(rect, _label, _value is float value ? value : 0f);
					return true;
				}

				if (_parameterType == typeof(double))
				{
					_value = EditorGUI.DoubleField(rect, _label, _value is double value ? value : 0d);
					return true;
				}

				return false;
			}

			private void RefreshEnumerableServiceItems()
			{
				_serviceItems = TryGetEnumerableServiceItems(_parameterType);
				if (_serviceItems == null || _serviceItems.Length == 0)
				{
					_serviceItemNames = null;
					return;
				}

				_serviceItemNames = new GUIContent[_serviceItems.Length];
				for (int i = 0; i < _serviceItems.Length; i++)
					_serviceItemNames[i] = new GUIContent(_serviceItems[i]?.ToString() ?? "null");
			}

			private static object[] TryGetEnumerableServiceItems(Type itemType)
			{
				var serviceTypes = GetEnumerableServiceTypes(itemType);
				for (int i = 0; i < serviceTypes.Length; i++)
				{
					var serviceType = serviceTypes[i];
					if (!TryGetService(serviceType, out var service))
						continue;

					if (service is not IEnumerable enumerable)
						continue;

					return enumerable.Cast<object>()
						.Where(x => x != null)
						.ToArray();
				}

				return null;
			}

			private static Type[] GetEnumerableServiceTypes(Type itemType)
			{
				if (_parameterTypeToEnumerableServiceTypes.TryGetValue(itemType, out var serviceTypes))
					return serviceTypes;

				var enumerableType = typeof(IEnumerable<>).MakeGenericType(itemType);
				serviceTypes = enumerableType.GetAllTypes()
					.Where(x => !x.IsGenericTypeDefinition)
					.Prepend(enumerableType)
					.Distinct()
					.OrderBy(x => x.Name)
					.ToArray();

				_parameterTypeToEnumerableServiceTypes[itemType] = serviceTypes;
				return serviceTypes;
			}

			private static bool TryGetService(Type serviceType, out object service)
			{
				var args = new object[] {null};
				var method = _tryGetServiceMethod.MakeGenericMethod(serviceType);
				if (method.Invoke(null, args) is bool success && success)
				{
					service = args[0];
					return service != null;
				}

				service = null;
				return false;
			}
		}

		private void TryClearTree()
		{
			_args = null;
			_typeArgsToTree.tree?.Dispose();
			_typeArgsToTree.tree = null;
			_typeArgsToTree.type = null;
		}
	}
}
