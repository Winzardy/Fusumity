using System.Collections;
using System.Collections.Generic;

namespace UI
{
	public partial class UIGroup<TWidget, TWidgetLayout, TWidgetArgs> : IEnumerable<TWidget>
	{
		public IEnumerator<TWidget> GetEnumerator() => _used.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _used.GetEnumerator();
	}
}
