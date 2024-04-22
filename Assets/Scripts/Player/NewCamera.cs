using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewCamera : MonoBehaviour
{
    [SerializeField] Rigidbody playerRigidbody;
    [SerializeField] Transform lookAtPoint;
    [SerializeField] Vector3 initialOffset;

    [SerializeField] float cameraDistanceMultiplier = 2.0f; // Initial camera distance
    [SerializeField] float movementFactor = 0.01f; // Adjust this to control camera responsiveness to movement

    Vector3 desiredLookAtPoint;
    Vector3 currentLookAtPoint;

    void Start()
    {
        // Store the initial offset between the camera and the player
        //initialOffset = transform.position - playerRigidbody.position;
    }

    void Update()
    {
        // Calculate the camera's target position based on player's velocity
        Vector3 targetPosition = playerRigidbody.position + initialOffset + (-playerRigidbody.velocity * cameraDistanceMultiplier);

        Vector3 directionVer = targetPosition - transform.position;

        // Smoothly move the camera towards the target position
        //transform.position = Vector3.Lerp(transform.position, targetPosition, movementFactor);
        transform.position += directionVer * Time.deltaTime * movementFactor;
        //transform.position += directionVer;

        // Look at the lookAtPoint (just above the player)
        desiredLookAtPoint = playerRigidbody.transform.position + playerRigidbody.velocity;
        transform.LookAt(desiredLookAtPoint);
    }

}
