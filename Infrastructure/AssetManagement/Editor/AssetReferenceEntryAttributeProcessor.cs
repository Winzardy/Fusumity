namespace AssetManagement.Editor
{
	public class AssetReferenceEntryAttributeProcessor : BaseAssetReferenceEntryAttributeProcessor<IAssetReferenceEntry>
	{
		protected override string FieldName => nameof(AssetReferenceEntry.assetReference);
	}
}
