using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Nett;

namespace Coral_iMD
{

using RigidPairType       = Tuple<Rigidbody, Rigidbody>;
using RigidTripletType    = Tuple<Rigidbody, Rigidbody, Rigidbody>;
using RigidQuadrupletType = Tuple<Rigidbody, Rigidbody, Rigidbody, Rigidbody>;

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

    // This method generate InteractionManager for local of SceneBuilder by side effect.
    internal void GenerateLocalInteractionManagers(
            GameObject scene_builder, List<GameObject> base_particles, float timescale)
    {
        List<TomlTable> local_ffs = ForceFieldTable.Get<List<TomlTable>>("local");
        foreach (TomlTable local_ff in local_ffs)
        {
            string interaction   = local_ff.Get<string>("interaction");
            string potential_str = local_ff.Get<string>("potential");
            if (interaction == "BondLength")
            {
                var parameters       = local_ff.Get<List<TomlTable>>("parameters");
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
                    = scene_builder.AddComponent<BondLengthInteractionManager>() as BondLengthInteractionManager;
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
                    scene_builder.AddComponent<BondAngleInteractionManager>() as BondAngleInteractionManager;
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
                    scene_builder.AddComponent<DihedralAngleInteractionManager>() as DihedralAngleInteractionManager;
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
}

} // Coral_iMD
