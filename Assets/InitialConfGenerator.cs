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

    private SystemObserver             m_SystemObserver;
    private ReflectingBoundaryManager  m_ReflectingBoundaryManager;
    private UnderdampedLangevinManager m_UnderdampedLangevinManager;
    private HarmonicBondManager        m_HarmonicBondManager;

    private void Awake()
    {
        // initialize member variables
        kb_scaled = kb * timescale * timescale;
        m_NormalizedRandom = new NormalizedRandom();
    }
    // Start is called before the first frame update
    void Start()
    {
        // read input file
        string input_file_path = Application.dataPath + "/../input/input.toml";
        TomlTable root = Toml.ReadFile(input_file_path);

        // generate initial particle position, velocity and system temperature
        List<TomlTable> systems = root.Get<List<TomlTable>>("systems");
        if (2 <= systems.Count)
        {
            throw new System.Exception(
                $"There are {systems.Count} systems. the multiple systems case is not supported.");
        }

        TomlTable system = systems[0];
        temperature = system.Get<TomlTable>("attributes").Get<float>("temperature");

        // read particles information
        List<TomlTable> particles = system.Get<List<TomlTable>>("particles");
        List<GameObject> general_particles = new List<GameObject>();
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

        // read boundary_shape information
        // if there is no boundary_shape key in system table, Unlimitedboundary will be select.
        Vector3 upper_boundary = new Vector3();
        Vector3 lower_boundary = new Vector3();
        if (system.ContainsKey("boundary_shape"))
        {
            TomlTable boundary_shape = system.Get<TomlTable>("boundary_shape");
            List<float> upper_bound_arr = boundary_shape.Get<List<float>>("upper");
            List<float> lower_bound_arr = boundary_shape.Get<List<float>>("lower");
            upper_boundary = new Vector3(upper_bound_arr[0], upper_bound_arr[1], upper_bound_arr[2]);
            lower_boundary = new Vector3(lower_bound_arr[0], lower_bound_arr[1], lower_bound_arr[2]);
            m_ReflectingBoundaryManager = GetComponent<ReflectingBoundaryManager>();
            m_ReflectingBoundaryManager.Init(general_particles, upper_boundary, lower_boundary);
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
                            rigid_pairs.Add(new List<Rigidbody>() { rigid1, rigid2 });
                            v0s.Add(parameter.Get<float>("v0"));
                            ks.Add(parameter.Get<float>("k"));
                            Assert.AreEqual(indices.Count, 2,
                                "The length of indices must be 2.");
                        }
                        m_HarmonicBondManager = GetComponent<HarmonicBondManager>();
                        m_HarmonicBondManager.Init(v0s, ks, rigid_pairs, timescale);
                        Debug.Log("HarmonicBondManager initialization finished.");
                    }
                    else if (potential == "GoContact")
                    {
                        Debug.Log("Now implementing ...");
                    }
                    else
                    {
                        throw new System.Exception($@"
                        Unknown local forcefields is specified. Available local forcefield is
                            - Harmonic
                            - GoContact
                        ");
                    }
                }
            }

            if (ff.ContainsKey("global"))
            {
                List<TomlTable> global_ffs = ff.Get<List<TomlTable>>("global");
                foreach (TomlTable global_ff in global_ffs)
                {
                    string potential = global_ff.Get<string>("potential");
                    List<TomlTable> parameters = global_ff.Get<List<TomlTable>>("parameters");
                    if (potential == "LennardJones")
                    {
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
                            ljparticle.Init(radius, parameter.Get<float>("epsilon"), timescale);
                        }
                        Debug.Log("LennardJones initialization finished.");
                    }
                    else if (potential == "ExcludedVolume")
                    {
                        foreach (TomlTable parameter in parameters)
                        {
                            int index    = parameter.Get<int>("index");
                            float radius = parameter.Get<float>("radius");
                            if (max_radius < radius)
                            {
                                max_radius = radius;
                            }
                            GameObject general_particle = general_particles[index];
                            var exvparticle
                                = general_particle.AddComponent(typeof(ExcludedVolumeParticle)) as ExcludedVolumeParticle;
                            exvparticle.sphere_radius = radius;
                            exvparticle.Init(radius , global_ff.Get<float>("epsilon"), timescale);
                        }
                        Debug.Log("ExcludedVolume initialization finished.");
                    }
                    else
                    {
                        throw new System.Exception($@"
                        Unknown global forcefields is specified. Available global forcefield is
                            - LennardJones
                            - ExcludedVolume
                        ");
                    }
                }
            }
        }

        // Initialize SystemObserver
        m_SystemObserver = GetComponent<SystemObserver>();
        m_SystemObserver.Init(general_particles, timescale);
        Debug.Log("SystemObserver initialization finished.");

        // Set Floor and Player position
        GameObject floor = GameObject.Find("Floor");
        GameObject player = GameObject.Find("OVRPlayerController");
        if (system.ContainsKey("boundary_shape"))
        {
            Vector3 box_length_half = upper_boundary - lower_boundary;
            Vector3 box_center      = box_length_half + lower_boundary;
            floor.transform.position    = new Vector3(box_center.x,
                                                      lower_boundary.y - max_radius,
                                                      box_center.z);
            player.transform.position   = new Vector3(box_center.x,
                                                      box_center.y,
                                                      lower_boundary.z - box_length_half.z);
        }
        else
        {
            Vector3 upper_edge = detect_upper_edge(general_particles);
            Vector3 lower_edge = detect_lower_edge(general_particles);
            Vector3 pseudo_box_center      = (upper_edge + lower_edge) * 0.5f;
            Vector3 pseudo_box_length_half = (upper_edge - lower_edge) * 0.5f;
            upper_boundary = upper_edge + pseudo_box_length_half;
            lower_boundary = lower_edge - pseudo_box_length_half;
            floor.transform.position  = new Vector3(pseudo_box_center.x,
                                                    lower_boundary.y,
                                                    pseudo_box_center.z);
            player.transform.position = new Vector3(pseudo_box_center.x,
                                                    pseudo_box_center.y,
                                                    lower_boundary.z - pseudo_box_length_half.z);
        }
    }

    private Vector3 detect_upper_edge(List<GameObject> general_particles)
    {
        var ret_vec = new Vector3(0.0f, 0.0f, 0.0f);
        foreach (GameObject gen_part in general_particles)
        {
            Vector3 coord = gen_part.GetComponent<Rigidbody>().position;
            for(int idx = 0; idx < 3; idx++)
            {
                if (ret_vec[idx] < coord[idx]) { ret_vec[idx] = coord[idx]; }
            }
        }
        return ret_vec;
    }

    private Vector3 detect_lower_edge(List<GameObject> general_particles)
    {
        var ret_vec = new Vector3(0.0f, 0.0f, 0.0f);
        foreach (GameObject gen_part in general_particles)
        {
            Vector3 coord = gen_part.GetComponent<Rigidbody>().position;
            for (int idx = 0; idx < 3; idx++)
            {
                if (coord[idx] < ret_vec[idx]) { ret_vec[idx] = coord[idx]; }
            }
        }
        return ret_vec;
    }
}