using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Extensions;
using Sapientia.Utility;

namespace UI
{
	/// <summary>
	/// Прослойка-класс в основном от которого наследуются все корневые виджеты (Window, Popup)
	/// </summary>
	public abstract class UIClosableRootWidget<TLayout> : UIBaseRootWidget<TLayout>
		where TLayout : UIBaseCanvasGroupLayout
	{
		private CancellationTokenSource _closableCts;
		private UIHoldButtonView _closeAllHold;

		protected CancellationToken ClosableCancellationToken => ClosableCancellationTokenSource.Token;
		protected CancellationTokenSource ClosableCancellationTokenSource => _closableCts ??= new CancellationTokenSource();

		protected internal override void OnBeganClosingInternal()
		{
			AsyncUtility.TriggerAndSetNull(ref _closableCts);
			base.OnBeganClosingInternal();
		}

		protected internal override void OnLayoutInstalledInternal()
		{
			if (_layout.closeAllHold)
			{
				_closeAllHold = new UIHoldButtonView(_layout.closeAllHold);
				_closeAllHold.Completed += OnCloseAllHeld;
			}

			base.OnLayoutInstalledInternal();
		}

		protected internal override void OnLayoutClearedInternal()
		{
			if (_closeAllHold != null)
			{
				_closeAllHold.Completed -= OnCloseAllHeld;
				DisposeUtility.DisposeAndSetNull(ref _closeAllHold);
			}

			base.OnLayoutClearedInternal();
		}

		public abstract void RequestClose();

		protected virtual void OnCloseAllHeld() => UIDispatcher.HideAll();

		protected async UniTask RequestCloseAsync(int delayMs = 500)
		{
			if (!Active)
				return;

			using var linked = ClosableCancellationTokenSource.Link(DisposeCancellationToken);
			await UniTask.Delay(delayMs, cancellationToken: linked.Token);

			if (_closableCts != null)
				RequestClose();
		}

		protected void CancelRequestClose()
			=> AsyncUtility.TriggerAndSetNull(ref _closableCts);

	}

	/// <summary>
	/// Прослойка-класс в основном от которого наследуются все корневые виджеты (Screen, Window, Popup)
	/// </summary>
	public abstract class UIBaseRootWidget<TLayout> : UISelfConstructedLayerWidget<TLayout>
		where TLayout : UIBaseLayout
	{
		protected override bool UseSetAsLastSibling => true;

		/// <summary>
		/// Дает возможность использовать одинаковые настройки для разных типов
		/// Можно конечно выдавать entry по типу... Но тогда совсем жесткая привязка <br/><br/>
		/// ВАЖНО! <br/>
		/// Нельзя использовать разные EntryId для экранов (<see cref="UIScreen"/>)
		/// </summary>
		protected abstract string Id { get; }
	}
}
