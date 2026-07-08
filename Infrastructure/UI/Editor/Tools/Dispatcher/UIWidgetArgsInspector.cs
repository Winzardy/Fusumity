using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Utility;
using Sapientia.Extensions.Reflection;
using Sapientia.Reflection;
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
		public static readonly UIWidgetArgsInspector Empty = default;

		private static readonly GUIContent _noneLabel = new("None");

		private object _args;

		private bool _useLabel;

		private Type _argsType;
		private Type _concreteArgsType;
		private Type[] _concreteArgsTypes;
		private GUIContent _concreteArgsTypeLabel;
		private ConstructorInfo _constructor;
		private Type _constructorOwnerType;
		private GUIContent _constructorLabel;
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
			TryClearTree();
			DisposeConstructorParameterInspectors();

			_argsType = null;
			_concreteArgsType = null;
			_concreteArgsTypes = null;
			_concreteArgsTypeLabel = null;
			_constructor = null;
			_constructorOwnerType = null;
			_constructorLabel = null;
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
					DisposeTree();
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
				_concreteArgsTypeLabel ?? _noneLabel,
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
			_concreteArgsTypeLabel = new GUIContent(GetTypeName(type));
			_constructor = null;
			_constructorOwnerType = null;
			_constructorLabel = null;
			DisposeConstructorParameterInspectors();
			DisposeTree();
			_typeArgsToTree.type = type;
			_shouldTryCreateFirstArgs = true;
		}

		private Type GetTargetArgsType() => _argsType == null ? null : _argsType.IsPolymorphic() ? _concreteArgsType : _argsType;

		private void EnsureConstructor(Type type)
		{
			if (type == null || !CanUseConstructor(type))
				return;

			if (_constructorOwnerType == type)
				return;

			_constructorOwnerType = type;
			SetConstructor(SelectDefaultConstructor(type));
		}

		private bool DrawConstructorParameters()
		{
			var type = GetTargetArgsType();
			if (type == null || !CanUseConstructor(type))
				return false;

			DrawConstructorSelector(type);

			var inspectors = _constructorParameterInspectors;
			if (inspectors == null)
				return false;

			for (int i = 0; i < inspectors.Length; i++)
			{
				inspectors[i].Draw();
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
				_constructorLabel ??= new GUIContent(GetConstructorName(constructor)),
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
			_constructorLabel = new GUIContent(GetConstructorName(constructor));
			_args = null;
			DisposeTree();
			DisposeConstructorParameterInspectors();

			var parameters = constructor?.GetParameters();
			if (parameters == null || parameters.Length == 0)
				return;

			_constructorParameterInspectors = new ConstructorParameterInspector[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				_constructorParameterInspectors[i] = new ConstructorParameterInspector(parameters[i]);
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
				DisposeTree();
				_typeArgsToTree.type = editableType;
			}

			if (_args == null)
			{
				GUILayout.Label($"Failed to create {GetTypeName(editableType)}", EditorStyles.miniLabel);
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
					DisposeTree();
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
				DisposeTree();
		}

		private bool TryDrawSimpleField(Type type)
		{
			if (type == null)
				return false;

			var nullableType = Nullable.GetUnderlyingType(type);
			if (nullableType != null)
			{
				_args = null;
				GUILayout.Label($"Nullable<{GetTypeName(nullableType)}> argument will be passed as null", EditorStyles.miniLabel);
				return true;
			}

			if (typeof(Delegate).IsAssignableFrom(type))
			{
				_args = null;
				GUILayout.Label("Delegate argument will be passed as null", EditorStyles.miniLabel);
				return true;
			}

			if (!TryDrawSimpleValue(GUIContent.none, type, _args, out var newValue))
				return false;

			_args = newValue;
			return true;
		}

		private static bool TryDrawSimpleValue(GUIContent label, Type type, object value, out object newValue)
		{
			if (type == typeof(string))
			{
				newValue = EditorGUILayout.TextField(label, value as string ?? string.Empty);
				return true;
			}

			if (type == typeof(bool))
			{
				newValue = EditorGUILayout.Toggle(label, value is bool boolValue && boolValue);
				return true;
			}

			if (type.IsEnum)
			{
				newValue = EditorGUILayout.EnumPopup(label, (Enum) (value ?? Activator.CreateInstance(type)));
				return true;
			}

			if (type == typeof(int))
			{
				newValue = EditorGUILayout.IntField(label, value is int intValue ? intValue : 0);
				return true;
			}

			if (type == typeof(long))
			{
				newValue = EditorGUILayout.LongField(label, value is long longValue ? longValue : 0L);
				return true;
			}

			if (type == typeof(float))
			{
				newValue = EditorGUILayout.FloatField(label, value is float floatValue ? floatValue : 0f);
				return true;
			}

			if (type == typeof(double))
			{
				newValue = EditorGUILayout.DoubleField(label, value is double doubleValue ? doubleValue : 0d);
				return true;
			}

			if (type == typeof(Vector2))
			{
				newValue = EditorGUILayout.Vector2Field(label.text, value is Vector2 vector2Value ? vector2Value : default);
				return true;
			}

			if (type == typeof(Vector3))
			{
				newValue = EditorGUILayout.Vector3Field(label.text, value is Vector3 vector3Value ? vector3Value : default);
				return true;
			}

			if (type == typeof(Vector4))
			{
				newValue = EditorGUILayout.Vector4Field(label.text, value is Vector4 vector4Value ? vector4Value : default);
				return true;
			}

			if (type == typeof(Color))
			{
				newValue = EditorGUILayout.ColorField(label, value is Color colorValue ? colorValue : default);
				return true;
			}

			newValue = value;
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

			var inspectors = _constructorParameterInspectors;
			var args = new object[inspectors?.Length ?? 0];
			for (int i = 0; i < args.Length; i++)
			{
				args[i] = inspectors[i].GetValue();
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
				_constructorParameterInspectors != null;
		}

		private static ConstructorInfo SelectDefaultConstructor(Type type)
		{
			return GetConstructors(type)
				.OrderBy(x => x.GetParameters().Length == 0 ? 0 : 1)
				.ThenBy(x => x.GetParameters().Count(p => !p.IsOptional && !p.HasDefaultValue))
				.ThenBy(x => x.GetParameters().Length)
				.FirstOrDefault();
		}

		private static readonly Dictionary<Type, ConstructorInfo[]> _constructorsByType = new();

		private static ConstructorInfo[] GetConstructors(Type type)
		{
			if (_constructorsByType.TryGetValue(type, out var constructors))
				return constructors;

			constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(x => !x.IsPrivate)
				.ToArray();

			_constructorsByType[type] = constructors;
			return constructors;
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

		private static readonly Dictionary<Type, string> _typeNameByType = new();

		// internal: переиспользуется резолверами параметров конструктора (см. IUIConstructorParameterResolver)
		public static string GetTypeName(Type type)
		{
			if (type == null)
				return "null";

			if (_typeNameByType.TryGetValue(type, out var cachedName))
				return cachedName;

			string typeName;
			if (!type.IsGenericType)
			{
				typeName = type.Name;
			}
			else
			{
				var name = type.Name;
				var index = name.IndexOf('`');
				if (index >= 0)
					name = name[..index];

				typeName = $"{name}<{string.Join(", ", type.GetGenericArguments().Select(GetTypeName))}>";
			}

			_typeNameByType[type] = typeName;
			return typeName;
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

			return type.Name.Contains("Anemic", StringComparison.OrdinalIgnoreCase);
		}

		private static readonly Dictionary<Type, EditableMember[]> _editableViewModelMembersByType = new();

		private static void DrawEditableViewModelMembers(object target, Type type)
		{
			var members = GetEditableViewModelMembers(type);
			if (members.Length == 0)
			{
				GUILayout.Label("No editable fields", EditorStyles.miniLabel);
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

			if (!TryDrawSimpleValue(member.Label, member.Type, value, out var newValue))
				newValue = EditorGUILayout.ObjectField(member.Label, value as UnityObject, member.Type, true);

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
			// Реализации ищутся рефлексией — сам инспектор не знает о конкретных источниках значений
			private static IUIConstructorParameterResolver[] _resolvers;

			private readonly ParameterInfo _parameter;
			private readonly Type _parameterType;
			private readonly Type _editableViewModelType;
			private readonly GUIContent _label;

			private object _value;
			private bool _editableViewModelCreationFailed;
			private GUIContent _readOnlyLabelContent;

			private bool _resolverResolved;
			private IUIConstructorParameterResolver _resolver;
			private object _resolverState;

			private object _box;
			private FieldInfo _boxValueField;
			private PropertyTree _boxTree;

			public ConstructorParameterInspector(ParameterInfo parameter)
			{
				_parameter = parameter;
				_parameterType = GetParameterType(parameter);
				_editableViewModelType = ResolveEditableViewModelType(_parameterType);
				_label = new GUIContent(_parameter.Name.NicifyText());

				if (TryGetDefaultParameterValue(parameter, out var defaultValue))
					_value = defaultValue;
			}

			public void Dispose()
			{
				_resolver?.Dispose(_resolverState);
				_resolverState = null;

				_boxTree?.Dispose();
				_boxTree = null;
			}

			public void Draw()
			{
				if (_parameterType == null)
				{
					DrawReadOnlyLabel("null");
					return;
				}

				if (typeof(Delegate).IsAssignableFrom(_parameterType))
				{
					_value = null;
					DrawReadOnlyLabel($"{GetTypeName(_parameterType)} will be passed as null");
					return;
				}

				if (TryDrawEditableViewModel())
					return;

				if (TryDrawSimpleValue(_label, _parameterType, _value, out var newValue))
				{
					_value = newValue;
					return;
				}

				if (typeof(UnityObject).IsAssignableFrom(_parameterType))
				{
					_value = EditorGUILayout.ObjectField(_label, _value as UnityObject, _parameterType, false);
					return;
				}

				if (TryDrawViaResolver())
					return;

				// Последний резерв: любой сложный тип отдаём на откуп Odin — его attribute-процессоры
				// сами знают, как нарисовать конкретные типы (ContentReference и т.п.)
				if (TryDrawOdinBoxedValue())
					return;

				_value = null;

				var nullableType = Nullable.GetUnderlyingType(_parameterType);
				DrawReadOnlyLabel(nullableType != null
					? $"Nullable<{GetTypeName(nullableType)}> will be passed as null"
					: $"{GetTypeName(_parameterType)} will be passed as null");
			}

			private void DrawReadOnlyLabel(string value)
			{
				if (_readOnlyLabelContent == null || _readOnlyLabelContent.text != value)
					_readOnlyLabelContent = new GUIContent(value);

				EditorGUILayout.LabelField(_label, _readOnlyLabelContent);
			}

			public object GetValue()
			{
				if (_value == null && _parameterType?.IsValueType == true && Nullable.GetUnderlyingType(_parameterType) == null)
					return Activator.CreateInstance(_parameterType);

				return _value;
			}

			private bool TryDrawEditableViewModel()
			{
				if (_editableViewModelType == null)
					return false;

				EnsureEditableViewModelValue();
				if (_value == null)
				{
					DrawReadOnlyLabel($"Failed to create {GetTypeName(_editableViewModelType)}");
					return true;
				}

				DrawReadOnlyLabel(GetTypeName(_editableViewModelType));
				DrawEditableViewModelMembers(_value, _editableViewModelType);
				return true;
			}

			private bool TryDrawViaResolver()
			{
				if (!_resolverResolved)
				{
					_resolverResolved = true;

					var resolvers = GetResolvers();
					for (int i = 0; i < resolvers.Length; i++)
					{
						var state = resolvers[i].CreateState(_parameterType);
						if (state == null)
							continue;

						_resolver = resolvers[i];
						_resolverState = state;
						break;
					}
				}

				return _resolver != null && _resolver.TryDraw(_resolverState, _label, ref _value);
			}

			private static IUIConstructorParameterResolver[] GetResolvers()
			{
				if (_resolvers != null)
					return _resolvers;

				var types = ReflectionUtility.GetAllTypes<IUIConstructorParameterResolver>(editor: true);
				var resolvers = new List<IUIConstructorParameterResolver>(types.Count);
				for (int i = 0; i < types.Count; i++)
				{
					if (types[i].TryCreateInstance(out IUIConstructorParameterResolver resolver))
						resolvers.Add(resolver);
				}

				_resolvers = resolvers.ToArray();
				return _resolvers;
			}

			private bool TryDrawOdinBoxedValue()
			{
				EnsureBoxedValue();

				_boxTree ??= PropertyTree.Create(_box, SerializationBackend.Odin);
				_boxTree.RootProperty.Children[nameof(ConstructorParameterBox<object>.value)]
					.Draw(GUIContent.none);
				_boxTree.ApplyChanges();
				_value = _boxValueField.GetValue(_box);
				return true;
			}

			private void EnsureBoxedValue()
			{
				if (_box != null)
					return;

				var boxType = typeof(ConstructorParameterBox<>).MakeGenericType(_parameterType);
				_box = Activator.CreateInstance(boxType);
				_boxValueField = boxType.GetField(nameof(ConstructorParameterBox<object>.value));
				_boxValueField.SetValue(_box, GetValue());
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
		}

		[Serializable]
		private class ConstructorParameterBox<T>
		{
			[HideLabel]
			public T value;
		}

		private void DisposeTree()
		{
			_typeArgsToTree.tree?.Dispose();
			_typeArgsToTree.tree = null;
		}

		private void DisposeConstructorParameterInspectors()
		{
			var inspectors = _constructorParameterInspectors;
			if (inspectors == null)
				return;

			_constructorParameterInspectors = null;
			for (int i = 0; i < inspectors.Length; i++)
			{
				inspectors[i]?.Dispose();
			}
		}

		private void TryClearTree()
		{
			_args = null;
			DisposeTree();
			_typeArgsToTree.type = null;
		}
	}
}
