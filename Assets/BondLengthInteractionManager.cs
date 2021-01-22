using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Coral_iMD
{

    using RigidPairType           = Tuple<Rigidbody, Rigidbody>;

    public class BondLengthInteractionManager : MonoBehaviour
    {
        private List<Tuple<PotentialBase, RigidPairType>> m_PotentialRigidbodiesPairs;

        private void Awake()
        {
            enabled = false;
        }

        private void FixedUpdate()
        {
            foreach (Tuple<PotentialBase, RigidPairType> pot_rigids_pair in m_PotentialRigidbodiesPairs)
            {
                PotentialBase potential  = pot_rigids_pair.Item1;
                RigidPairType rigid_pair = pot_rigids_pair.Item2;
                Rigidbody rigid_i = rigid_pair.Item1;
                Rigidbody rigid_j = rigid_pair.Item2;
                Vector3 dist_vec = rigid_j.position - rigid_i.position;
                Vector3 norm_vec = dist_vec.normalized;
                float   coef     = potential.Derivative(dist_vec.magnitude);
                rigid_i.AddForce( coef * norm_vec);
                rigid_j.AddForce(-coef * norm_vec);
            }
        }

        internal void Init(List<Tuple<PotentialBase, RigidPairType>> pot_rigids_pairs)
        {
            enabled = true;

            m_PotentialRigidbodiesPairs = pot_rigids_pairs;

            // setting ingnore collision
            foreach (Tuple<PotentialBase, RigidPairType> pot_rigids_pair in m_PotentialRigidbodiesPairs)
            {
                RigidPairType rigid_pair = pot_rigids_pair.Item2;
                Collider collider_i = rigid_pair.Item1.GetComponent<Collider>();
                Collider collider_j = rigid_pair.Item2.GetComponent<Collider>();
                Physics.IgnoreCollision(collider_i, collider_j);
            }
        }

        internal string PotentialName()
        {
            return m_PotentialRigidbodiesPairs[0].Item1.Name();
        }
    }

} // Coral_iMD
