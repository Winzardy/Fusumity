using UnityEngine;

namespace UI
{
	public class OrderedLayoutRotationReactor : OrderedLayoutElementReactor
	{
		[SerializeField]
		private Transform _target;

		[Tooltip("Итоговый поворот = stepEuler * index")]
		[SerializeField]
		private Vector3 _stepEuler = new Vector3(0f, 0f, -3.5f);

		public override void OnOrderChanged(int index)
		{
			if (_target == null)
				_target = transform;

			_target.localRotation = Quaternion.Euler(_stepEuler * index);
		}

#if UNITY_EDITOR
		private void Reset()
		{
			_target = transform;
		}
#endif
	}
}
