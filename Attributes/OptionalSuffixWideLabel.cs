using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

/// <summary>
/// Используется для полей с ObjectField, где Odin дополнительно рисует кнопку
/// открытия объекта (иконка карандаша), из-за чего требуется больший отступ
/// </summary>
[IncludeMyAttributes]
[SuffixLabel("Optional          ", true)]
[Conditional("UNITY_EDITOR")]
public class OptionalSuffixWideLabel : Attribute
{
}

[IncludeMyAttributes]
[SuffixLabel("Optional   ", true)]
[Conditional("UNITY_EDITOR")]
public class OptionalSuffixLabel : Attribute
{
}
