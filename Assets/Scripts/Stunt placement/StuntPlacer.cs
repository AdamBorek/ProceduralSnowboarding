using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;
using UnityEngine.UIElements;

public class StuntPlacer : MonoBehaviour
{
    // List of stunts
    //[SerializeField] List<GameObject> stuntList = new List<GameObject>();
    [SerializeField] List<GameObject> bigRampList = new List<GameObject>();
    private List<GameObject> spawnedBigRamps = new List<GameObject>();
    [SerializeField] int maxBigRamps;
    [SerializeField] int bigRampTries;

    [SerializeField] List<GameObject> smallRampList = new List<GameObject>();
    private List<GameObject> spawnedSmallRamps = new List<GameObject>();
    [SerializeField] int maxSmallRamps;

    [SerializeField] List<GameObject> railList = new List<GameObject>();
    private List<GameObject> spawnedRails = new List<GameObject>();
    [SerializeField] int maxRails;

    [SerializeField] int smallRampAndRailTries;

    [SerializeField] GameObject finishLine;

    // spline
    [SerializeField] SplineContainer splineContainer;
    // terrain gen for getting path
    [SerializeField] TerrainGenerator terrainGen;

    [SerializeField] public GameObject stuntHolder;

    private Spline spline;
    private List<Node> path;
    private List<GameObject> spawnedStunts = new List<GameObject>();

    [SerializeField] GameObject playButton;

    private GameObject PlaceStunt(GameObject stunt, float posId, Vector2 forwardAngle)
    {
        Prerequisites prerequisite = stunt.GetComponent<Prerequisites>();

        // calculate spawn things
        Vector3 convertedPos = new Vector3();
        convertedPos = spline.EvaluatePosition(posId);
        convertedPos += new Vector3(0, 10, 0);

        Vector3 spawnPos = new Vector3();

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(convertedPos, Vector3.down, out hit, Mathf.Infinity, 3))
        {
            spawnPos = hit.point - (hit.normal * -prerequisite.offset.y);
        }
        else
        {
            return null;
        }

        // spawn
        GameObject spawnedStunt = Instantiate(stunt, spawnPos, Quaternion.identity, stuntHolder.transform);

        spawnedStunt.transform.up = hit.normal;

        // get forward vector in 2d
        Vector3 forw = spawnedStunt.transform.forward;

        Vector2 forw2D = new Vector2(forw.x, forw.z);

        // get the difference

        float diff = Vector2.SignedAngle(forw2D, forwardAngle.normalized);

        spawnedStunt.transform.Rotate(Vector3.up, -diff, Space.Self);

        forw = spawnedStunt.transform.forward;

        //rotate accordingly
        Vector3 rotation = spawnedStunt.GetComponent<Prerequisites>().rotation;
        spawnedStunt.transform.Rotate(rotation, Space.Self);

        // set slope position id
        spawnedStunt.GetComponent<Prerequisites>().splinePos = posId;

        spawnedStunts.Add(spawnedStunt);

        spawnedStunt.name = "Stunt #" + spawnedStunts.Count;
        spawnedStunt.GetComponent<SmoothnessTester>().steepness = GetSteepness(posId);

        switch (prerequisite.stuntType)
        {
            case StuntType.SmallRamp:

                spawnedStunt.name += " (small ramp)";
                spawnedSmallRamps.Add(spawnedStunt);

                break;
            case StuntType.BigRamp:

                spawnedStunt.name += " (big ramp)";
                spawnedBigRamps.Add(spawnedStunt);

                break;
            case StuntType.Rail:

                spawnedStunt.name += " (rail)";
                spawnedRails.Add(spawnedStunt);

                break;
            default:

                spawnedStunt.name += " (erm what the heck)";

                break;
        }

        return spawnedStunt;
    }

    private void TryPlaceStunt(List<GameObject> stuntList, int stuntId, float posId)
    {
        Prerequisites prerequisite = stuntList[stuntId].GetComponent<Prerequisites>();

        // curvature
        int curveIndex = SplineUtility.SplineToCurveT(spline, posId, out float curveT);

        float curveY = CalculateCurvatureY(curveT, spline.GetCurve(curveIndex));

        // steepness
        float3 dir = spline.EvaluateTangent(posId);

        Vector3 lineV3 = new Vector3(dir.x, dir.y, dir.z);
        Vector3 horizontalSurfaceNormal = Vector3.up; // Assuming the surface is parallel to the ground

        float dotProduct = Vector3.Dot(lineV3.normalized, horizontalSurfaceNormal);
        float angleRadians = Mathf.Acos(dotProduct);
        float steepness = angleRadians * Mathf.Rad2Deg - 90;

        // speed
        float speed1 = path[curveIndex].estimatedSpeed;
        float speed2 = path[curveIndex + 1].estimatedSpeed;

        float estimatedSpeed = Mathf.Lerp(speed1, speed2, curveT);

        // curvature
        float avgCurveBefore = GetAvgCurvature(posId - prerequisite.distanceBefore / spline.GetLength(), posId);
        float avgCurveAfter = GetAvgCurvature(posId, posId + prerequisite.distanceBefore / spline.GetLength());

        float medianCurveBefore = GetMedianCurvature(posId - prerequisite.distanceBefore / spline.GetLength(), posId);
        float medianCurveAfter = GetMedianCurvature(posId, posId + prerequisite.distanceBefore / spline.GetLength());

        // forward difference

        float3 avgForward = GetAvgForward(posId, posId + prerequisite.distanceAfter / spline.GetLength());

        Vector3 convertedAvgForward = new Vector3();
        convertedAvgForward = avgForward;

        Vector2 avgForwardDir = new Vector2(convertedAvgForward.x, convertedAvgForward.z);

        float3 endDir = spline.EvaluateTangent(posId + prerequisite.distanceAfter / spline.GetLength());

        Vector3 convertedEndDir = new Vector3();
        convertedEndDir = endDir;

        Vector2 endTwoDDir = new Vector2(convertedEndDir.x, convertedEndDir.z);

        float directionDiff = Vector2.Angle(avgForwardDir, endTwoDDir);

        float nextAverageSteepness = GetAvgSteepness(posId, posId + prerequisite.distanceAfter / spline.GetLength());

        //check if other stunts are in this area

        if (avgCurveBefore < prerequisite.maxAvgCurveBefore && avgCurveAfter < prerequisite.maxAvgCurveAfter && CheckSpace(prerequisite, posId))
        {
            if (medianCurveBefore < prerequisite.maxMedianCurveBefore && medianCurveAfter < prerequisite.maxMedianCurveAfter && directionDiff < prerequisite.maxDirectionDifference)
            {
                if (steepness > prerequisite.minSlope && steepness < prerequisite.maxSlope && nextAverageSteepness > prerequisite.minAvgSlopeAfter && nextAverageSteepness < prerequisite.maxAvgSlopeAfter)
                {
                    if (estimatedSpeed > prerequisite.minSpeed && estimatedSpeed < prerequisite.maxSpeed)
                    {
                        if (prerequisite.stuntType == StuntType.BigRamp)
                        {
                            if (nextAverageSteepness > steepness * 1.25f)
                            {
                                PlaceStunt(stuntList[stuntId], posId, avgForwardDir);
                            }
                        }
                        else
                        {
                            PlaceStunt(stuntList[stuntId], posId, avgForwardDir);
                        }
                    }
                }
            }
        }
    }

    private float GetSteepness(float point)
    {
        float3 dir = spline.EvaluateTangent(point);

        Vector3 lineV3 = new Vector3(dir.x, dir.y, dir.z);
        Vector3 horizontalSurfaceNormal = Vector3.up; // Assuming the surface is parallel to the ground

        float dotProduct = Vector3.Dot(lineV3.normalized, horizontalSurfaceNormal);
        float angleRadians = Mathf.Acos(dotProduct);
        float steepness = angleRadians * Mathf.Rad2Deg - 90;

        return steepness;
    }

    private float GetAvgSteepness(float start, float end)
    {
        List<float> nextSteepnessList = new List<float>();

        for (float i = start; i < end; i += 0.001f)
        {
            float3 nextDir = spline.EvaluateTangent(i);

            Vector3 nextLineV3 = new Vector3(nextDir.x, nextDir.y, nextDir.z);
            Vector3 nextHorizontalSurfaceNormal = Vector3.up; // Assuming the surface is parallel to the ground

            float nextDotProduct = Vector3.Dot(nextLineV3.normalized, nextHorizontalSurfaceNormal);
            float nextAngleRadians = Mathf.Acos(nextDotProduct);
            float nextSteepness = nextAngleRadians * Mathf.Rad2Deg - 90;

            nextSteepnessList.Add(nextSteepness);
        }

        float sum = 0;
        foreach (float value in nextSteepnessList)
        {
            sum += value;
        }

        return sum / nextSteepnessList.Count;
    }

    private float GetAvgCurvature(float start, float end)
    {
        List<float> nextCurveYList = new List<float>();

        for (float i = start; i < end; i += 0.001f)
        {
            int nextCurveIndex = SplineUtility.SplineToCurveT(spline, i, out float curveT);

            float nextCurveY = CalculateCurvatureY(curveT, spline.GetCurve(nextCurveIndex));

            nextCurveYList.Add(nextCurveY);
        }

        float sum = 0;
        foreach (float value in nextCurveYList)
        {
            sum += value;
        }

        return sum / nextCurveYList.Count;
    }

    private float GetMedianCurvature(float start, float end)
    {
        List<float> nextCurveYList = new List<float>();

        for (float i = start; i < end; i += 0.001f)
        {
            int nextCurveIndex = SplineUtility.SplineToCurveT(spline, i, out float curveT);

            float nextCurveY = CalculateCurvatureY(curveT, spline.GetCurve(nextCurveIndex));

            nextCurveYList.Add(nextCurveY);
        }

        // Sort the list of curvature values
        nextCurveYList.Sort();

        // Calculate the median
        int count = nextCurveYList.Count;
        if (count == 0)
        {
            return 0; // or any default value
        }
        else if (count % 2 == 0)
        {
            // If the list has an even number of elements, average the two middle values
            int middleIndex = count / 2;
            return (nextCurveYList[middleIndex - 1] + nextCurveYList[middleIndex]) / 2f;
        }
        else
        {
            // If the list has an odd number of elements, return the middle value
            return nextCurveYList[count / 2];
        }
    }

    private float3 GetAvgForward(float start, float end)
    {
        List<float3> nextForwardList = new List<float3>();

        for (float i = start; i < end; i += 0.001f)
        {
            float3 nextDir = spline.EvaluateTangent(i);

            nextForwardList.Add(nextDir);
        }

        float3 sum = 0;
        foreach (float3 value in nextForwardList)
        {
            sum += value;
        }

        return sum / nextForwardList.Count;
    }

    private bool CheckSpace(Prerequisites prerequisite, float posId)
    {
        foreach (GameObject stunt in spawnedStunts)
        {
            // get the range of it
            float existingStuntMin = stunt.GetComponent<Prerequisites>().distanceBefore;
            float existingStuntMax = stunt.GetComponent<Prerequisites>().distanceAfter;
            float existingStuntPos = stunt.GetComponent<Prerequisites>().splinePos * spline.GetLength();

            float existingStuntStart = existingStuntPos - existingStuntMin;
            float existingStuntEnd = existingStuntPos + existingStuntMax;

            // check if the stunt trying to be placed is inside the range (including its own range)
            float newStuntMin = prerequisite.distanceBefore;
            float newStuntMax = prerequisite.distanceAfter;
            float newStuntPos = posId * spline.GetLength();

            float newStuntStart = newStuntPos - newStuntMin;
            float newStuntEnd = newStuntPos + newStuntMax;

            // Check if the start or end of the new stunt falls within the range of an existing stunt
            if ((newStuntStart >= existingStuntStart && newStuntStart <= existingStuntEnd) ||
                (newStuntEnd >= existingStuntStart && newStuntEnd <= existingStuntEnd))
            {
                return false; // Overlap detected
            }
            // Check if the existing stunt falls within the range of the new stunt
            if ((existingStuntStart >= newStuntStart && existingStuntStart <= newStuntEnd) ||
                (existingStuntEnd >= newStuntStart && existingStuntEnd <= newStuntEnd))
            {
                return false; // Overlap detected
            }
        }

        return true;
    }

    public void PlaceStunts(int iterations)
    {
        DeleteAllStunts();

        spline = splineContainer.Spline;
        path = terrainGen.path;

        for (int i = 0; i < bigRampTries; i++)
        {
            int randStuntId = UnityEngine.Random.Range(0, bigRampList.Count - 1);
            float randPosId = UnityEngine.Random.Range(0.1f, 0.9f);

            TryPlaceStunt(bigRampList, randStuntId, randPosId);

            if(spawnedBigRamps.Count == maxBigRamps)
            {
                break;
            }
        }

        for (int i = 0; i < smallRampAndRailTries; i++)
        {
            int rampOrRail = UnityEngine.Random.Range(0, 2);

            if (rampOrRail == 0 && spawnedSmallRamps.Count < maxSmallRamps)
            {
                int randStuntId = UnityEngine.Random.Range(0, smallRampList.Count - 1);
                float randPosId = UnityEngine.Random.Range(0.1f, 0.9f);

                TryPlaceStunt(smallRampList, randStuntId, randPosId);
            }
            else if (rampOrRail == 1 && spawnedRails.Count < maxRails)
            {
                int randStuntId = UnityEngine.Random.Range(0, railList.Count);
                float randPosId = UnityEngine.Random.Range(0.1f, 0.9f);

                TryPlaceStunt(railList, randStuntId, randPosId);
            }
        }

        Vector3 forw3D = spline.EvaluateTangent(1);
        Vector2 forw2D = new Vector2(forw3D.x, forw3D.z);
        PlaceFinishLineAlt(forw2D);

        playButton.SetActive(true);

    }

    public void PlaceFinishLine()
    {

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(spline.EvaluatePosition(1) + new float3(0, 10, 0), Vector3.down, out hit, Mathf.Infinity, 3))
        {
            GameObject finish = Instantiate(finishLine, spline.EvaluatePosition(1f), Quaternion.identity);
            finish.transform.LookAt(spline.EvaluateTangent(0.999f));
            finish.transform.parent = stuntHolder.transform;
        }
        else
        {
            return;
        }
    }

    private void PlaceFinishLineAlt(Vector2 forwardAngle)
    {

        // calculate spawn things
        Vector3 convertedPos = spline.EvaluatePosition(1) + new float3(0, 10, 0);

        Vector3 spawnPos = new Vector3();

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(convertedPos, Vector3.down, out hit, Mathf.Infinity, 3))
        {
            spawnPos = hit.point;
        }
        else
        {
            return;
        }

        // spawn
        GameObject finish = Instantiate(finishLine, spawnPos, Quaternion.identity, stuntHolder.transform);

        finish.transform.up = hit.normal;

        // get forward vector in 2d
        Vector3 forw = finish.transform.forward;

        Vector2 forw2D = new Vector2(forw.x, forw.z);

        // get the difference

        float diff = Vector2.SignedAngle(forw2D, forwardAngle.normalized);

        //Debug.Log(spawnedStunts.Count + " Diff: " + diff);

        finish.transform.Rotate(Vector3.up, -diff + 180, Space.Self);
        finish.transform.position += finish.transform.up * 30;
    }

    public void DeleteAllStunts()
    {
        // Get all child objects of the stuntParent
        Transform[] children = stuntHolder.GetComponentsInChildren<Transform>();

        // Start from index 1 to avoid deleting the stuntHolder itself
        for (int i = 1; i < children.Length; i++)
        {
            // Destroy the child object
            Destroy(children[i].gameObject);
        }

        // Clear list of spawned stunts
        spawnedStunts.Clear();
        spawnedBigRamps.Clear();
        spawnedSmallRamps.Clear();
        spawnedRails.Clear();
    }


    public Vector3 EvaluateFirstDerivative(float t, BezierCurve curve)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = 3 * uu * (curve.P1 - curve.P0) +
                    6 * u * t * (curve.P2 - curve.P1) +
                    3 * tt * (curve.P3 - curve.P2);

        return p;
    }

    public Vector3 EvaluateSecondDerivative(float t, BezierCurve curve)
    {
        float u = 1 - t;

        Vector3 p = 6 * u * (curve.P2 - 2 * curve.P1 + curve.P0) +
                    6 * t * (curve.P3 - 2 * curve.P2 + curve.P1);

        return p;
    }

    public float CalculateCurvatureY(float t, BezierCurve curve)
    {
        Vector3 firstDerivative = EvaluateFirstDerivative(t, curve);
        Vector3 secondDerivative = EvaluateSecondDerivative(t, curve);

        float numerator = Mathf.Abs(firstDerivative.x * secondDerivative.z - secondDerivative.x * firstDerivative.z);
        float denominator = Mathf.Pow(firstDerivative.magnitude, 3);

        float curvature = numerator / denominator;

        return curvature * 100;
    }
}
