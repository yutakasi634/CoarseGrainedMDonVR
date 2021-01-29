using System.Collections.Generic;
using UnityEngine;

namespace Coral_iMD
{

    internal class ClementiDihedralPotential : PotentialBase
    {
        private float m_Phi0;
        private float m_ScaledK;

        internal ClementiDihedralPotential(float phi0, float k, float timescale)
        {
            m_Phi0  = phi0;
            m_ScaledK = timescale * timescale * k;
        }

        internal override float Potential(float phi)
        {
            float phi_diff = phi - m_Phi0;
            return m_ScaledK * (2.0f - Mathf.Cos(phi_diff) - Mathf.Cos(3.0f * phi_diff));
        }

        internal override float Derivative(float phi)
        {
            float phi_diff = phi - m_Phi0;
            return 0.5f * m_ScaledK * (Mathf.Sin(2.0f * phi_diff) + 3.0f * Mathf.Sin(6.0f * phi_diff));
        }

        internal override string Name()
        {
            return "ClementiDihedralPotential";
        }
    }
} // Coral_iMD
