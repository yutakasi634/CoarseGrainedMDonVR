using System;
using System.Collections.Generic;
using UnityEngine;
using Nett;

namespace Coral_iMD
{
    internal class InputToml
    {
        internal TomlTable SimulatorTable  { get; }
        internal TomlTable SystemTable     { get; }
        internal TomlTable ForceFieldTable { get; }

        private  NormalizedRandom Rng;

        internal InputToml(string input_file_path)
        {
            TomlTable root = Toml.ReadFile(input_file_path);

            SimulatorTable = root.Get<TomlTable>("simulator");

            List<TomlTable> systems = root.Get<List<TomlTable>>("systems");
            if (2 <= systems.Count)
            {
                throw new System.Exception(
                        $"There are {systems.Count} systems. the multiple systems case is not supported");
            }
            SystemTable = systems[0];

            List<TomlTable> forcefields = root.Get<List<TomlTable>>("forcefields");
            if (2 <= forcefields.Count)
            {
                throw new System.Exception(
                        $"There are {forcefields.Count} systems. the multiple systems case is not supported");
            }
            ForceFieldTable = forcefields[0];
        }

        // This method generate BaseParticle in system by side effect.
        internal List<GameObject> GenerateBaseParticles(GameObject base_particle, float kb_scaled)
        {
            var return_list = new List<GameObject>();

            var temperature = SystemTable.Get<TomlTable>("attributes").Get<float>("temperature");

            List<TomlTable> particles = SystemTable.Get<List<TomlTable>>("particles");
            foreach (TomlTable particle_info in particles)
            {
                var position = particle_info.Get<List<float>>("pos");
                GameObject new_particle =
                    GameObject.Instantiate(base_particle,
                                           new Vector3(position[0], position[1], position[2]),
                                           base_particle.transform.rotation);

                // initialize particle velocity
                Rigidbody new_rigid = new_particle.GetComponent<Rigidbody>();
                new_rigid.mass = particle_info.Get<float>("m");
                if (particle_info.ContainsKey("vel"))
                {
                    var velocity = particle_info.Get<List<float>>("vel");
                    new_rigid.velocity = new Vector3(velocity[0], velocity[1], velocity[2]);
                }
                else
                {
                    float sigma = Mathf.Sqrt(kb_scaled * temperature / new_rigid.mass);
                    new_rigid.velocity = new Vector3(Rng.Generate() * sigma,
                                                     Rng.Generate() * sigma,
                                                     Rng.Generate() * sigma);
                }
                return_list.Add(new_particle);
            }

            return return_list;
        }
    }

} // Coral_iMD
