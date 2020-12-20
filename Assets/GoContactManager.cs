using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class GoContactManager : MonoBehaviour
{
    private List<float> m_V0s;
    private List<float> m_60ScaledKsV02;
    private List<List<Rigidbody>> m_RigidPairs;

    private void Awake()
    {
        enabled = false;
    }
    // Update is called once per frame
    private void FixedUpdate()
    {
        for (int pair_idx = 0; pair_idx < m_RigidPairs.Count; pair_idx++)
        {
            List<Rigidbody> rigid_pair = m_RigidPairs[pair_idx];
            Rigidbody rigid_first  = rigid_pair[0];
            Rigidbody rigid_second = rigid_pair[1];
            Vector3 dist_vec = rigid_second.position - rigid_first.position;
            float r0invr   = m_V0s[pair_idx] / dist_vec.magnitude;
            float r0invr2  = r0invr * r0invr;
            float r0invr4  = r0invr2 * r0invr2;
            float r0invr8  = r0invr4 * r0invr4;
            float r0invr12 = r0invr4 * r0invr8;
            float r0invr14 = r0invr12 * r0invr2;
            float coef = m_60ScaledKsV02[pair_idx] * (r0invr12 - r0invr14);
            rigid_first.AddForce ( coef * dist_vec);
            rigid_second.AddForce(-coef * dist_vec);
        }
    }

    internal void Init(List<float> v0s, List<float> ks, List<List<Rigidbody>> rigid_pairs, float timescale)
    {
        enabled = true;
        enabled = true;
        Assert.AreEqual(rigid_pairs.Count, v0s.Count,
            "The number of v0 should equal to that of lennard-jones particles.");
        Assert.AreEqual(rigid_pairs.Count, ks.Count,
            "The number of k should equal to that of lennard-jones particles.");

        m_RigidPairs = rigid_pairs;
        m_V0s           = new List<float>();
        m_60ScaledKsV02 = new List<float>();
        for (int pair_idx = 0; pair_idx < rigid_pairs.Count; pair_idx++)
        {
            float v0 = v0s[pair_idx];
            m_V0s.Add(v0);
            m_60ScaledKsV02.Add(60.0f * ks[pair_idx] * timescale * timescale / (v0 * v0));
        }

        // setting ignore collision
        foreach (List<Rigidbody> rigid_pair in m_RigidPairs)
        {
            Collider first_collider  = rigid_pair[0].GetComponent<Collider>();
            Collider second_collider = rigid_pair[1].GetComponent<Collider>();
            Physics.IgnoreCollision(first_collider, second_collider);
        }
    }
}
