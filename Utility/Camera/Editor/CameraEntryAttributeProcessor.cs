using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fusumity.Utility.Camera.Editor
{
	public class CameraEntryAttributeProcessor : OdinAttributeProcessor<CameraRenderSettings>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(CameraRenderSettings.renderIndex):
					attributes.Add(new ShowIfAttribute(nameof(CameraRenderSettings.useRenderIndex)));

					var className = nameof(CameraEntryAttributeProcessor);
					var methodName = nameof(GetURPRenderers);
					attributes.Add(new ValueDropdownAttribute($"@{className}.{methodName}($property)"));
					break;
			}
		}

		public static IEnumerable GetURPRenderers(InspectorProperty property)
		{
			var i = 0;
			var pipeline = GraphicsSettings.currentRenderPipeline;
			if (pipeline is UniversalRenderPipelineAsset urp)
			{
				var field = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				var list = (ScriptableRendererData[]) field.GetValue(urp);

				foreach (var data in list)
				{
					yield return new ValueDropdownItem(data.name, i);
					i++;
				}
			}

		}
	}
}
