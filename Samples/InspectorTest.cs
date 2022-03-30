using System;
using Fusumity.Attributes.Specific;
using UnityEngine;

public class InspectorTest : MonoBehaviour
{
	[ReferenceSelectionAttribute, SerializeReference]
	public ITest test;
	public Tst test1;
	[HideLabel]
	public Tst test2;
}

public interface ITest{}

[Serializable]
public class Tst : ITest
{
	[DisableIf("c")]
	public int a;
	[EnableIf("c")]
	public float b;
	public bool c;
}

[Serializable]
public class Tst1 : ITest
{
    [Minimum("min"), Maximum("max")]
	public int a;

	public int min;
	public int max;
}
