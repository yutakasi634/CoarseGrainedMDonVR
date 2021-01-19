using System.Collections.Generic;
using UnityEngine;

internal class HarmonicPotential : PotentialBase
{
    // inherited member variable from abstract class
    // internal List<Rigidbody> m_Rigidbodies;

    private float m_V0;
    private float m_ScaledK;

    internal HarmonicPotential(float v0, float k, List<Rigidbody> rigid_bodies, float timescale)
    {
        m_V0          = v0;
        m_ScaledK     = k * timescale * timescale;
        m_Rigidbodies = rigid_bodies;
    }

    internal override float potential(float r)
    {
        float r_v0 = r - m_V0;
        return m_ScaledK * r_v0 * r_v0;
    }

    internal override float derivative(float r)
    {
        float r_v0 = r - m_V0;
        return 2.0f * m_ScaledK * r_v0;
    }
}
