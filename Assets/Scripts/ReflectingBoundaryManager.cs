using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Coral_iMD
{

public class ReflectingBoundaryManager : MonoBehaviour
{
    private Vector3 m_UpperBoundary;
    private Vector3 m_LowerBoundary;
    private List<Rigidbody> m_Rigidbodies;

    private void Awake()
    {
        enabled = false;
    }

    private void FixedUpdate()
    {
        foreach (Rigidbody rigid in m_Rigidbodies)
        {
            // fix by Reflecting Boundary Condition
            Vector3 currentPos = rigid.position;
            Vector3 currentVel = rigid.velocity;
            if (currentPos.x < m_LowerBoundary.x || m_UpperBoundary.x < currentPos.x)
            {
                currentVel.x = -currentVel.x;
            }
            if (currentPos.y < m_LowerBoundary.y || m_UpperBoundary.y < currentPos.y)
            {
                currentVel.y = -currentVel.y;
            }
            if (currentPos.z < m_LowerBoundary.z || m_UpperBoundary.z < currentPos.z)
            {
                currentVel.z = -currentVel.z;
            }
            rigid.velocity = currentVel;
        }
    }

    internal void Init(List<GameObject> general_particles, 
        Vector3 upper_boundary, Vector3 lower_boundary)
    {
        enabled = true;
        m_UpperBoundary = upper_boundary;
        m_LowerBoundary = lower_boundary;
        m_Rigidbodies   = new List<Rigidbody>();
        foreach(GameObject gen_part in general_particles)
        {
            m_Rigidbodies.Add(gen_part.GetComponent<Rigidbody>());
        }
    }
}

} // Coral_iMD
