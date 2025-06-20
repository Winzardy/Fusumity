using System.Collections.Generic;
using Sapientia.Extensions;

namespace Analytics
{
	public struct AnalyticsEventArgs
	{
		public string id;

		public Dictionary<string, object> parameters;

		public AnalyticsEventArgs(string id) : this()
		{
			this.id = id;
		}

		public override string ToString()
		{
			var str = string.Empty;

			foreach (var pair in parameters)
			{
				if (str.IsNullOrEmpty())
					str += "\nparameters:";

				str += "\n	" + pair.Key + "=" + pair.Value + ";";
			}

			return $"\nId: {id}{str}";
		}
	}
}
