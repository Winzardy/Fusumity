using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class GraphicColorIntComparisonStateSwitcher : IntComparisonStateSwitcher
	{
		[Space]

		[SerializeField]
		private Graphic _graphic;
		[SerializeField]
		private Color _trueColor;
		[SerializeField]
		private Color _falseColor;

		protected override void OnStateSwitched(bool value)
		{
			_graphic.color = value ? _trueColor : _falseColor;
		}
	}
}
