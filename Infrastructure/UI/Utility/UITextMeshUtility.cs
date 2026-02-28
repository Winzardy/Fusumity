using TMPro;

namespace UI
{
	public static class UITextMeshUtility
	{
		public static string GradientText(this string text, TMP_ColorGradient gradient)
		{
			return $"<gradient={gradient.name}>{text}</gradient>";
		}
	}
}
