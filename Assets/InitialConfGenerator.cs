﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Nett;

public class InitialConfGenerator : MonoBehaviour
{
    public float box_size           = 5.0f;

    public LennardJonesParticle m_LJParticle;

    private float temperature = 300.0f;
    private float kb = 0.8317e-4f; // mass:Da, length:Å, time:0.01ps
    private NormalizedRandom m_NormalizedRandom;
    private SystemManager    m_SystemManager;

    // Start is called before the first frame update
    void Start()
    {
        // read input file
        string input_file_path = Application.dataPath + "/../input/lennard-jones.toml";
        TomlTable root = Toml.ReadFile(input_file_path);

        // generate initial particle position, velocity and system temperature
        List<TomlTable> systems                = root.Get<List<TomlTable>>("systems");
        List<LennardJonesParticle> ljparticles = new List<LennardJonesParticle>();
        m_NormalizedRandom                     = new NormalizedRandom();
        foreach (TomlTable system in systems)
        {
            temperature = system.Get<TomlTable>("attributes").Get<float>("temperature");
            List<TomlTable> particles = system.Get<List<TomlTable>>("particles");
            foreach (TomlTable particle_info in particles)
            {
                // initialize particle position
                float[] position = particle_info.Get<float[]>("pos");
                LennardJonesParticle new_particle =
                    Instantiate(m_LJParticle,
                                new Vector3(position[0], position[1], position[2]),
                                transform.rotation);

                // initialize particle velocity
                Rigidbody new_rigid = new_particle.GetComponent<Rigidbody>();
                new_rigid.mass = particle_info.Get<float>("m");
                if (particle_info.ContainsKey("vel"))
                {
                    float[] velocity = particle_info.Get<float[]>("vel");
                    new_rigid.velocity = new Vector3(velocity[0], velocity[1], velocity[2]);
                }
                else
                {
                    float sigma = Mathf.Sqrt(kb * temperature / new_rigid.mass);
                    new_rigid.velocity = new Vector3(m_NormalizedRandom.Generate(0.0f, sigma),
                                                     m_NormalizedRandom.Generate(0.0f, sigma),
                                                     m_NormalizedRandom.Generate(0.0f, sigma));
                }
                ljparticles.Add(new_particle);
            }
        }
        Debug.Log("System initialization finished.");

        // Initialize SystemManager
        m_SystemManager = GetComponent<SystemManager>();
        m_SystemManager.Init(ljparticles, box_size);
        Debug.Log("SystemManager initialization finished.");
    }
}
