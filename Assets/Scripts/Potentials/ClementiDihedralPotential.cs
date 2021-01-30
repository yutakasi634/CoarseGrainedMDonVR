using System.Collections.Generic;
using UnityEngine;

namespace Coral_iMD
{

    internal class ClementiDihedralPotential : PotentialBase
    {
        private float m_Phi0;
        private float m_ScaledK1;
        private float m_ScaledK3;

        internal ClementiDihedralPotential(float phi0, float k1, float k3, float timescale)
        {
            m_Phi0  = phi0;
            m_ScaledK1 = timescale * timescale * k1;
            m_ScaledK3 = timescale * timescale * k3;
        }

        internal override float Potential(float phi)
        {
            float phi_diff = phi - m_Phi0;
            return m_ScaledK1 * (1.0f - Mathf.Cos(phi_diff)) + m_ScaledK3 * (1.0f - Mathf.Cos(3.0f * phi_diff));
        }

        internal override float Derivative(float phi)
        {
            float phi_diff = phi - m_Phi0;
            return 0.5f * m_ScaledK1 * Mathf.Sin(2.0f * phi_diff) + 1.5f * m_ScaledK3 * Mathf.Sin(6.0f * phi_diff);
        }

        internal override string Name()
        {
            return "ClementiDihedralPotential";
        }
    }
} // Coral_iMD
