using System;
using Fusumity.Editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	public class FilteredContentEntryPropertyDrawer : OdinValueDrawer<IFilteredContentEntry>
	{
		private const string FOLDOUT_KEY = nameof(FilteredContentEntryPropertyDrawer) + "_Foldout";

		private LocalPersistentContext<bool> _foldout;
		private PropertyTree _tree;

		protected override void Initialize()
		{
			_foldout = Property.Context.GetPersistent(this, FOLDOUT_KEY, false);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var filtered = ValueEntry.SmartValue;
			var value = filtered.Entry.RawValue;
			var type = filtered.Type;

			EditorGUI.BeginChangeCheck();
			value = SirenixEditorFields.PolymorphicObjectField(label, value, type, true);
			var headerRect = GUILayoutUtility.GetLastRect();

			if (EditorGUI.EndChangeCheck())
			{
				filtered.SetValue(value);
				ValueEntry.SmartValue = filtered;

				_tree?.Dispose();
				_tree = null;

				Property!.MarkSerializationRootDirty();
			}

			if (value != null && (_tree == null || _tree.WeakTargets[0] != value))
				_tree = PropertyTree.Create(value, ValueEntry.SerializationBackend);

			Action body = _tree != null ? Body : null;
			var foldout = _foldout.Value;
			FusumityEditorGUILayout.FoldoutContainer(Header, body, ref foldout, this, true);
			_foldout.Value = foldout;

			Rect Header() => headerRect;

			void Body()
			{
				EditorGUI.BeginChangeCheck();
				_tree.Draw(false);
				if (EditorGUI.EndChangeCheck())
				{
					_tree.ApplyChanges();
					Property.MarkSerializationRootDirty();
				}
			}
		}
	}
}
