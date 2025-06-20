using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
	/// <summary>
	/// Просьба при использовании в верстке писать summary
	/// в которым через see cref указать тип верстки (пример ниж)
	/// </summary>
	/*
		/// <summary>
		/// <see cref="ResourceWidgetLayout"/>
		/// </summary>
		public UIGroupLayout group;
	*/
	public class UIGroupLayout : UIBaseLayout
	{
		public RectTransform parent;

		[SerializeField, FormerlySerializedAs("template")]
		[ShowIf(nameof(ShowTemplate))]
		private UIBaseLayout _template;

		[Tooltip("Вызывать force перестройку LayoutGroup после Show")]
		public bool forceRebuild;

		public virtual UIBaseLayout template => _template;

		protected virtual Type GetTemplateType() => null;

		private bool ShowTemplate() => GetTemplateType() == null;
	}

	public class UIGroupLayout<T> : UIGroupLayout where T : UIBaseLayout
	{
		[SerializeField]
		private T _template;

		public override UIBaseLayout template => _template;
		protected override Type GetTemplateType() => typeof(T);
	}
}
