using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OribitngCameraScript : MonoBehaviour
{
    public Transform target; // The object to orbit around
    public float orbitSpeed = 5f; // Speed of the orbit
    public float orbitRadius = 2f; // Radius of the orbit
    public float height = 700f;

    void Update()
    {
        // Ensure there is a target assigned
        if (target == null)
        {
            Debug.LogError("Target not assigned for orbiting.");
            return;
        }

        // Calculate the desired position in a circular orbit
        float orbitAngle = Time.time * orbitSpeed;
        float x = Mathf.Cos(orbitAngle) * orbitRadius;
        float z = Mathf.Sin(orbitAngle) * orbitRadius;

        // Set the object's position relative to the target
        Vector3 orbitPosition = new Vector3(x, height, z);
        transform.position = target.position + orbitPosition;

        // Make the object look at the target to face it while orbiting
        transform.LookAt(target);
    }
}
