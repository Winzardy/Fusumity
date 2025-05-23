namespace Localizations
{
	public partial class TextLocalizationArgs
	{
		public static implicit operator TextLocalizationArgs(LocKey key) => new() {key = key};
	}
}
