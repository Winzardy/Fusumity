using System;
using Content;
using Sapientia.Extensions;
using UnityEngine.Scripting;

namespace Presenters
{
	/// <summary>
	/// <para>
	/// Роль Presenter связывать логику между различными системами без прямой зависимости.
	/// Никто не знает о Presenter, это он знает о всех.
	/// Это важно чтобы не прокидывать лишние зависимости!
	/// </para>
	/// <para>
	/// Если появилась мысль прокинуть его куда-то, это сигнал, что нужно отделить эту логику в отдельный Service!
	/// </para>
	/// <para>
	/// Еще важное уточнение, что возможно ваш кейс можно решить от обратного,
	/// например вызывать логику подписавшись на какое-то событие, а не прокидывать Presenter!
	/// </para>
	/// </summary>
	public interface IPresenter : IDisposable
	{
		public void Initialize();

		public bool IsDeferred => true;
	}

	/// <inheritdoc cref="IPresenter"/>
	[Preserve]
	public abstract class BasePresenter : MessageSubscriber, IPresenter
	{
		public virtual bool IsDeferred => true;

		public virtual void Initialize() => OnInitialize();

		protected abstract void OnInitialize();
	}

	[Preserve]
	public abstract class BasePresenter<TSettings> : BasePresenter
	{
		protected TSettings _settings;

		public override void Initialize()
		{
			ContentManager.Get(out _settings);

			base.Initialize();
		}
	}
}
