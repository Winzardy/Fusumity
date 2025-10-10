using UnityEngine;

namespace Fusumity.MVVM
{
	public abstract class ViewCollectionLayout<TViewLayout> : MonoBehaviour
	{
		public TViewLayout prefab;
		public Transform root;
	}
}
