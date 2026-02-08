using System;
using Content;
using Sapientia.Extensions;
using UnityEngine.Scripting;

namespace Presenters
{
	[Preserve]
	[Obsolete]
	public abstract class BasePresenter : MessageSubscriber
	{
		public void Initialize() => OnInitialize();
		protected abstract void OnInitialize();
	}
}
