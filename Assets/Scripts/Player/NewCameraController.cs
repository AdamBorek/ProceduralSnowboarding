using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewCameraController : MonoBehaviour
{
    public Transform target; // The object to rotate around
    public float distance = 5.0f; // Distance from the target
    public float heightOffset = 1.0f; // Height offset above the target
    public float rotationSpeed = 2.0f; // Speed of rotation

    private float currentRotationX = 0.0f;
    private float currentRotationY = 0.0f;

    // Update is called once per frame
    void Update()
    {
        currentRotationX += Input.GetAxis("Mouse X") * rotationSpeed;
        currentRotationY -= Input.GetAxis("Mouse Y") * rotationSpeed;
        currentRotationY = Mathf.Clamp(currentRotationY, -80, 80); // Limit vertical rotation

        // Calculate the rotation quaternion
        Quaternion rotation = Quaternion.Euler(currentRotationY, currentRotationX, 0);

        // Calculate the desired camera position based on rotation and distance
        Vector3 offset = new Vector3(0, heightOffset, 0);
        Vector3 desiredPosition = target.position + offset - (rotation * Vector3.forward) * distance;

        // Apply the calculated rotation and position to the camera
        transform.rotation = rotation;
        transform.position = desiredPosition;
    }
}
