using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Coral_iMD
{

public class LennardJonesParticle : MonoBehaviour
{
    public float sphere_radius  = 0.5f;
    public float scaled_epsilon = 0.05f;

    private Rigidbody      m_Rigidbody;
    private SphereCollider m_SphereCollider;

    private void OnTriggerStay(Collider other)
    {
        LennardJonesParticle other_lj = other.GetComponent<LennardJonesParticle>();
        if (other_lj == null)
        {
            return;
        }

        Vector3 dist_vec = other.attachedRigidbody.position - transform.position;
        float sigma      = sphere_radius + other_lj.sphere_radius;
        float epsilon    = Mathf.Sqrt(scaled_epsilon * other_lj.scaled_epsilon);
        float rinv       = 1.0f / dist_vec.magnitude;
        float r1s1       = sigma * rinv;
        float r3s3       = r1s1 * r1s1* r1s1;
        float r6s6       = r3s3 * r3s3;
        float r12s12     = r6s6 * r6s6;
        float derivative = 24.0f * epsilon * (r6s6 - 2.0f * r12s12) * rinv;
        m_Rigidbody.AddForce(derivative * rinv * dist_vec);
    }

    internal void Init(float radius, float epsilon, float timescale)
    {
        sphere_radius  = radius;
        scaled_epsilon = epsilon * timescale * timescale;
        float diameter = radius * 2.0f;
        transform.localScale = new Vector3(diameter, diameter, diameter);

        m_Rigidbody = GetComponent<Rigidbody>();
        m_SphereCollider = GetComponent<SphereCollider>();

        // Check no gravity apply to this particle
        Assert.IsFalse(m_Rigidbody.useGravity, "LJParticle should have false useGravity flag.");

        // This radius mean cutoff radius
        m_SphereCollider.radius = 2.5f * 0.5f; // relative value to Scale of Transform
        m_SphereCollider.isTrigger = true;
    }
}

} // Coral_iMD 
