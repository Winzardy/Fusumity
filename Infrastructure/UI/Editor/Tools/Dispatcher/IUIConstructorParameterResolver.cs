using System;
using UnityEngine;

namespace UI.Editor
{
	public interface IUIConstructorParameterResolver
	{
		object CreateState(Type parameterType);
		bool TryDraw(object state, GUIContent label, ref object value);
		void Dispose(object state);
	}
}
