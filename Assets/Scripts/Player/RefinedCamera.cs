using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RefinedCamera : MonoBehaviour
{
    [SerializeField] float minFOV;
    [SerializeField] float maxFOV;
    float currentFOV;

    [SerializeField] float minDistance;
    [SerializeField] float maxDistance;
    float currentDistance;

    [SerializeField] GameObject player;
    PlayerController controller;

    Vector3 playerToCamera;

    // Start is called before the first frame update
    void Start()
    {
        controller = player.GetComponent<PlayerController>();
        playerToCamera = new Vector3(0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        playerToCamera = -controller.currentVelocity;

        Debug.Log("player to camera: " + playerToCamera);

        transform.position = player.transform.position + playerToCamera;

        transform.LookAt(player.transform);
    }
}
