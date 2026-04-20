using UnityEngine;

namespace UI
{
	public class RotatePulsar : Pulsar
	{
		public Vector3 rotation;

		public bool useLocal;

		protected override void OnUpdate(float normalizedValue)
		{
			var q = Quaternion.Euler(rotation * normalizedValue);
			if (useLocal)
				transform.localRotation = q;
			else
				transform.rotation = q;
		}
	}
}
