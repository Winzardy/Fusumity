using Sapientia.Utility;

namespace UI
{
	public partial interface IWidget : ISignalReceiver
	{
	}

	public abstract partial class UIWidget
	{
		public bool Signal(string name)
		{
			var success = OnSignalReceived(name);

			if (!success)
				GUIDebug.LogWarning($"No handler found for signal [ {name} ]");

			return success;
		}

		protected virtual bool OnSignalReceived(string name) => false;
	}

	public abstract partial class UIWidget<TLayout>
	{
		private protected virtual void OnLayoutSignalReceivedInternal(string name) => Signal(name);
	}
}
