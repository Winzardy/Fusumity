using UnityEngine;

namespace UI
{
	/// <summary>
	/// Автодеактивация на Awake
	///
	/// Компонент используется в основном для того, чтобы в определённых ситуациях, например при работе с template's,
	/// не приходилось вручную отключать их.
	/// </summary>
	public class AutoDeactivation : MonoBehaviour
	{
		private void Awake() => gameObject.SetActive(false);
	}
}
