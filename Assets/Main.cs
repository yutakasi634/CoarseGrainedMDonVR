using System;
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
    public GameObject m_BaseParticle;

    private float temperature = 300.0f;
    private float kb = 0.0019827f; // kcal/mol,  1 tau .=. 49 fs
    private float kb_scaled;

    private SystemObserver               m_SystemObserver;
    private ReflectingBoundaryManager    m_ReflectingBoundaryManager;

    private void Awake()
    {
        // initialize member variables
        kb_scaled = kb * timescale * timescale;
    }
    // Start is called before the first frame update
    void Start()
    {
        // read input file
        string input_file_path = Application.dataPath + "/../input/input.toml";
        Debug.Log($"input file path is {input_file_path}.");
        InputToml input = new InputToml(input_file_path);

        // generate initial particle position, velocity and system temperature
        TomlTable system = input.SystemTable;
        temperature = system.Get<TomlTable>("attributes").Get<float>("temperature");

        // read particles information
        List<GameObject> base_particles = input.GenerateBaseParticles(m_BaseParticle, kb_scaled);

        // read simulator information
        input.GenerateIntegratorManagers(gameObject, base_particles, kb_scaled, timescale);

        // read boundary_shape information
        input.GenerateBoundaryManager(gameObject, base_particles);

        // read forcefields information
        TomlTable ff = input.ForceFieldTable;
        if (ff.ContainsKey("local"))
        {
            input.GenerateLocalInteractionManagers(gameObject, base_particles, timescale);
        }

        if (ff.ContainsKey("global"))
        {
            input.GenerateGlobalInteractionManagers(base_particles, timescale);
        }
        Debug.Log("ForceField initialization finished.");

        // Initialize SystemObserver
        m_SystemObserver = GetComponent<SystemObserver>();
        m_SystemObserver.Init(base_particles, timescale);
        Debug.Log("SystemObserver initialization finished.");

        // set particle colors
        int particle_num = base_particles.Count;
        float color_step = 2.0f / (particle_num - 1);
        List<Color> color_list = new List<Color>();
        float color_val  = 0.0f;
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


        // Set Player position
        GameObject player = GameObject.Find("OVRPlayerController");

        float max_radius = 0.0f;
        if (gameObject.GetComponent<ReflectingBoundaryManager>() != null)
        {
            foreach (GameObject base_particle in base_particles)
            {
                float radius = base_particle.transform.localScale.x;
                if(max_radius < radius)
                {
                    max_radius = radius;
                }
            }

            var rb_manager =gameObject.GetComponent<ReflectingBoundaryManager>();
            Vector3 upper_boundary = rb_manager.UpperBoundary;
            Vector3 lower_boundary = rb_manager.LowerBoundary;
            Vector3 box_length_half = upper_boundary - lower_boundary;
            Vector3 box_center      = box_length_half + lower_boundary;
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
            Vector3 upper_boundary = upper_edge + pseudo_box_length_half;
            Vector3 lower_boundary = lower_edge - pseudo_box_length_half;
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
