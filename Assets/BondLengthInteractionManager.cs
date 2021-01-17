using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class BondLengthInteractionManager : MonoBehaviour
{
    private List<PotentialBase>   m_Potentails;

    private void Awake()
    {
        enabled = false;
    }

    private void FixedUpdate()
    {
        foreach (PotentialBase potential in m_Potentials)
        {
            List<Rigidbody> rigid_pair = potential.m_RigidPair;
            Rigidbody rigid_i = rigid_pair[0];
            Rigidbody rigid_j = rigid_pair[1];
            Vector3 dist_vec = rigid_j.position - rigid_i.position;
            Vector3 norm_vec = dist_vec.normalized;
            float   coef     = potential.derivative(dist_vec.magnitude);
            rigid_i.AddForce( coef * norm_vec);
            rigid_j.AddForce(-coef * norm_vec);
        }
    }

    internal void Init(List<PotentialBase> potentials)
    {
        enabled = true;

        m_Potentials = potentials;

        // setting ingnore collision
        foreach (PotentailBase potentail in m_Potentials)
        {
            List<Rigidbody> potential.m_RigidPair;
            Collider collider_i = potential.GetComponent<Collider>();
            Collider collider_j = potential.GetComponent<Collider>();
            Physics.IgnoreCollision(collider_i, collider_j);
        }
    }
}
