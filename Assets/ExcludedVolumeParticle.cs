using System;
using UnityEngine;
using UnityEngine.Assertions;

public class ExcludedVolumeParticle : MonoBehaviour
{
    public float sphere_radius  = 0.5f;
    public float scaled_epsilon = 0.05f;

    private Rigidbody      m_Rigidbody;
    private SphereCollider m_SphereCollider;

    private void OnTriggerStay(Collider other)
    {
        ExcludedVolumeParticle other_exv = other.GetComponent<ExcludedVolumeParticle>();
        if (other_exv == null)
        {
            return;
        }

        Vector3 dist_vec = other.attachedRigidbody.position - transform.position;
        float sigma   = sphere_radius + other_exv.sphere_radius;
        float epsilon = Mathf.Sqrt(scaled_epsilon * other_exv.scaled_epsilon);
        float rinv    = 1.0f / dist_vec.magnitude;
        float d_r     = sigma * rinv;
        float dr3     = d_r * d_r * d_r;
        float dr6     = dr3 * dr3;
        float dr12    = dr6 * dr6;
        m_Rigidbody.AddForce(-12.0f * epsilon * dr12 * rinv * rinv * dist_vec);
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
        Assert.IsFalse(m_Rigidbody.useGravity, "ExvParticle should have false userGravity flag.");

        // This radius mean cutoff radius
        m_SphereCollider.radius = 2.0f * sphere_radius;
        m_SphereCollider.isTrigger = true;
    }
}
