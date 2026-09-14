using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Extensions;
using Sapientia.Utility;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Прослойка-класс в основном от которого наследуются все корневые виджеты (Window, Popup)
	/// </summary>
	public abstract class UIClosableRootWidget<TLayout> : UIBaseRootWidget<TLayout>
		where TLayout : UIBaseLayout
	{
		private CancellationTokenSource _closableCts;
		private UIHoldButtonView _closeHold;

		protected CancellationToken ClosableCancellationToken => ClosableCancellationTokenSource.Token;
		protected CancellationTokenSource ClosableCancellationTokenSource => _closableCts ??= new CancellationTokenSource();

		protected internal override void OnBeganClosingInternal()
		{
			AsyncUtility.TriggerAndSetNull(ref _closableCts);
			base.OnBeganClosingInternal();
		}

		public abstract void RequestClose();

		protected virtual bool CloseHoldEnabled => true;

		protected void SetupCloseHold(Button close)
		{
			if (!CloseHoldEnabled || close is not CustomButton button)
				return;

			_closeHold = new UIHoldButtonView(button);
			_closeHold.Completed += OnCloseHeld;
		}

		protected void ClearCloseHold()
		{
			if (_closeHold == null)
				return;

			_closeHold.Completed -= OnCloseHeld;
			DisposeUtility.DisposeAndSetNull(ref _closeHold);
		}

		protected virtual void OnCloseHeld() => UIDispatcher.HideAll();

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
