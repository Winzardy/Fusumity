using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public abstract class ImageStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		protected Image _image;

		protected virtual void Reset()
		{
			_image = GetComponent<Image>();
		}
	}
}
