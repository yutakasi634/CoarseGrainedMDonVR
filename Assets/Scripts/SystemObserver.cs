using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Coral_iMD
{

public class SystemObserver : MonoBehaviour
{
    private float invtimescale;
    private float kinetic_ene;
    private List<GameObject> m_GeneralParticles;

    private void Awake()
    {
        InvokeRepeating("UpdateKineticEnergy", 1.0f, 1.0f);
    }

    internal void Init(List<GameObject> general_particles, float timescale)
    {
        invtimescale  = 1 / timescale;
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

} // Coral_iMD
