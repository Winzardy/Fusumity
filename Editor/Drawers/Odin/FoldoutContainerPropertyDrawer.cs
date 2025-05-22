using System;
using System.Linq;
using Fusumity.Attributes;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public class FoldoutContainerPropertyDrawer : OdinAttributeDrawer<FoldoutContainerAttribute>
	{
		private bool _foldout;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			Action body = Property.Children.Count > 1 ? Body : null;
			FusumityEditorGUILayout.FoldoutContainer(Header, body, ref _foldout, this);

			Rect Header()
			{
				var mainProperty = Property.Children[0];
				mainProperty.Draw(label);
				return mainProperty.LastDrawnValueRect;
			}

			void Body()
			{
				foreach (var child in Property.Children.Skip(1))
					child.Draw();
			}
		}
	}
}
