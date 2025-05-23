using UnityEngine;

namespace UI
{
	public class RotatePulsar : Pulsar
	{
		public Vector3 rotation;

		protected override void OnUpdate(float normalizedValue) =>
			transform.rotation = Quaternion.Euler(rotation * normalizedValue);
	}
}
