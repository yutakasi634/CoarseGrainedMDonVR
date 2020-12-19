using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SystemManager : MonoBehaviour
{
    private Vector3 m_UpperBoundary;
    private Vector3 m_LowerBoundary;
    private List<GameObject> m_GeneralParticles;

    private float invtimescale;
    private float kinetic_ene;

    private void Awake()
    {
        InvokeRepeating("UpdateKineticEnergy", 1.0f, 1.0f);
    }

    private void FixedUpdate()
    {
        foreach (GameObject lj_part in m_GeneralParticles)
        {
            // fix by Reflecting Boundary Condition
            Rigidbody lj_rigid = lj_part.GetComponent<Rigidbody>();
            Vector3 currentPos = lj_rigid.position;
            Vector3 currentVel = lj_rigid.velocity;
            if (currentPos.x < m_LowerBoundary.x || m_UpperBoundary.x < currentPos.x)
            {
                currentVel.x = -currentVel.x;
            }
            if (currentPos.y < m_LowerBoundary.y || m_UpperBoundary.y < currentPos.y)
            {
                currentVel.y = -currentVel.y;
            }
            if (currentPos.z < m_LowerBoundary.z || m_UpperBoundary.z < currentPos.z)
            {
                currentVel.z = -currentVel.z;
            }
            lj_rigid.velocity = currentVel;
        }
    }

    internal void Init(List<GameObject> general_particles,
        Vector3 upper_boundary, Vector3 lower_boundary, float timescale)
    {
        invtimescale  = 1 / timescale;
        m_UpperBoundary = upper_boundary;
        m_LowerBoundary = lower_boundary;
        m_GeneralParticles = general_particles;
        UpdateKineticEnergy();
    }

    internal void UpdateKineticEnergy()
    {
        kinetic_ene = 0.0f;
        foreach (GameObject gen_part in m_GeneralParticles)
        {
            Rigidbody gen_Rigidbody = gen_part.GetComponent<Rigidbody>();
            kinetic_ene +=
                Mathf.Pow(gen_Rigidbody.velocity.magnitude * invtimescale, 2.0f) * gen_Rigidbody.mass;
        }
        kinetic_ene *= 0.5f;
        Debug.Log($"Kinetic energy is {kinetic_ene} kcal/mol.");
    }
}
