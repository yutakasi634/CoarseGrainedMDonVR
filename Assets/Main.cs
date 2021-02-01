﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Nett;

namespace Coral_iMD
{
    using RigidPairType       = Tuple<Rigidbody, Rigidbody>;
    using RigidTripletType    = Tuple<Rigidbody, Rigidbody, Rigidbody>;
    using RigidQuadrupletType = Tuple<Rigidbody, Rigidbody, Rigidbody, Rigidbody>;

    public class Main : MonoBehaviour
    {
        public float timescale = 100.0f;
        public GameObject m_GeneralParticle;

        private float temperature = 300.0f;
        private float kb = 0.0019827f; // kcal/mol,  1 tau .=. 49 fs
        private float kb_scaled;
        private NormalizedRandom m_NormalizedRandom;

        private SystemObserver               m_SystemObserver;
        private ReflectingBoundaryManager    m_ReflectingBoundaryManager;
        private UnderdampedLangevinManager   m_UnderdampedLangevinManager;

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
            Debug.Log($"input file path is {input_file_path}.");
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
            List<GameObject> base_particles = new List<GameObject>();
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
                base_particles.Add(new_particle);
            }

            // set particle colors
            int particle_num = base_particles.Count;
            float color_step = 2.0f / (particle_num - 1);
            List<Color> color_list = new List<Color>();
            float color_val = 0.0f;
            color_list.Add(new Color(1.0f, 0.0f, 0.0f));
            foreach (GameObject base_particle in base_particles)
            {
                color_val += color_step;
                Material particle_material = base_particle.GetComponent<Renderer>().material;
                if (color_val < 1.0f)
                {
                    particle_material.SetColor("_Color", new Color(1.0f, color_val, color_val));
                }
                else
                {
                    particle_material.SetColor("_Color", new Color(2.0f-color_val, 2.0f-color_val, 1.0f));
                }
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
                m_ReflectingBoundaryManager.Init(base_particles, upper_boundary, lower_boundary);
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
                                int base_particles_num = base_particles.Count;
                                List<TomlTable> gammas_tables = integrator.Get<List<TomlTable>>("gammas");
                                float[] gammas = new float[base_particles.Count];
                                foreach (TomlTable gamma_table in gammas_tables)
                                {
                                    // TODO: check dupulicate and lacking of declaration.
                                    gammas[gamma_table.Get<int>("index")] = gamma_table.Get<float>("gamma");
                                }
                                m_UnderdampedLangevinManager = GetComponent<UnderdampedLangevinManager>();
                                m_UnderdampedLangevinManager.Init(
                                    kb_scaled, temperature, base_particles, gammas, timescale);
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
            List<TomlTable> ffs = root.Get<List<TomlTable>>("forcefields");
            float max_radius = 0.0f;
            foreach (TomlTable ff in ffs)
            {
                if (ff.ContainsKey("local"))
                {
                    List<TomlTable> local_ffs = ff.Get<List<TomlTable>>("local");
                    foreach (TomlTable local_ff in local_ffs)
                    {
                        string potential_str   = local_ff.Get<string>("potential");
                        string interaction = local_ff.Get<string>("interaction");
                        if (interaction == "BondLength")
                        {
                            var parameters  = local_ff.Get<List<TomlTable>>("parameters");
                            var pot_rigids_pairs = new List<Tuple<PotentialBase, RigidPairType>>();
                            if(potential_str == "Harmonic")
                            {
                                foreach (TomlTable parameter in parameters)
                                {
                                    var v0 = parameter.Get<float>("v0");
                                    var k  = parameter.Get<float>("k");
                                    var potential = new HarmonicPotential(v0, k, timescale);

                                    List<int> indices = parameter.Get<List<int>>("indices");

                                    Assert.AreEqual(indices.Count, 2,
                                        "The length of indices must be 2.");

                                    var rigid1 = base_particles[indices[0]].GetComponent<Rigidbody>();
                                    var rigid2 = base_particles[indices[1]].GetComponent<Rigidbody>();
                                    var rigid_pair = new RigidPairType(rigid1, rigid2);

                                    pot_rigids_pairs.Add(new Tuple<PotentialBase, RigidPairType>(potential, rigid_pair));
                                }
                            }
                            else if (potential_str == "GoContact")
                            {
                                foreach (TomlTable parameter in parameters)
                                {
                                    var v0 = parameter.Get<float>("v0");
                                    var k  = parameter.Get<float>("k");
                                    var potential = new GoContactPotential(v0, k, timescale);

                                    List<int> indices = parameter.Get<List<int>>("indices");

                                    Assert.AreEqual(indices.Count, 2,
                                        "The length of indices must be 2.");

                                    var rigid1 = base_particles[indices[0]].GetComponent<Rigidbody>();
                                    var rigid2 = base_particles[indices[1]].GetComponent<Rigidbody>();
                                    var rigid_pair = new RigidPairType(rigid1, rigid2);

                                    pot_rigids_pairs.Add(new Tuple<PotentialBase, RigidPairType>(potential, rigid_pair));
                                }
                            }
                            BondLengthInteractionManager bli_manager
                                =  gameObject.AddComponent<BondLengthInteractionManager>() as BondLengthInteractionManager;
                            bli_manager.Init(pot_rigids_pairs);
                            string potential_name = bli_manager.PotentialName();
                            Debug.Log($"BondLengthInteraction with {potential_name} initialization finished.");
                        }
                        else if (interaction == "BondAngle")
                        {
                            var parameters = local_ff.Get<List<TomlTable>>("parameters");
                            var pot_rigids_pairs = new List<Tuple<PotentialBase, RigidTripletType>>();
                            if(potential_str == "Harmonic")
                            {
                                foreach (TomlTable parameter in parameters)
                                {
                                    var v0 = parameter.Get<float>("v0");
                                    var k  = parameter.Get<float>("k");
                                    var potential = new HarmonicPotential(v0, k, timescale);

                                    List<int> indices = parameter.Get<List<int>>("indices");

                                    Assert.AreEqual(indices.Count, 3,
                                        "The length of indices must be 3.");

                                    var rigid_i = base_particles[indices[0]].GetComponent<Rigidbody>();
                                    var rigid_j = base_particles[indices[1]].GetComponent<Rigidbody>();
                                    var rigid_k = base_particles[indices[2]].GetComponent<Rigidbody>();
                                    var rigid_triplets = new RigidTripletType(rigid_i, rigid_j, rigid_k);

                                    pot_rigids_pairs.Add(new Tuple<PotentialBase, RigidTripletType>(potential, rigid_triplets));
                                }
                            }
                            BondAngleInteractionManager bai_manager =
                                gameObject.AddComponent<BondAngleInteractionManager>() as BondAngleInteractionManager;
                            bai_manager.Init(pot_rigids_pairs);
                            string potential_name = bai_manager.PotentialName();
                            Debug.Log($"BondAngleInteraciton with {potential_name} initialization finished.");
                        }
                        else if (interaction == "DihedralAngle")
                        {
                            var parameters       = local_ff.Get<List<TomlTable>>("parameters");
                            var pot_rigids_pairs = new List<Tuple<PotentialBase, RigidQuadrupletType>>();
                            if(potential_str == "ClementiDihedral")
                            {
                                foreach (TomlTable parameter in parameters)
                                {
                                    var v0 = parameter.Get<float>("v0");
                                    var k1 = parameter.Get<float>("k1");
                                    var k3 = parameter.Get<float>("k3");
                                    var potential = new ClementiDihedralPotential(v0, k1, k3, timescale);

                                    List<int> indices = parameter.Get<List<int>>("indices");
                                    Assert.AreEqual(indices.Count, 4,
                                            "The length of indices must be 4.");

                                    var rigid_i = base_particles[indices[0]].GetComponent<Rigidbody>();
                                    var rigid_j = base_particles[indices[1]].GetComponent<Rigidbody>();
                                    var rigid_k = base_particles[indices[2]].GetComponent<Rigidbody>();
                                    var rigid_l = base_particles[indices[3]].GetComponent<Rigidbody>();
                                    var rigid_quadruplet = new RigidQuadrupletType(rigid_i, rigid_j, rigid_k, rigid_l);

                                    pot_rigids_pairs.Add(
                                            new Tuple<PotentialBase, RigidQuadrupletType>(potential, rigid_quadruplet));
                                }
                            }
                            DihedralAngleInteractionManager dai_manager =
                                gameObject.AddComponent<DihedralAngleInteractionManager>() as DihedralAngleInteractionManager;
                            dai_manager.Init(pot_rigids_pairs);
                            string potential_name = dai_manager.PotentialName();
                            Debug.Log($"DihedralAngleInteraction with {potential_name} initialization finished");
                        }
                        else
                        {
                            Debug.LogWarning($@"
Unknown combination of local interaction {interaction} and forcefields {potential_str} is specified.
This table will be ignored.
Available combination is
    - Interaction: BondLength,    Potential: Harmonic
    - Interaction: BondLength,    Potential: GoContact
    - Interaction: BondAngle,     Potential: Harmonic
    - Interaction: DihedralAngle, Potential: ClementiDihedral");
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
                                GameObject base_particle = base_particles[index];
                                var ljparticle
                                    = base_particle.AddComponent(typeof(LennardJonesParticle)) as LennardJonesParticle;
                                ljparticle.Init(radius, parameter.Get<float>("epsilon"), timescale);
                            }
                            Debug.Log("LennardJones initialization finished.");
                        }
                        else if (potential == "ExcludedVolume")
                        {
                            foreach (TomlTable parameter in parameters)
                            {
                                int index = parameter.Get<int>("index");
                                float radius = parameter.Get<float>("radius");
                                if (max_radius < radius)
                                {
                                    max_radius = radius;
                                }
                                GameObject base_particle = base_particles[index];
                                var exvparticle
                                    = base_particle.AddComponent(typeof(ExcludedVolumeParticle)) as ExcludedVolumeParticle;
                                exvparticle.sphere_radius = radius;
                                exvparticle.Init(radius, global_ff.Get<float>("epsilon"), timescale);
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
            m_SystemObserver.Init(base_particles, timescale);
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
                Vector3 upper_edge = detect_upper_edge(base_particles);
                Vector3 lower_edge = detect_lower_edge(base_particles);
                Vector3 pseudo_box_center      = (upper_edge + lower_edge) * 0.5f;
                Vector3 pseudo_box_length_half = (upper_edge - lower_edge) * 0.5f;
                upper_boundary = upper_edge + pseudo_box_length_half;
                lower_boundary = lower_edge - pseudo_box_length_half;
                floor.transform.position    = new Vector3(pseudo_box_center.x,
                                                        lower_boundary.y,
                                                        pseudo_box_center.z);
                player.transform.position   = new Vector3(pseudo_box_center.x,
                                                        upper_boundary.y,
                                                        lower_boundary.z - pseudo_box_length_half.z);
                player.transform.localScale = new Vector3(upper_boundary.y,
                                                          upper_boundary.y,
                                                          upper_boundary.y);
            }
        }

        private Vector3 detect_upper_edge(List<GameObject> base_particles)
        {
            var ret_vec = new Vector3(0.0f, 0.0f, 0.0f);
            foreach (GameObject base_part in base_particles)
            {
                Vector3 coord = base_part.GetComponent<Rigidbody>().position;
                for(int idx = 0; idx < 3; idx++)
                {
                    if (ret_vec[idx] < coord[idx]) { ret_vec[idx] = coord[idx]; }
                }
            }
            return ret_vec;
        }

        private Vector3 detect_lower_edge(List<GameObject> base_particles)
        {
            var ret_vec = new Vector3(0.0f, 0.0f, 0.0f);
            foreach (GameObject base_part in base_particles)
            {
                Vector3 coord = base_part.GetComponent<Rigidbody>().position;
                for (int idx = 0; idx < 3; idx++)
                {
                    if (coord[idx] < ret_vec[idx]) { ret_vec[idx] = coord[idx]; }
                }
            }
            return ret_vec;
        }
    }

} // Coral_iMD
