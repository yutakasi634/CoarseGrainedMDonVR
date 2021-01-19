using System.Collections.Generic;
using UnityEngine;

internal abstract class PotentialBase
{
    internal List<Rigidbody> m_Rigidbodies;

    internal abstract float potential (float r);
    internal abstract float derivative(float r);
}
