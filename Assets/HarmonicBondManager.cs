using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class HarmonicBondManager : MonoBehaviour
{
    private List<float>     m_V0s;
    private List<float>     m_Ks;
    private List<List<Rigidbody>> m_LJRigidPairs;

    private void Awake()
    {
        enabled = false;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        for (int pair_idx = 0; pair_idx < m_LJRigidPairs.Count; pair_idx++)
        {
            List<Rigidbody> ljrigid_pair = m_LJRigidPairs[pair_idx];
            Rigidbody ljrigid_first  = ljrigid_pair[0];
            Rigidbody ljrigid_second = ljrigid_pair[1];
            Vector3 dist_vec = ljrigid_second.position - ljrigid_first.position;
            Vector3 norm_vec = dist_vec.normalized;
            float   coef     = 2.0f * m_Ks[pair_idx] * dist_vec.magnitude;
            ljrigid_first.AddForce(coef * norm_vec);
            ljrigid_second.AddForce(-coef * norm_vec);
        }
    }

    internal void Init(List<float> v0s, List<float> ks, List<List<Rigidbody>> ljrigid_pairs)
    {
        enabled = true;
        m_V0s = v0s;
        m_Ks  = ks;
        m_LJRigidPairs = ljrigid_pairs;

        Assert.AreEqual(m_LJRigidPairs.Count, m_V0s.Count,
            "The number of v0 should equal to that of lennard-jones particles.");
        Assert.AreEqual(m_LJRigidPairs.Count, m_Ks.Count,
            "The number of k should equal to that of lennard-jones particles.");
    }
}
