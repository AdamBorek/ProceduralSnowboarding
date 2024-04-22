using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class GrindScript : MonoBehaviour
{
    [SerializeField] SplineContainer splineContainer;
    Spline grindSpline;

    [SerializeField] float checkDistance;
    [SerializeField] float lockInDistance;

    GameObject snowboard;

    float currentDistance;

    // Start is called before the first frame update
    void Start()
    {
        grindSpline = splineContainer.Spline;

    }

    private void FixedUpdate()
    {
        if (snowboard != null)
        {
            currentDistance = Vector3.Distance(gameObject.transform.position, snowboard.transform.position);
        }
        else
        {
            currentDistance = Mathf.Infinity;
        }
    }

    // Update is called once per frame
    void Update()
    {
        snowboard = GameObject.Find("Snowboard");
        NewSnowboardController controller;

        if (snowboard != null)
        {
            controller = snowboard.GetComponent<NewSnowboardController>();

            if (!controller.inGrind && controller.canEnterGrind)
            {
                //float railToSnowboardDistance = Vector3.Distance(gameObject.transform.position, snowboard.transform.position);
                if (currentDistance < checkDistance)
                {

                    //Debug.Log("distance to " + gameObject.name + " from snowboard : " + railToSnowboardDistance);

                    float closestPointOnSpline = GetClosestPointOnSpline(0.005f);
                    //Debug.Log("closest spline point on " + gameObject.name + ": " + closestPointOnSpline);

                    Vector3 closestPointGlobal = grindSpline.EvaluatePosition(closestPointOnSpline);
                    Vector3 globalClosestPoint = transform.TransformPoint(closestPointGlobal);

                    float closestPointToSnowboardDistance = Vector3.Distance(globalClosestPoint, snowboard.transform.position);
                    //Debug.Log("distance to snowboard from " + gameObject.name + ": " + closestPointToSnowboardDistance);

                    if (closestPointToSnowboardDistance < lockInDistance && snowboard.transform.position.y >= globalClosestPoint.y)
                    {
                        // switch snwboard to grind mode
                        Debug.Log("locking in now on " + gameObject.name + " at point " + closestPointOnSpline);

                        // switch to grindmode
                        controller.SwitchToInGrind(splineContainer, closestPointOnSpline, 0.02f * controller.currentLinearSpeed);
                    }
                }
            }
            
        }
    }

    float GetClosestPointOnSpline(float interval)
    {
        float closestDistance = Mathf.Infinity;
        float closestPointOnSpline = 0;

        //Debug.Log("Spline length: " + grindSpline.GetLength());

        for (float i = 0; i < grindSpline.GetLength(); i+= interval)
        {
            //Debug.Log("i: " + i);

            float splinePoint = i / grindSpline.GetLength();

            //Debug.Log("splinePoint: " + splinePoint);

            Vector3 splinePointVector3 = grindSpline.EvaluatePosition(splinePoint);
            Vector3 global = transform.TransformPoint(splinePointVector3);

            //Debug.Log("Spline V3: " + global);

            //Debug.Log("Snowboard pos: " + snowboard.transform.position);

            float distance = Vector3.Distance(global, snowboard.transform.position);

            //Debug.Log("Distance: " + distance);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPointOnSpline = splinePoint;
            }

            //Debug.Log("Current closest distance: " + closestDistance);
            //Debug.Log("Which correlates to point: " + closestPointOnSpline);
        }

        return closestPointOnSpline;
    }
}
