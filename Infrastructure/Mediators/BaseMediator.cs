using System;
using Content;
using Sapientia.Extensions;
using UnityEngine.Scripting;

namespace Mediators
{
	/// <summary>
	/// <para>
	/// Роль Mediator связывать логику между различными системами без прямой зависимости.
	/// Никто не знает о Mediator, это он знает о всех.
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
	public interface IMediator : IDisposable
	{
		public void Initialize();

		public bool IsDeferred => true;
	}

	/// <inheritdoc cref="IMediator"/>
	[Preserve]
	public abstract class BaseMediator : MessageSubscriber, IMediator
	{
		public virtual bool IsDeferred => true;

		public virtual void Initialize() => OnInitialize();

		protected abstract void OnInitialize();
	}

	[Preserve]
	public abstract class BaseMediator<TSettings> : BaseMediator
	{
		protected TSettings _settings;

		public override void Initialize()
		{
			ContentManager.Get(out _settings);

			base.Initialize();
		}
	}
}
