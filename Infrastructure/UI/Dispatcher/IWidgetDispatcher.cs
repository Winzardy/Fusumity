using System.Collections.Generic;

namespace UI
{
	public interface IWidgetDispatcher
	{
		IEnumerable<UIWidget> GetAllActive();
		void ClearAll();
	}
}
