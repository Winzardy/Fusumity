using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using InputManagement;
using Sapientia.ServiceManagement;
using Sirenix.OdinInspector;

namespace Booting.Input
{
	[TypeRegistryItem(
		"\u2009Input", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.Joystick)]
	[Serializable]
	public class InputManagementBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 40;

		private IInputReader _inputReader;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			_inputReader = new DesktopInputReader();
			_inputReader.RegisterAsService();

			return UniTask.CompletedTask;
		}

		protected override void OnDispose() => _inputReader.Dispose();
	}
}
