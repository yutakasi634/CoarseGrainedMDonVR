using System;
using System.Collections.Generic;
using UnityEngine;

namespace Coral_iMD
{
    using RigidQuadrupletType = Tuple<Rigidbody, Rigidbody, Rigidbody, Rigidbody>;

    public class DihedralAngleInteraction : MonoBehaviour
    {
        private List<Tuple<PotentialBase, RigidQuadrupletType>> m_PotentialRigidbodiesPairs;

        private void Awake()
        {
            enabled = false;
        }

        private void FixedUpdate()
        {
            // TODO: implementation
        }

        internal void Init()
        {
            // TODO: implementation
        }

        internal string PotentialName()
        {
            return m_PotentialRigidbodiesPairs[0].Item1.Name();
        }
    }
} // Coral_iMD
