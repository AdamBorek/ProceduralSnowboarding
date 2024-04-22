using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishLineScript : MonoBehaviour
{
    GameStateManager stateManager;
    BoxCollider collider;
    GameObject snowboard;

    private void Start()
    {
        collider = GetComponent<BoxCollider>();
        stateManager = GameObject.Find("GameStateManager").GetComponent<GameStateManager>();
    }

    // Update is called once per frame
    void Update()
    {
        snowboard = GameObject.Find("Snowboard");

        if (snowboard != null )
        {
            if (collider.bounds.Contains(snowboard.transform.position))
            {
                stateManager.StartMenu();
            }
        }
    }
}
