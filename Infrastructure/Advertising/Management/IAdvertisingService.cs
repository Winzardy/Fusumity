namespace Advertising
{
	/// <summary>
	/// Проектно-ориентированный интерфейс, адаптирующий <see cref="Advertising"/> под инфраструктуру и ограничения самого продукта (проекта)
	/// </summary>
	public interface IAdvertisingService
	{
		public bool CanShow(AdPlacementKey key, out AdShowError? error);
		public AdvertisingRegisterResult RegisterShow(AdPlacementKey key);
	}

	public enum AdvertisingRegisterResult
	{
		Done,
		Pending, // Interop
		// Failed
	}
}
