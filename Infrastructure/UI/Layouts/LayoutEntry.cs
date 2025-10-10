using System;
using System.Collections.Generic;
using AssetManagement;
using Sapientia.Extensions;
using UnityEngine;

namespace UI
{
	[Flags]
	public enum LayoutAutomationMode
	{
		None = 0,

		/// <summary>
		/// Загрузить ассет с версткой в память (обычно для этого есть отдельный объект или логика, например: <see cref="AssetsPreloader"/>)
		/// </summary>
		Preload = 1 << 0,

		/// <summary>
		/// Уничтожает верстку с настраиваемой задержкой
		/// </summary>
		AutoDestroy = 1 << 1
	}

	[Serializable]
	public class LayoutEntry<TLayout> : LayoutEntry
		where TLayout : UIBaseLayout
	{
		[SerializeField]
		private ComponentReferenceEntry<TLayout> _layoutReference;

		public override ComponentReferenceEntry LayoutReference => _layoutReference;
	}

	public abstract class LayoutEntry
	{
		public abstract ComponentReferenceEntry LayoutReference { get; }

		[Tooltip("Список автоматизаций для работы с версткой.\n" +
			"<b>" + nameof(LayoutAutomationMode.AutoDestroy) + "</b> - авто-удаление верстки (+Release) через заданную задержку (delay)\n" +
			"<b>" + nameof(LayoutAutomationMode.Preload) +
			"</b> - загрузить верстку в память при запуске приложения (возможно потребуется поддержки от разработчика)")]
		public LayoutAutomationMode automationMode = LayoutAutomationMode.None;

		public int autoDestroyDelayMs = 5000;

		/// <summary>
		/// Список ассетов для предзагрузки при использовании этой верстки. <see cref="UISelfConstructedWidget{TLayout}"/>
		/// </summary>
		[Space]
		[Tooltip("Список ассетов для предзагрузки при использовании этой верстки.")]
		public List<AssetReferenceEntry> preloadAssets;

		public bool HasFlag(LayoutAutomationMode mode) => automationMode.Has(mode);
	}
}
