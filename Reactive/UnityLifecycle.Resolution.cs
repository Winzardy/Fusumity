using UnityEngine;

namespace Fusumity.Reactive
{
	//TODO: Resolution???
	public partial class UnityLifecycle
	{
		private Resolution _resolution;

		private void Awake()
		{
			_resolution = Screen.currentResolution;
		}

		private void LateUpdateResolution()
		{
			var currentResolution = Screen.currentResolution;
			if (_resolution.width != currentResolution.width || _resolution.height != currentResolution.height)
			{
				_resolution = currentResolution;
				ResolutionChangedEvent.ImmediatelyInvoke();
			}
		}
	}
}
