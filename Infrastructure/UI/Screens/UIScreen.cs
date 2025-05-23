using System;
using System.Collections.Generic;
using AssetManagement;
using Content.Constants.UI.Layers;
using Sapientia;
using Sapientia.Extensions;

namespace UI.Screens
{
	public interface IScreen : IDisposable, IIdentifiable
	{
		public bool Active { get; }

		internal void Initialize(UIScreenEntry entry);

		internal void Show();
		internal void Hide(bool reset);

		internal IDisposable Prepare(Action callback);
	}

	/// <summary>
	/// Название типа Screen должно соотвествовать его конфигу (Entry)!
	/// </summary>
	public abstract class UIScreen<TLayout> : UIBaseRootWidget<TLayout>, IScreen
		where TLayout : UIBaseScreenLayout
	{
		private const string LAYOUT_PREFIX_NAME = "[Screen] ";

		private UIScreenEntry _entry;

		private bool? _resetting;

		string IIdentifiable.Id => Id;

		#region Layout

		protected override ComponentReferenceEntry LayoutReference => _entry.layout.LayoutReference;
		protected override bool LayoutAutoDestroy => _entry.layout.HasFlag(LayoutAutomationMode.AutoDestroy);
		protected override int LayoutAutoDestroyDelayMs => _entry.layout.autoDestroyDelayMs;
		protected override List<AssetReferenceEntry> PreloadAssets => _entry.layout.preloadAssets;

		#endregion

		protected override string Layer => LayerType.SCREENS;

		protected override bool UseSetAsLastSibling => false;

		public sealed override void SetupLayout(TLayout layout)
		{
			if (_animator == null)
				SetAnimator<DefaultScreenAnimator>();

			base.SetupLayout(layout);
		}

		[Obsolete(INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE, true)]
		public sealed override void Initialize() =>
			throw new Exception(INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE_FORMAT.Format(GetType().Name));

		void IScreen.Initialize(UIScreenEntry entry)
		{
			_entry = entry;

			base.Initialize();
		}

		void IScreen.Show()
		{
			var immediate = false;

			if (Active)
			{
				immediate = true;

				//Неявное поведение...
				//Нужно вызывать OnHide если хотим
				//переоткрыть экран
				SetActive(false, true);
			}

			SetActive(true, immediate);
		}

		void IScreen.Hide(bool reset)
		{
			_resetting = reset;
			SetActive(false);
		}

		protected override void OnEndedClosingInternal()
		{
			if (_resetting.HasValue)
			{
				if (_resetting.Value)
					Reset(false);

				_resetting = null;
			}

			base.OnEndedClosingInternal();
		}

		IDisposable IScreen.Prepare(Action callback) => Prepare(callback);

		protected override void OnUpdateVisibleInternal(bool value)
		{
			//TODO: очень многое оказалось завязано на SetActive... Нельзя просто выключать рендер канваса,
			//начинается некорректая отработка
// 			if (_layout.canvas)
// 			{
// 				_layout.canvas.enabled = Visible;
//
// #if UNITY_EDITOR
// 				const string DISABLE_STATUS = " (disabled)";
//
// 				_layout.name = Visible ? _layout.name.Replace(DISABLE_STATUS, string.Empty) :
// 					_layout.name.Contains(DISABLE_STATUS) ? _layout.name :
// 					_layout.name + DISABLE_STATUS;
// #endif
// 				return;
// 			}

			base.OnUpdateVisibleInternal(value);
		}

		protected override string LayoutPrefixName => LAYOUT_PREFIX_NAME;
	}
}
