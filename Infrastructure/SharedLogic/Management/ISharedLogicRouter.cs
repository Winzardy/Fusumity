using System;
using Sapientia;

namespace SharedLogic
{
	public interface ISharedLogicRouter
	{
		public IDateTimeProvider DateProvider { get; }
		public void SetupServerTime(DateTime newDateTime);
		public bool ExecuteCommand<T>(in T command) where T : struct, ICommand;
	}
}
