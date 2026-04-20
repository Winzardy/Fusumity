// using UnityEditor;
// using UnityEditor.Overlays;
// using UnityEngine;
//
// namespace UI.Editor
// {
// 	[Overlay(typeof(SceneView), "Layout Animations", "Layout Animations")]
// 	public class UIBaseLayoutEditorOverlay : IMGUIOverlay, ITransientOverlay
// 	{
// 		private const float MIN_OVERLAY_WIDTH = 255f;
// 		private const float PLAY_BUTTON_WIDTH = 52f;
//
// 		private UIBaseLayout _target;
//
// 		public bool visible { get => _target && _target.UseLayoutAnimations && !Application.isPlaying; }
//
// 		public override void OnCreated()
// 		{
// 			Selection.selectionChanged += OnSelectionChanged;
// 			OnSelectionChanged();
// 		}
//
// 		public override void OnWillBeDestroyed()
// 		{
// 			Selection.selectionChanged -= OnSelectionChanged;
// 			_target = null;
// 		}
//
// 		public override void OnGUI()
// 		{
// 			if (!_target)
// 			{
// 				OnSelectionChanged();
// 				if (!_target)
// 					return;
// 			}
//
// 			GUILayoutUtility.GetRect(MIN_OVERLAY_WIDTH, 0f, GUILayout.ExpandWidth(true));
//
// 			EditorGUILayout.LabelField(_target.name, EditorStyles.boldLabel);
//
// 			if (_target.customSequences == null || _target.customSequences.Count == 0)
// 			{
// 				EditorGUILayout.HelpBox("customSequences is empty.", MessageType.Info);
// 				return;
// 			}
//
//
// 		}
//
// 		private void OnSelectionChanged()
// 		{
// 			_target = null;
//
// 			if (Selection.activeGameObject == null)
// 				return;
//
// 			_target = Selection.activeGameObject.GetComponentInParent<UIBaseLayout>(true);
// 		}
// 	}
// }
