using System;
using System.Collections.Generic;
using UnityEngine;

namespace Coral_iMD
{

using RigidTripletType = Tuple<Rigidbody, Rigidbody, Rigidbody>;

public class BondAngleInteractionManager : MonoBehaviour
{
    private List<Tuple<PotentialBase, RigidTripletType>> m_PotentialRigidbodiesPairs;

    private void Awake()
    {
        enabled = false;
    }

    private void FixedUpdate()
    {
        foreach (Tuple<PotentialBase, RigidTripletType> pot_rigids_pair in m_PotentialRigidbodiesPairs)
        {
            PotentialBase potential        = pot_rigids_pair.Item1;
            RigidTripletType rigid_triplet = pot_rigids_pair.Item2;
            Rigidbody rigid_i = rigid_triplet.Item1;
            Rigidbody rigid_j = rigid_triplet.Item2;
            Rigidbody rigid_k = rigid_triplet.Item3;
            Vector3 r_ji = rigid_i.position - rigid_j.position;
            Vector3 r_jk = rigid_k.position - rigid_j.position;
            Vector3 e_ji = r_ji.normalized;
            Vector3 e_jk = r_jk.normalized;
            float cos_theta = Vector3.Dot(e_ji, e_jk);
            float theta     = Mathf.Acos(cos_theta);
            float sin_theta = Mathf.Sin(theta);

            float inv_sin_r_ji_len = 1.0f / (sin_theta * r_ji.magnitude);
            float inv_sin_r_jk_len = 1.0f / (sin_theta * r_jk.magnitude);
            float coef             = potential.Derivative(theta);

            Vector3 Fi = -coef * inv_sin_r_ji_len * (cos_theta * e_ji - e_jk);
            Vector3 Fk = -coef * inv_sin_r_jk_len * (cos_theta * e_jk - e_ji);

            rigid_i.AddForce(Fi);
            rigid_k.AddForce(Fk);
            rigid_j.AddForce(-(Fi + Fk));
        }
    }

    internal void Init(List<Tuple<PotentialBase, RigidTripletType>> pot_rigids_pairs)
    {
        enabled = true;

        m_PotentialRigidbodiesPairs = pot_rigids_pairs;

        // setting ignore collision
        foreach (Tuple<PotentialBase, RigidTripletType> pot_rigids_pair in m_PotentialRigidbodiesPairs)
        {
            RigidTripletType rigid_triplet = pot_rigids_pair.Item2;
            Collider collider_i = rigid_triplet.Item1.GetComponent<Collider>();
            Collider collider_k = rigid_triplet.Item3.GetComponent<Collider>();
            Physics.IgnoreCollision(collider_i, collider_k);
        }
    }

    internal string PotentialName()
    {
        return m_PotentialRigidbodiesPairs[0].Item1.Name();
    }
}

} // Coral_iMD
