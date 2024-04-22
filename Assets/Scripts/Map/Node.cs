using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public int nodeNumber;

    public Vector3 position;
    public int x;
    public int y;
    public Vector3[] corners;
    public Vector3 normal;
    public bool wall;
    public bool path;
    public float estimatedSpeed;

    public GameObject board;

    public float actualSpeed;

    bool speedUpdated;

    public BoxCollider collider;

    public void Start()
    {
        collider = gameObject.GetComponent<BoxCollider>();
        speedUpdated = false;
    }

    public void Update()
    {
        if (collider.bounds.Contains(board.transform.position) && !speedUpdated)
        {
            actualSpeed = board.GetComponent<NewSnowboardController>().currentLinearSpeed;
            speedUpdated = true;
        }
    }
}
