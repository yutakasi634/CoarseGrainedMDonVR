using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ClementiDihedralAngleManager : MonoBehaviour
{
    private List<float> m_Phi0s;
    private List<float> m_ScaledK1_2s; //     k1 / 2
    private List<float> m_ScaledK3_2s; // 3 * k3 / 2
    private List<List<Rigidbody>> m_RigidQuadruples;

    private void Awake()
    {
        enabled = false;
    }

    private void FixedUpdate()
    {
        for (int quadruple_idx = 0; quadruple_idx < m_RigidQuadruples.Count; quadruple_idx++)
        {
            List<Rigidbody> rigid_quadruple = m_RigidQuadruples[quadruple_idx];
            Rigidbody rigid_i = rigid_quadruple[0];
            Rigidbody rigid_j = rigid_quadruple[1];
            Rigidbody rigid_k = rigid_quadruple[2];
            Rigidbody rigid_l = rigid_quadruple[3];

            Vector3 r_ji = rigid_i.position - rigid_j.position;
            Vector3 r_jk = rigid_k.position - rigid_j.position;
            Vector3 r_kj = -r_jk;
            Vector3 r_lk = rigid_l.position - rigid_k.position;

            Vector3 m = Vector3.Cross(r_ji, r_jk);
            Vector3 n = Vector3.Cross(r_jk, r_lk);
            float m_len = m.magnitude;
            float n_len = n.magnitude;

            float r_jk_len   = r_jk.magnitude;
            float r_jk_lensq = r_jk_len * r_jk_len;

            float cos_phi = Vector3.Dot(m, n) / (m_len * n_len);
            float phi = Mathf.Sign(Vector3.Dot(r_ji, n)) * Mathf.Acos(cos_phi);
            float phi_diff = phi - m_Phi0s[quadruple_idx];
            float coef = 
                m_ScaledK1_2s[quadruple_idx] * Mathf.Sin(2.0f * phi_diff) -
                m_ScaledK3_2s[quadruple_idx] * Mathf.Sin(6.0f * phi_diff);

            Vector3 Fi =  coef * r_jk_len / (m_len * m_len) * m;
            Vector3 Fl = -coef * r_jk_len / (n_len * n_len) * n;

            float coef_ijk = Vector3.Dot(r_ji, r_jk) * r_jk_lensq;
            float coef_jkl = Vector3.Dot(r_lk, r_jk) * r_jk_lensq;

            rigid_i.AddForce(Fi);
            rigid_j.AddForce((coef_ijk - 1.0f) * Fi - coef_jkl * Fl);
            rigid_k.AddForce((coef_jkl - 1.0f) * Fl - coef_ijk * Fi);
            rigid_l.AddForce(Fl);
        }
    }

    internal void Init(List<float> v0s, List<float> k1s, List<float> k3s,
        List<List<Rigidbody>> rigid_quadruples, float timescale)
    {
        enabled = true;

        Assert.AreEqual(rigid_quadruples.Count, v0s.Count,
                "The number of v0 should equal to that of dihedral quadruples.");
        Assert.AreEqual(rigid_quadruples.Count, k1s.Count,
                "The number of k should equal to that of dihedral quadruples.");
        Assert.AreEqual(rigid_quadruples.Count, k3s.Count,
                "The number of n should equal to that of dihedral quadruples.");

        m_Phi0s = v0s;
        m_RigidQuadruples = rigid_quadruples;
        m_ScaledK1_2s = new List<float>();
        m_ScaledK3_2s = new List<float>();
        for(int quadruple_idx = 0; quadruple_idx < m_RigidQuadruples.Count; quadruple_idx++)
        {
            m_ScaledK1_2s.Add(k1s[quadruple_idx] * timescale * timescale * 0.5f);
            m_ScaledK3_2s.Add(k3s[quadruple_idx] * timescale * timescale * 0.5f);
        }

        // setting ignore collision
        foreach (List<Rigidbody> rigid_quadruple in rigid_quadruples)
        {
            Collider collider_i = rigid_quadruple[0].GetComponent<Collider>();
            Collider collider_l = rigid_quadruple[3].GetComponent<Collider>();
            Physics.IgnoreCollision(collider_i, collider_l);
        }
    }

}
