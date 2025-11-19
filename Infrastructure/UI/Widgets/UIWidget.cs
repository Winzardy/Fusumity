using System;
using System.ComponentModel;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Utility;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// Строительный блок-кирпичик, который может содержать вложенные Widget.<br/>
	/// Widget совмещает роли View и Controller (MVC-семейство), но при передаче ViewModel извне
	/// может выступать как чистая View<br/>
	/// Название выбрано для удобства, чтобы сразу было понятно, что речь идёт о UI view<br/>
	/// </summary>
	public abstract partial class UIWidget : CompositeDisposable, IWidget
	{
		protected const string INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE_FORMAT =
			"Initialization override for widget by type [ {0} ]";

		protected const string INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE = "Initialization override for widget";

		private CancellationTokenSource _disposeCts;

		protected bool _activeInHierarchy = true;

		private bool _active;

		/// <summary>
		/// Флаг немедленного выполнения (анимации при OnShow) при последнем вызове
		/// </summary>
		protected bool _immediate;

		/// <inheritdoc cref="IWidget{TLayout}.Active"/>
		public bool Active => _active;

		/// <inheritdoc cref="IWidget{TLayout}.Visible"/>
		public virtual bool Visible => Active;

		/// <inheritdoc cref="IWidget{TLayout}.Visible"/>
		public virtual bool Open => Active;

		protected virtual bool UseCustomReset => false;

		public abstract RectTransform RectTransform { get; }

		public string Layer { get; protected set; }

		public abstract event WidgetShownDelegate Shown;
		public abstract event WidgetHiddenDelegate Hidden;
		public abstract event WidgetLayoutInstalledDelegate LayoutInstalled;
		public abstract event WidgetLayoutClearedDelegate LayoutCleared;

		protected CancellationTokenSource DisposeCancellationTokenSource
		{
			get
			{
				_disposeCts ??= new CancellationTokenSource();
				return _disposeCts;
			}
		}

		protected CancellationToken DisposeCancellationToken => DisposeCancellationTokenSource.Token;

		/// <inheritdoc cref="IWidget{TLayout}.Active"/>
		public bool IsActive() => Active;

		/// <inheritdoc cref="IWidget{TLayout}.Visible"/>
		public bool IsVisible() => Visible;

		/// <inheritdoc cref="IWidget{TLayout}.Open"/>
		public bool IsOpen() => Open;

		/// <summary>
		/// Internal <br/><br/>
		///
		/// Чтобы Intellisense не отображал в Rider'е метод, нужно включить фильтрацию по этому аттрибуту:<br/>
		/// Settings -> Editor -> General -> Code Completion -> Filter members by [EditorBrowsable]
		/// </summary>
		//TODO: посмотреть удобно ли это в итоге, хотелось бы просто этот метод не светил перед глазами
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public abstract void Initialize();

		public virtual void Reset(bool deactivate = true)
		{
			if (!UseCustomReset)
				if (_children.Any())
					foreach (var child in _children)
						child.Reset(deactivate);

			OnReset(deactivate);
		}

		protected virtual void OnReset(bool deactivate)
		{
		}

		protected sealed override void OnDisposeInternal() => OnDisposedInternal();

		private protected virtual void OnDisposedInternal()
		{
			DisposeChildren();

			AsyncUtility.Trigger(ref _disposeCts);

			GC.SuppressFinalize(this);
			OnDispose();
		}

		public void SetActive(bool active, bool immediate = false, bool useCacheImmediate = true)
		{
			if (useCacheImmediate)
				_immediate = immediate;

			if (Active == active)
				return;

			_active = active;

			if (!_activeInHierarchy)
				return;

			SetActiveInternal(active, immediate);
		}

		protected internal virtual void SetActiveInternal(bool active, bool immediate)
		{
			if (active)
			{
				SetActiveInHierarchyForChildren(true);
				OnShow();
			}
			else
			{
				OnHide();
				SetActiveInHierarchyForChildren(false);
			}
		}

		private void SetActiveInHierarchy(bool active)
		{
			if (_activeInHierarchy == active)
				return;

			var prevActiveInHierarchy = _activeInHierarchy;

			if (!active && Active && prevActiveInHierarchy)
				SetActiveInternal(false, true);

			_activeInHierarchy = active;

			if (active && Active && !prevActiveInHierarchy)
				SetActiveInternal(true, true);
		}

		/// <summary>
		/// Вызывается при активации виджета (<see cref="SetActive"/> и только если есть верстка (<see cref="UIBaseLayout"/>)
		/// </summary>
		protected virtual void OnShow()
		{
		}

		/// <summary>
		/// Вызывается при деактивации виджета (<see cref="SetActive"/>)
		/// </summary>
		protected virtual void OnHide()
		{
		}

		/// <summary>
		/// Подождать пока виджет станет видимым
		/// </summary>
		public async UniTask WaitOpening(CancellationToken? cancellationToken = null)
		{
			if (cancellationToken.HasValue)
			{
				try
				{
					using var linked = DisposeCancellationTokenSource.Link(cancellationToken.Value);
					await UniTask.WaitUntil(IsVisible, cancellationToken: linked.Token);
				}
				catch (OperationCanceledException)
				{
					OnVisibleOperationCanceledException(true);
				}
			}
			else
			{
				await UniTask.WaitUntil(IsVisible, cancellationToken: DisposeCancellationToken);
			}
		}

		/// <summary>
		/// Подождать пока виджет скроется
		/// </summary>
		public async UniTask WaitClosing(CancellationToken? cancellationToken = null)
		{
			if (cancellationToken.HasValue)
			{
				try
				{
					using var linkedCts = DisposeCancellationTokenSource.Link(cancellationToken.Value);
					await UniTask.WaitWhile(IsVisible, cancellationToken: linkedCts.Token);
				}
				catch (OperationCanceledException)
				{
					OnVisibleOperationCanceledException(false);
				}
			}
			else
			{
				await UniTask.WaitWhile(IsVisible, cancellationToken: DisposeCancellationToken);
			}
		}

		protected virtual void OnVisibleOperationCanceledException(bool openingOrClosing)
		{
		}

		public static implicit operator bool(UIWidget widget) => widget != null;
	}

	public delegate void WidgetShownDelegate(IWidget widget);

	public delegate void WidgetHiddenDelegate(IWidget widget);

	public delegate void WidgetLayoutClearedDelegate([CanBeNull] UIBaseLayout layout);

	public delegate void WidgetLayoutInstalledDelegate(UIBaseLayout layout);
}
