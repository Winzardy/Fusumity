namespace Fusumity.Attributes.Specific
{
	public class SliderAttribute : FusumityDrawerAttribute
	{
		public readonly float min;
		public readonly float max;

		public SliderAttribute(float min, float max)
		{
			this.min = min;
			this.max = max;
		}
	}
}