using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Coral_iMD
{

public class UnderdampedLangevinManager : MonoBehaviour
{
    private List<float>      m_NoiseCoefs;
    private List<float>      m_ScaledGammas;
    private List<Rigidbody>  m_LJRigidbodies;
    private NormalizedRandom m_NormalizedRandom;

    private void Awake()
    {
        enabled = false;
    }

    private void FixedUpdate()
    {
        for (int part_idx = 0; part_idx < m_LJRigidbodies.Count; part_idx++)
        {
            Rigidbody ljrigid = m_LJRigidbodies[part_idx];
            Vector3 accelerate   = -m_ScaledGammas[part_idx] * ljrigid.velocity;
            Vector3 random_force = new Vector3(m_NormalizedRandom.Generate(),
                                               m_NormalizedRandom.Generate(),
                                               m_NormalizedRandom.Generate());
            accelerate += m_NoiseCoefs[part_idx] * random_force;
            ljrigid.AddForce(accelerate, ForceMode.Acceleration);
        }
    }

    internal void Init(float kb_scaled, float temperature,
        List<GameObject> general_particles, float[] gammas, float timescale)
    {
        enabled = true;
        m_NormalizedRandom = new NormalizedRandom();
        m_ScaledGammas = new List<float>();
        foreach (float gamma in gammas)
        {
            m_ScaledGammas.Add(gamma * timescale);
        }

        int particles_num = general_particles.Count;
        m_LJRigidbodies = new List<Rigidbody>();
        m_NoiseCoefs    = new List<float>();
        float invdt        = 1.0f / Time.fixedDeltaTime;
        for (int part_idx = 0; part_idx < particles_num; part_idx++)
        {
            Rigidbody ljrigid = general_particles[part_idx].GetComponent<Rigidbody>();
            float noise_coef 
                = Mathf.Sqrt(2.0f * m_ScaledGammas[part_idx] * kb_scaled * temperature * invdt / ljrigid.mass);
            m_LJRigidbodies.Add(ljrigid);
            m_NoiseCoefs.Add(noise_coef);
        }

        Assert.AreEqual(m_LJRigidbodies.Count, m_ScaledGammas.Count,
            "The number of gamma should equal to that of lennard-jones particles.");
    }
}

} // Coral_iMD
