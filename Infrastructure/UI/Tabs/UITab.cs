using System;
using System.Collections.Generic;
using AssetManagement;
using UnityEngine;

namespace UI.Tabs
{
	public interface ITab : IWidget
	{
		public ITabGroup Group { get; }
		public void Initialize(UITabEntry entry, ITabGroup group);
		public void Show(bool immediate = false);
		public void Hide(bool immediate = false);
		public string Id { get; }
	}

	public interface ITabGroup : ISelectedGroup<ITab>
	{
		public bool TryGetRoot(out RectTransform root)
		{
			root = null;
			return false;
		}
	}

	public abstract class UITab<TLayout> : UISelfConstructedWidget<TLayout>, ITab
		where TLayout : UIBaseTabLayout
	{
		private const string INITIALIZE_EXCEPTION = "Initialize without entry only with installed layout";

		private UITabEntry _entry;

		protected ITabGroup _group;

		public ITabGroup Group => _group;

		protected override bool UseSetAsLastSibling => true;

		protected override RectTransform LayerRectTransform =>
			_group.TryGetRoot(out var parent) ? parent : _layout.rectTransform.parent as RectTransform;

		public abstract string Id { get; }

		protected override string LayoutPrefixName => "[Tab] ";

		#region Layout

		protected override ComponentReferenceEntry LayoutReference => _entry?.layout.LayoutReference;
		protected override bool LayoutAutoDestroy => _entry?.layout.HasFlag(LayoutAutomationMode.AutoDestroy) ?? false;
		protected override int LayoutAutoDestroyDelayMs => _entry?.layout.autoDestroyDelayMs ?? 0;
		protected override List<AssetReferenceEntry> PreloadAssets => _entry?.layout.preloadAssets;

		#endregion

		public sealed override void SetupLayout(TLayout layout)
		{
			if (_animator == null)
				SetAnimator(new DefaultTabAnimator());

			base.SetupLayout(layout);
		}

		public void Initialize(UITabEntry entry, ITabGroup group)
		{
			_entry = entry;
			_group = group;

			base.Initialize();
		}

		public sealed override void Initialize()
		{
			if (_layout == null)
				throw new Exception(INITIALIZE_EXCEPTION);

			base.Initialize();
		}

		public void Show(bool immediate = false)
		{
			if (Active)
			{
				//Неявное поведение...
				//Нужно вызывать OnHide если хотим
				//переоткрыть окно с новыми аргументами
				SetActive(false, true);
			}

			SetActive(true, immediate);
		}

		public void Hide(bool immediate = false)
		{
			if (!Active)
				return;

			SetActive(false, immediate);
		}
	}

	public static class TabExtensions
	{
		public static int IndexFromParent(this ITab tab)
		{
			return tab.Group.IndexOf(tab);
		}
	}
}
