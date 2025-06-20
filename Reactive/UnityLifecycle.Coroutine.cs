using System.Collections;

namespace Fusumity.Reactive
{
	public partial class UnityLifecycle
	{
		public static void ExecuteCoroutine(IEnumerator enumerator)
		{
			_instance.StartCoroutine(enumerator);
		}

		public static void CancelCoroutine(IEnumerator enumerator)
		{
			_instance.StopCoroutine(enumerator);
		}
	}
}
