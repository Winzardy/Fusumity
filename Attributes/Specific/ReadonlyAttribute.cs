namespace Fusumity.Attributes.Specific
{
	public class ReadonlyAttribute : FusumityDrawerAttribute
	{
		public bool ifApplicationIsPlaying;
		public bool ifApplicationIsNotPlaying;
		public bool isReadonly;

		public ReadonlyAttribute()
		{
			isReadonly = true;
		}

		public ReadonlyAttribute(bool isReadonly)
		{
			this.isReadonly = isReadonly;
		}
	}
}