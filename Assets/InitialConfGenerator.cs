using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Nett;

public class InitialConfGenerator : MonoBehaviour
{
    public GameObject m_GeneralParticle;

    private float temperature = 300.0f;
    private float timescale   = 10.0f;
    private float kb          = 0.0019827f; // kcal/mol,  1 tau .=. 49 fs
    private float kb_scaled;
    private NormalizedRandom m_NormalizedRandom;

    private SystemManager              m_SystemManager;
    private UnderdampedLangevinManager m_UnderdampedLangevinManager;
    private HarmonicBondManager        m_HarmonicBondManager;

    private void Awake()
    {
        // initialize member variables
        kb_scaled = kb * timescale * timescale;
    }
    // Start is called before the first frame update
    void Start()
    {
        // read input file
        string input_file_path = Application.dataPath + "/../input/input_test.toml";
        TomlTable root = Toml.ReadFile(input_file_path);

        // generate initial particle position, velocity and system temperature
        List<TomlTable> systems                = root.Get<List<TomlTable>>("systems");
        if (2 <= systems.Count)
        {
            throw new System.Exception(
                $"There are {systems.Count} systems. the multiple systems case is not supported.");
        }
        List<GameObject> general_particles = new List<GameObject>();
        Vector3 upper_boundary = new Vector3();
        Vector3 lower_boundary = new Vector3();
        m_NormalizedRandom = new NormalizedRandom();
        foreach (TomlTable system in systems)
        {
            temperature = system.Get<TomlTable>("attributes").Get<float>("temperature");
            if (system.ContainsKey("boundary_shape"))
            {
                TomlTable boundary_shape = system.Get<TomlTable>("boundary_shape");
                List<float> upper_bound_arr = boundary_shape.Get<List<float>>("upper");
                List<float> lower_bound_arr = boundary_shape.Get<List<float>>("lower");
                upper_boundary = new Vector3(upper_bound_arr[0], upper_bound_arr[1], upper_bound_arr[2]);
                lower_boundary = new Vector3(lower_bound_arr[0], lower_bound_arr[1], lower_bound_arr[2]);
            }
            else
            {
                throw new System.Exception(
                    "There is no boundary_shape information. UnlimitedBoundary is not supported now.");
            }
            List<TomlTable> particles = system.Get<List<TomlTable>>("particles");
            foreach (TomlTable particle_info in particles)
            {
                // initialize particle position
                float[] position = particle_info.Get<float[]>("pos");
                GameObject new_particle =
                    Instantiate(m_GeneralParticle,
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
                    new_rigid.velocity = new Vector3(m_NormalizedRandom.Generate() * sigma,
                                                     m_NormalizedRandom.Generate() * sigma,
                                                     m_NormalizedRandom.Generate() * sigma);
                }
                general_particles.Add(new_particle);
            }
        }
        Debug.Log("System initialization finished.");


        // read simulator information
        if (root.ContainsKey("simulator"))
        {
            TomlTable simulator = root.Get<TomlTable>("simulator");
            if (simulator.ContainsKey("integrator"))
            {
                TomlTable integrator = simulator.Get<TomlTable>("integrator");
                if (integrator.ContainsKey("type"))
                {
                    string integrator_type = integrator.Get<string>("type");
                    if (integrator_type == "UnderdampedLangevin")
                    {
                        if (integrator.ContainsKey("gammas"))
                        {
                            int general_particles_num = general_particles.Count;
                            List<TomlTable> gammas_tables = integrator.Get<List<TomlTable>>("gammas");
                            float[]         gammas        = new float[general_particles.Count];
                            foreach (TomlTable gamma_table in gammas_tables)
                            {
                                // TODO: check dupulicate and lacking of declaration.
                                gammas[gamma_table.Get<int>("index")] = gamma_table.Get<float>("gamma");
                            }
                            m_UnderdampedLangevinManager = GetComponent<UnderdampedLangevinManager>();
                            m_UnderdampedLangevinManager.Init(
                                kb_scaled, temperature, general_particles, gammas, timescale);
                            Debug.Log("UnderdampedLangevinManager initialization finished.");
                        }
                        else
                        {
                            throw new System.Exception(
                                "When you use UnderdampedLangevin integrator, you must specify gammas for integrator.");
                        }
                    }
                }
            }
        }

        // read forcefields information
        List<TomlTable> ffs        = root.Get<List<TomlTable>>("forcefields");
        float           max_radius = 0.0f;
        foreach (TomlTable ff in ffs)
        {
            if (ff.ContainsKey("local"))
            {
                List<TomlTable> local_ffs = ff.Get<List<TomlTable>>("local");
                foreach (TomlTable local_ff in local_ffs)
                {
                    string potential = local_ff.Get<string>("potential");
                    if (potential == "Harmonic")
                    {
                        var parameters = local_ff.Get<List<TomlTable>>("parameters");
                        var v0s = new List<float>();
                        var ks = new List<float>();
                        var rigid_pairs = new List<List<Rigidbody>>();
                        foreach (TomlTable parameter in parameters)
                        {
                            List<int> indices = parameter.Get<List<int>>("indices");
                            var rigid1 = general_particles[indices[0]].GetComponent<Rigidbody>();
                            var rigid2 = general_particles[indices[1]].GetComponent<Rigidbody>();
                            rigid_pairs.Add(new List<Rigidbody>() { rigid1, rigid2});
                            v0s.Add(parameter.Get<float>("v0"));
                            ks.Add(parameter.Get<float>("k"));
                            Assert.AreEqual(indices.Count, 2,
                                "The length of indices must be 2.");
                        }
                        m_HarmonicBondManager = GetComponent<HarmonicBondManager>();
                        m_HarmonicBondManager.Init(v0s, ks, rigid_pairs, timescale);
                        Debug.Log("HarmonicBondManager initialization finished.");
                    }
                    else
                    {
                        throw new System.Exception($@"
                        Unknown local forcefields is specified. Available local forcefield is
                            - Harmonic");
                    }
                }
            }

            if (ff.ContainsKey("global"))
            {
                List<TomlTable> global_ffs = ff.Get<List<TomlTable>>("global");
                foreach (TomlTable global_ff in global_ffs)
                {
                    Assert.AreEqual("LennardJones", global_ff.Get<string>("potential"),
                        "The potential field is only allowed \"LennardJones\". Other potential or null is here.");
                    List<TomlTable> parameters = global_ff.Get<List<TomlTable>>("parameters");
                    foreach (TomlTable parameter in parameters)
                    {
                        int index = parameter.Get<int>("index");
                        float sigma = parameter.Get<float>("sigma"); // sigma correspond to diameter.
                        float radius = sigma * 0.5f;
                        if (max_radius < radius)
                        {
                            max_radius = radius;
                        }
                        GameObject general_particle = general_particles[index];
                        var ljparticle
                            = general_particle.AddComponent(typeof(LennardJonesParticle)) as LennardJonesParticle;
                        ljparticle.sphere_radius = radius;
                        ljparticle.scaled_epsilon = parameter.Get<float>("epsilon") * timescale;
                        ljparticle.transform.localScale = new Vector3(sigma, sigma, sigma);
                    }
                }
            }
        }

        // Initialize SystemManager
        m_SystemManager = GetComponent<SystemManager>();
        m_SystemManager.Init(general_particles, upper_boundary, lower_boundary, timescale);
        Debug.Log("SystemManager initialization finished.");

        // Set floor position
        GameObject floor = GameObject.Find("Floor");
        floor.transform.position = new Vector3(0.0f, lower_boundary[1] - max_radius, 0.0f);

        // Set player position and scale
        GameObject player = GameObject.Find("OVRPlayerController");
        Vector3 box_length_half = upper_boundary - lower_boundary;
        player.transform.position   = box_length_half + lower_boundary;
        player.transform.localScale = new Vector3(box_length_half[1] * 0.7f,
                                                  box_length_half[1] * 0.7f,
                                                  box_length_half[1] * 0.7f);
    }
}
