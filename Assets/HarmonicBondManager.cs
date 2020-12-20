using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class HarmonicBondManager : MonoBehaviour
{
    private List<float>     m_V0s;
    private List<float>     m_ScaledKs;
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
            List<Rigidbody> ljrigid_pair = m_RigidPairs[pair_idx];
            Rigidbody ljrigid_first  = ljrigid_pair[0];
            Rigidbody ljrigid_second = ljrigid_pair[1];
            Vector3 dist_vec = ljrigid_second.position - ljrigid_first.position;
            Vector3 norm_vec = dist_vec.normalized;
            float   coef     = 2.0f * m_ScaledKs[pair_idx] * (dist_vec.magnitude - m_V0s[pair_idx]);
            ljrigid_first.AddForce(coef * norm_vec);
            ljrigid_second.AddForce(-coef * norm_vec);
        }
    }

    internal void Init(List<float> v0s, List<float> ks, List<List<Rigidbody>> rigid_pairs, float timescale)
    {
        enabled = true;
        Assert.AreEqual(rigid_pairs.Count, v0s.Count,
            "The number of v0 should equal to that of lennard-jones particles.");
        Assert.AreEqual(rigid_pairs.Count, ks.Count,
            "The number of k should equal to that of lennard-jones particles.");

        m_V0s = v0s;
        m_RigidPairs = rigid_pairs;
        m_ScaledKs = new List<float>();
        foreach (float k in ks)
        {
            m_ScaledKs.Add(k * timescale * timescale);
        }

        // setting ingnore collision
        foreach (List<Rigidbody> rigid_pair in m_RigidPairs)
        {
            Collider first_collider  = rigid_pair[0].GetComponent<Collider>();
            Collider second_collider = rigid_pair[1].GetComponent<Collider>();
            Physics.IgnoreCollision(first_collider, second_collider);
        }
    }
}
