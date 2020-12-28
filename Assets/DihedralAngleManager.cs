using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CosineDihedralAngleManager : MonoBehaviour
{
    private List<float> m_V0s;
    private List<float> m_ScaledKs;
    private List<int>   m_Ns;
    private List<List<Rigidbody>> m_RigidQuadruples;

    private void Awake()
    {
        enabled = false;
    }

    internal void Init(List<float> v0s, List<float> ks, List<int> ns,
        List<List<Rigidbody>> rigid_quadruples, float timescale)
    {
        enabled = true;

        m_V0s = v0s;
        m_RigidQuadruples = rigid_quadruples;
        m_Ns = ns;
        foreach (float k in ks)
        {
            m_ScaledKs.Add(k * timescale * timescale);
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
