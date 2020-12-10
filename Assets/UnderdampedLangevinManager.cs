using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class UnderdampedLangevinManager : MonoBehaviour
{
    private List<float>      m_NoiseCoefs;
    private List<float>      m_Gammas;
    private List<Rigidbody>  m_LJRigidbodies;
    private NormalizedRandom m_NormalizedRandom;

    private void Start()
    {
        m_NormalizedRandom = new NormalizedRandom();
    }   

    private void FixedUpdate()
    {
        for (int part_idx = 1; part_idx <= m_LJRigidbodies.Count; part_idx++)
        {
            Rigidbody ljrigid = m_LJRigidbodies[part_idx];
            Vector3 vel   = ljrigid.velocity;
            Vector3 accelerate   = -m_Gammas[part_idx] * vel;
            Vector3 random_force = new Vector3(m_NormalizedRandom.Generate(),
                                               m_NormalizedRandom.Generate(),
                                               m_NormalizedRandom.Generate());
            accelerate += m_NoiseCoefs[part_idx] * random_force;
            ljrigid.AddForce(accelerate, ForceMode.Acceleration);
        }
    }

    internal void Init (float kb, float temperature,
        List<LennardJonesParticle> ljparticles, List<float> gammas)
    {
        m_Gammas = gammas;

        int particles_num = ljparticles.Count;
        m_LJRigidbodies = new List<Rigidbody>();
        m_NoiseCoefs    = new List<float>();
        for (int part_idx = 1; part_idx <= particles_num; part_idx++)
        {
            Rigidbody ljrigid = ljparticles[part_idx].GetComponent<Rigidbody>();
            float noise_coef 
                = Mathf.Sqrt(2.0f * m_Gammas[part_idx] * kb * temperature / ljrigid.mass);
            m_LJRigidbodies.Add(ljrigid);
            m_NoiseCoefs.Add(noise_coef);
        }

        Assert.AreEqual(m_LJRigidbodies.Count, m_Gammas.Count,
            "The number of gamma should equal to that of lennard-jones particles.");
    }
}
