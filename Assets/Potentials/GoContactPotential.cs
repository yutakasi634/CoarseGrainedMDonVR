using System.Collections.Generic;
using UnityEngine;

internal class GoContactPotential : PotentialBase
{
    // inherited member variable from abstract class
    // internal List<Rigidbody> m_Rigidbodies;

    private float m_V0;
    private float m_K;
    private float m_60ScaledK_V0;

    internal GoContactPotential(float v0, float k, List<Rigidbody> rigid_bodies, float timescale)
    {
        m_V0            = v0;
        m_K             = k;
        m_60ScaledK_V0  = 60.0f * k * timescale * timescale / v0;
        m_Rigidbodies   = rigid_bodies;
    }

    internal override float potential(float r)
    {
        float invr     = 1.0f / r;
        float r0invr   = m_V0     * invr;
        float r0invr2  = r0invr   * r0invr;
        float r0invr4  = r0invr2  * r0invr2;
        float r0invr8  = r0invr4  * r0invr4;
        float r0invr10 = r0invr8  * r0invr2;
        float r0invr12 = r0invr10 * r0invr2;
        return m_K * (5.0f * r0invr12 - 6.0f * r0invr10);
    }

    internal override float derivative(float r)
    {
        float invr     = 1.0f / r;
        float r0invr   = m_V0     * invr;
        float r0invr2  = r0invr   * r0invr;
        float r0invr4  = r0invr2  * r0invr2;
        float r0invr8  = r0invr4  * r0invr4;
        float r0invr10 = r0invr8  * r0invr2;
        float r0invr12 = r0invr10 * r0invr2;
        return m_60ScaledK_V0 * (r0invr10 - r0invr12);
    }
} 
