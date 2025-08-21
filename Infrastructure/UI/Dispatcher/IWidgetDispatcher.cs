using System.Collections.Generic;

namespace UI
{
	public interface IWidgetDispatcher
	{
		public IEnumerable<UIWidget> GetAllActive();
	}
}
