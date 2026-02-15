#if CLIENT
using System;
using Sapientia.Extensions.Reflection;
using Sirenix.OdinInspector;

namespace Trading
{
	[TypeRegistryItem(
		"\u2009Collection",
		"/",
		SdfIconType.Stack,
		darkIconColorR: R, darkIconColorG: G, darkIconColorB: B,
		darkIconColorA: A,
		lightIconColorR: R, lightIconColorG: G, lightIconColorB: B,
		lightIconColorA: A)]
	public partial class TradeRewardCollection : ITradeRewardRepresentable
	{
		/// <summary>
		/// Фильтрует типы только в инспекторе!
		/// </summary>
		public bool Filter(Type type) => type.HasAttribute<SerializableAttribute>();

		public string visual;
		public string VisualId { get => visual; }
	}
}
#endif
