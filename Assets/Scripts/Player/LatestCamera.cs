using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LatestCamera : MonoBehaviour
{
    public GameObject target; // The object to follow (your rigidbody object)
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 5.0f;

    [SerializeField] Vector3 offset;
    public Vector3 grindOffset;
    [SerializeField] Vector3 lookAtOffset;
    [SerializeField] float positionDistanceMultiplier;
    [SerializeField] float lookDistanceMultiplier;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target not assigned to VelocityBasedCamera!");
            enabled = false;
        }

        // Calculate the initial offset between the camera and the target
        //offset = transform.position - target.transform.position;

        transform.LookAt(target.transform.position);
    }

    private void Update()
    {


        // Calculate the current velocity vector of the target object
        Vector3 currentVelocity = target.GetComponent<Rigidbody>().velocity;

        // Calculate the rotation based on the velocity vector
        //Vector3 desiredPos = target.transform.position + offset - currentVelocity * positionDistanceMultiplier;
        Vector3 desiredPos = new Vector3();

        if (!target.GetComponent<Rigidbody>().isKinematic)
        {
            desiredPos = target.transform.position + offset - currentVelocity * positionDistanceMultiplier;
        }
        else
        {
            desiredPos = target.transform.position + grindOffset + new Vector3(0,5,0) - (currentVelocity * positionDistanceMultiplier);
        }

        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * moveSpeed);

        //transform.LookAt(target.transform.position);

        Vector3 desiredLookAt = target.transform.position + currentVelocity * lookDistanceMultiplier + lookAtOffset;

        //transform.LookAt(target.transform.position + currentVelocity * lookDistanceMultiplier + lookAtOffset);
        Quaternion desiredRotation = Quaternion.LookRotation(desiredLookAt - transform.position);

        // Smoothly rotate the camera towards the desired rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSpeed);

        if (!target.GetComponent<Rigidbody>().isKinematic)
        {
        }
        else
        {
            //// If the target's Rigidbody is kinematic, just follow the target closely without rotation
            //Vector3 desiredPos = target.transform.position - target.GetComponent<NewSnowboardController>().currentGrindSpline.gameObject.transform.forward.normalized * 10 + offset;
            //transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * moveSpeed);

            //// Look at the target
            //transform.LookAt(target.transform.position);

            //transform.position = target.transform.position + grindOffset;
        }
    }
}
