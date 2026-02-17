using UnityEngine;

namespace UI
{
	/// <summary>
	/// Авто-уничтожение на Awake
	///
	/// Компонент используется в основном для того, чтобы в определённых ситуациях, например при работе с template's,
	/// не приходилось вручную отключать их.
	/// </summary>
	public class AutoDestroy : MonoBehaviour
	{
		private void Awake() => Destroy(gameObject);
	}
}
