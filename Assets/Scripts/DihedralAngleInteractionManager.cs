using System;
using System.Collections.Generic;
using UnityEngine;

namespace Coral_iMD
{
    using RigidQuadrupletType = Tuple<Rigidbody, Rigidbody, Rigidbody, Rigidbody>;

    public class DihedralAngleInteractionManager : MonoBehaviour
    {
        private List<Tuple<PotentialBase, RigidQuadrupletType>> m_PotentialRigidbodiesPairs;

        private void Awake()
        {
            enabled = false;
        }

        private void FixedUpdate()
        {
            foreach (Tuple<PotentialBase, RigidQuadrupletType> pot_rigids_pair in m_PotentialRigidbodiesPairs)
            {
                PotentialBase       potential  = pot_rigids_pair.Item1;
                RigidQuadrupletType rigid_quadruplet = pot_rigids_pair.Item2;
                Rigidbody rigid_i = rigid_quadruplet.Item1;
                Rigidbody rigid_j = rigid_quadruplet.Item2;
                Rigidbody rigid_k = rigid_quadruplet.Item3;
                Rigidbody rigid_l = rigid_quadruplet.Item4;

                Vector3 r_ji = rigid_i.position - rigid_j.position;
                Vector3 r_jk = rigid_k.position - rigid_j.position;
                Vector3 r_kj = -r_jk;
                Vector3 r_lk = rigid_k.position - rigid_l.position;

                Vector3 m = Vector3.Cross(r_ji, r_jk);
                Vector3 n = Vector3.Cross(r_jk, r_lk);
                float m_len = m.magnitude;
                float n_len = n.magnitude;

                float r_jk_len    = r_jk.magnitude;
                float r_jk_rlensq = 1.0f / (r_jk_len * r_jk_len);

                float cos_phi  = Mathf.Clamp(Vector3.Dot(m, n) / (m_len * n_len), -1.0f, 1.0f);
                float phi      = Mathf.Sign(Vector3.Dot(r_ji, n)) * Mathf.Acos(cos_phi);
                float coef     = potential.Derivative(phi);

                Vector3 Fi =  coef * r_jk_len / (m_len * m_len) * m;
                Vector3 Fl = -coef * r_jk_len / (n_len * n_len) * n;

                float coef_ijk = Vector3.Dot(r_ji, r_jk) * r_jk_rlensq;
                float coef_jkl = Vector3.Dot(r_lk, r_jk) * r_jk_rlensq;

                rigid_i.AddForce(Fi);
                rigid_j.AddForce((coef_ijk - 1.0f) * Fi - coef_jkl * Fl);
                rigid_k.AddForce((coef_jkl - 1.0f) * Fl - coef_ijk * Fi);
                rigid_l.AddForce(Fl);
            }
        }

        internal void Init(List<Tuple<PotentialBase, RigidQuadrupletType>> pot_rigids_pairs)
        {
            enabled = true;

            m_PotentialRigidbodiesPairs = pot_rigids_pairs;

            // setting ignore collision
            foreach (Tuple<PotentialBase, RigidQuadrupletType> pot_rigids_pair in m_PotentialRigidbodiesPairs)
            {
                RigidQuadrupletType rigid_quadruplet = pot_rigids_pair.Item2;
                Collider collider_i = rigid_quadruplet.Item1.GetComponent<Collider>();
                Collider collider_l = rigid_quadruplet.Item4.GetComponent<Collider>();
                Physics.IgnoreCollision(collider_i, collider_l);
            }
        }

        internal string PotentialName()
        {
            return m_PotentialRigidbodiesPairs[0].Item1.Name();
        }
    }
} // Coral_iMD
