using System.Collections;
using System.Collections.Generic;

namespace UI
{
	public partial class UIGroup<TWidget, TWidgetLayout, TWidgetArgs> : IEnumerable<TWidget>
	{
		public IEnumerator<TWidget> GetEnumerator() => _widgets.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _widgets.GetEnumerator();
	}
}
