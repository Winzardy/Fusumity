using System;
using System.Linq;
using Fusumity.Attributes;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public class FoldoutContainerPropertyDrawer : OdinAttributeDrawer<FoldoutContainerAttribute>
	{
		private bool _isExpanded;
		private bool _initialized;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (!_initialized)
			{
				_isExpanded  = Attribute.ExpandedByDefault;
				_initialized = true;
			}

			var visibleChildren = Property.Children.Where(x => x.State.Visible).ToList();
			if (visibleChildren.Count == 0)
				return;


			if (visibleChildren.Count == 1)
			{
				Header();
				return;
			}

			if (Attribute.UseBox)
				FusumityEditorGUILayout.BeginCardBox(Attribute.BoxColor);
			{
				FusumityEditorGUILayout.FoldoutContainer(Header, Body, ref _isExpanded, this);
			}
			if (Attribute.UseBox)
				FusumityEditorGUILayout.EndCardBox();


			Rect Header()
			{
				var mainProperty = visibleChildren[0];
				mainProperty.Draw(label);
				return mainProperty.LastDrawnValueRect;
			}

			void Body()
			{
				foreach (var child in visibleChildren.Skip(1))
					child.Draw();
			}
		}
	}
}
