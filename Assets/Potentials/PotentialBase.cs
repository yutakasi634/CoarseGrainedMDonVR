using System.Collections.Generic;
using UnityEngine;

internal abstract class PotentialBase
{
    internal abstract float  Potential (float r);
    internal abstract float  Derivative(float r);
    internal abstract string Name();
}
