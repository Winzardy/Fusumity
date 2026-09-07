namespace Analytics
{
	/// <summary>
	/// Источник установки, полученный от MMP
	/// </summary>
	/// <remarks>
	/// MMP знает источник, но пишут его в свойства пользователя другие интеграции,
	/// поэтому данные разносятся сообщением, а не прямой ссылкой между ними
	/// </remarks>
	public struct AttributionResolvedMessage
	{
		/// <summary>
		/// Данные получены на первом запуске, только для них источник достоверен
		/// </summary>
		public bool firstLaunch;

		/// <summary>
		/// Organic / Non-organic
		/// </summary>
		public string status;

		public string source;
		public string campaign;
		public string adset;
	}
}
