using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Coral_iMD
{

    public class HarmonicAngleManager : MonoBehaviour
    {
        private List<float>           m_Theta0s;
        private List<float>           m_2ScaledKs;
        private List<List<Rigidbody>> m_RigidTriples;

        private void Awake()
        {
            enabled = false;
        }

        private void FixedUpdate()
        {
            for (int triplet_idx = 0; triplet_idx < m_RigidTriples.Count; triplet_idx++)
            {
                List<Rigidbody> rigid_triplet = m_RigidTriples[triplet_idx];
                Rigidbody rigid_i = rigid_triplet[0];
                Rigidbody rigid_j = rigid_triplet[1];
                Rigidbody rigid_k = rigid_triplet[2];
                Vector3 r_ji = rigid_i.position - rigid_j.position;
                Vector3 r_jk = rigid_k.position - rigid_j.position;
                Vector3 e_ji = r_ji.normalized;
                Vector3 e_jk = r_jk.normalized;
                float cos_theta = Vector3.Dot(e_ji, e_jk);
                float theta = Mathf.Acos(cos_theta);
                float sin_theta = Mathf.Sin(theta);

                float k2_inv_sin_r_ji_len = m_2ScaledKs[triplet_idx] / (sin_theta * r_ji.magnitude);
                float k2_inv_sin_r_jk_len = m_2ScaledKs[triplet_idx] / (sin_theta * r_jk.magnitude);

                float theta0 = m_Theta0s[triplet_idx];
                float half_cos_theta = 0.5f * cos_theta;
                Vector3 Fi =
                    -k2_inv_sin_r_ji_len * (theta - theta0) * (half_cos_theta * e_ji - e_jk);
                Vector3 Fk =
                    -k2_inv_sin_r_jk_len * (theta - theta0) * (half_cos_theta * e_jk - e_ji);

                rigid_i.AddForce(Fi);
                rigid_k.AddForce(Fk);
                rigid_j.AddForce(-(Fi + Fk));
            }
        }

        // Update is called once per frame
        internal void Init(List<float> theta0s, List<float> ks,
            List<List<Rigidbody>> rigid_triples, float timescale)
        {
            enabled = true;

            m_Theta0s = theta0s;
            m_RigidTriples = rigid_triples;
            m_2ScaledKs = new List<float>();
            foreach (float k in ks)
            {
                m_2ScaledKs.Add(2.0f * k * timescale * timescale);
            }

            // setting ignore collision
            foreach (List<Rigidbody> rigid_triple in m_RigidTriples)
            {
                Collider first_collider = rigid_triple[0].GetComponent<Collider>();
                Collider second_collider = rigid_triple[2].GetComponent<Collider>();
                Physics.IgnoreCollision(first_collider, second_collider);
            }
        }
    }

} // Coral_iMD
