using System.Collections.Generic;
using UnityEngine;

namespace Coral_iMD
{

    internal class HarmonicPotential : PotentialBase
    {
        private float m_V0;
        private float m_2ScaledK;

        internal HarmonicPotential(float v0, float k, float timescale)
        {
            m_V0       = v0;
            m_2ScaledK = 2.0f * k * timescale * timescale;
        }

        internal override float Potential(float r)
        {
            float r_v0 = r - m_V0;
            return m_2ScaledK * 0.5f * r_v0 * r_v0;
        }

        internal override float Derivative(float r)
        {
            float r_v0 = r - m_V0;
            return m_2ScaledK * r_v0;
        }

        internal override string Name()
        {
            return "HarmonicPotential";
        }
    }

} // Coral_iMD
