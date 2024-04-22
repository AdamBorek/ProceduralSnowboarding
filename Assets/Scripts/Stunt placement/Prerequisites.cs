using UnityEngine;

public enum StuntType
{
    SmallRamp,
    BigRamp,
    Rail,
    Combined
}

public class Prerequisites : MonoBehaviour
{
    [Header("Stunt Type")]
    public StuntType stuntType;

    [Header("Offset")]
    public Vector3 offset;

    [Header("Orientation")]
    public Vector3 rotation;

    [Header ("Speed")]
    public float minSpeed;
    public float maxSpeed;

    [Header("Steepness")]
    public float minSlope;
    public float maxSlope;

    public float minAvgSlopeAfter;
    public float maxAvgSlopeAfter;

    [Header("Curvature")]
    public float maxAvgCurveBefore;
    public float maxAvgCurveAfter;

    public float maxMedianCurveBefore;
    public float maxMedianCurveAfter;

    [Header("Direction")]
    public float maxDirectionDifference;

    [Header("Spacing")]
    public float distanceBefore;
    public float distanceAfter;

    [Header("Spline Position")]
    public float splinePos;

    //private void Start()
    //{
    //    if (Sean.IsLeaving())
    //    {
    //        Sean.Say("Have a good weekend!");
    //    }
    //}
}

