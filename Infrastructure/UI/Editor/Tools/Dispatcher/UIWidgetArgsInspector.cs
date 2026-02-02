using System;
using Fusumity.Utility;
using Sapientia.Collections;
using Sapientia.Extensions.Reflection;
using Sapientia.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
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
			_typeArgsToTree = default;
			_useLabel = false;
			_shouldTryCreateFirstArgs = true;
		}

		public object GetArgs() => _args;

		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			if (_argsType == null)
			{
				TryClearTree();
				return;
			}

			if (_typeArgsToTree.type != null && _argsType != _typeArgsToTree.type)
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
				if (_shouldTryCreateFirstArgs)
				{
					if (_args == null)
					{
						_args = _argsType.GetAllTypes()
							.First()
							.CreateInstanceSafe(out var exception);

						if (exception != null)
							GUIDebug.LogException(exception);
					}

					_shouldTryCreateFirstArgs = false;
				}

				var picked = SirenixEditorFields.PolymorphicObjectField(_args, _argsType, false);
				if (!ReferenceEquals(picked, _args))
				{
					_args = picked;
					_typeArgsToTree.tree = null;
					_typeArgsToTree.type = _argsType;
				}

				if (_args == null)
					return;
			}
			else
			{
				if (_args == null || _args.GetType() != _argsType)
				{
					_args = _argsType.CreateInstanceSafe();
					_typeArgsToTree.tree = null;
				}
			}

			_typeArgsToTree.type ??= _argsType;
			_typeArgsToTree.tree ??= PropertyTree.Create(_args, SerializationBackend.Odin);

			_typeArgsToTree.tree.Draw(false);
			_typeArgsToTree.tree.ApplyChanges();

			var ve = _typeArgsToTree.tree.RootProperty?.ValueEntry;
			if (ve != null)
			{
				var newValue = ve.WeakSmartValue;
				if (!ReferenceEquals(newValue, _args))
				{
					_args = newValue;
					if (newValue != null && newValue.GetType() != _argsType && !_argsType.IsAssignableFrom(newValue.GetType()))
					{
						_typeArgsToTree.tree = null;
					}
				}
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
