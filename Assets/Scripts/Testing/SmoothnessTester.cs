using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;
using System.Linq;
using NUnit;

public class SmoothnessTester : MonoBehaviour
{
    [SerializeField] float testDitance;

    bool tested = false;

    private GameObject snowboard;

    [SerializeField] SplineContainer splineContainer;

    private Spline pathSpline;

    Vector3[] controlPoints = new Vector3[3];

    Prerequisites prerequisite;

    CsvExporter csvExporter;

    public MountainType mountainType;
    public StuntType stuntType;
    public bool normalSetup;
    public float steepness;
    public float avgSetupCurve;
    public float medianSetupCurve;
    public float uniformity;
    public float boardSpeed;

    private void Start()
    {
        //splineContainer = GetComponent<SplineContainer>();
        pathSpline = GameObject.Find("SplineContainer").GetComponent<SplineContainer>().Spline;
        csvExporter = GameObject.Find("CSVExporter").GetComponent<CsvExporter>();
        prerequisite = gameObject.GetComponent<Prerequisites>();
    }

    // Update is called once per frame
    void Update()
    {

        if (!tested)
        {
            snowboard = GameObject.Find("Snowboard");

            float currentDistance = new float();

            if (snowboard != null )
            {
                currentDistance = Vector3.Distance(snowboard.transform.position, gameObject.transform.position);
            }

            if (snowboard != null && currentDistance < testDitance)
            {
                controlPoints = GetControlPoints();
            }
        }

    }

    Vector3[] GetControlPoints()
    {
        Vector3[] tempControlPoints = new Vector3[3];

        tested = true;

        Vector2 snowboardPos2D = new Vector2(snowboard.transform.position.x, snowboard.transform.position.z);
        Vector2 snowboardVelocityVector = new Vector2(snowboard.GetComponent<Rigidbody>().velocity.x, snowboard.GetComponent<Rigidbody>().velocity.z).normalized;
        
        Vector2 stuntPos2D = new Vector2(gameObject.transform.position.x, transform.position.z);
        Vector2 stuntForwardVector = new Vector2(-gameObject.transform.forward.x, -gameObject.transform.forward.z).normalized;

        // Calculate the direction vector between the two starting points
        Vector2 deltaPos = stuntPos2D - snowboardPos2D;

        // Calculate the determinant
        float determinant = stuntForwardVector.x * snowboardVelocityVector.y - stuntForwardVector.y * snowboardVelocityVector.x;

        float stuntToSnowboardZ = gameObject.transform.InverseTransformPoint(snowboard.transform.position).z;

        // Check if the vectors are parallel (determinant close to zero)
        if (Mathf.Approximately(determinant, 0) || stuntToSnowboardZ > 0)
        {
            // Handle parallel case
            // Vectors are either parallel or collinear, so there is no intersection point
            Debug.Log("Vectors are parallel or collinear, no intersection point, or somehow the snowboard is after stunt already");

            return null;
        }
        else
        {
            // Calculate the scalar values for each vector
            float scalarStunt = (deltaPos.x * snowboardVelocityVector.y - deltaPos.y * snowboardVelocityVector.x) / determinant;
            float scalarSnowboard = (deltaPos.x * stuntForwardVector.y - deltaPos.y * stuntForwardVector.x) / determinant;

            // Calculate the intersection point
            Vector2 intersectionPoint = snowboardPos2D - scalarSnowboard * snowboardVelocityVector;
            //Debug.Log("Intersection point: " + intersectionPoint);


            //intersectionPoint = Intersect(snowboardPos2D, snowboardVelocityVector, stuntPos2D, stuntForwardVector);

            //Debug.Log("Intersection point: " + intersectionPoint);
            // Get global position of intersection

            Vector3 rayCastStart = new Vector3(intersectionPoint.x, 10000, intersectionPoint.y);

            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(rayCastStart, Vector3.down, out hit, Mathf.Infinity, 3))
            {
                // set the first control point to be the snowboard

                RaycastHit snowboardHit = new RaycastHit();
                if (Physics.Raycast(snowboard.transform.position, Vector3.down, out snowboardHit, Mathf.Infinity, 3))
                {
                    tempControlPoints[0] = gameObject.transform.InverseTransformPoint(snowboardHit.point);
                }
                else
                {
                    tempControlPoints[0] = gameObject.transform.InverseTransformPoint(snowboard.transform.position);
                }

                bool goofy = snowboard.GetComponent<NewSnowboardController>().goofy;

                float hitPointZSnowboard = snowboard.transform.InverseTransformPoint(hit.point).z;
                float hitPointZStunt = gameObject.transform.InverseTransformPoint(hit.point).z;
                //Debug.Log("Hit Z in relation to snowboard: " + hitPointZ);

                // set the second control point
                // check if the point is behind the snowboard
                if ((!goofy && hitPointZSnowboard > 0 && hitPointZStunt < 0) || (goofy && hitPointZSnowboard < 0 && hitPointZStunt < 0))
                {
                    tempControlPoints[1] = gameObject.transform.InverseTransformPoint(hit.point);
                    //Debug.Log("Local intersection point: " + tempControlPoints[1]);
                    normalSetup = true;
                    //Debug.Log("Normal setup for " + gameObject.name);
                }
                else
                {

                    tempControlPoints[1] = gameObject.transform.InverseTransformPoint(pathSpline.EvaluatePosition(gameObject.GetComponent<Prerequisites>().splinePos - (40 / pathSpline.GetLength())));
                    normalSetup = false;
                    //Debug.Log("Backup setup for " + gameObject.name);
                }

                // set the last control point to be the stunt
                //tempControlPoints[2] = Vector3.zero;
                //tempControlPoints[2] = pathSpline.EvaluatePosition(gameObject.GetComponent<Prerequisites>().splinePos);
                tempControlPoints[2] = gameObject.transform.InverseTransformPoint(pathSpline.EvaluatePosition(gameObject.GetComponent<Prerequisites>().splinePos));

                controlPoints = tempControlPoints;

                DrawSpline(snowboard.GetComponent<Rigidbody>().velocity);

                SetupValues();
                AddData();

                return tempControlPoints;
            }
            else
            {
                Debug.Log("Raycast did not hit after point was found");
                return null;
            }

        }
    }

    void SetupValues()
    {
        mountainType = GameObject.Find("Terrain").GetComponent<TerrainGenerator>().type;
        stuntType = prerequisite.stuntType;
        //normalSetup gets set in the GetControlPoints() method;
        avgSetupCurve = GetAvgCurvature(0, 1);
        medianSetupCurve = GetMedianCurvature(0, 1);

        // Compute deviations from the mean
        List<float> deviations = CalculateDeviations(avgSetupCurve, 0, 1);

        // Calculate standard deviation
        uniformity = CalculateStandardDeviation(deviations);
        boardSpeed = snowboard.GetComponent<NewSnowboardController>().currentLinearSpeed;

    }

    void DrawSpline(Vector3 velocity)
    {
        for (int i = 0; i < controlPoints.Length; i++)
        {
            BezierKnot knot = new BezierKnot(controlPoints[i]);
            splineContainer.Spline.Add(knot, UnityEngine.Splines.TangentMode.AutoSmooth);
        }

        IEnumerable<BezierKnot> knots = splineContainer.Spline.Knots;

        BezierKnot first = knots.ElementAt(0);
        first.Rotation = Quaternion.LookRotation(velocity);

        //BezierKnot third = knots.ElementAt(2);
        //third.Rotation = gameObject.transform.rotation;
    }

    void AddData()
    {
        Prerequisites prerequisite = gameObject.GetComponent<Prerequisites>();

        csvExporter.AddNewLine(mountainType, stuntType, normalSetup, steepness, avgSetupCurve, medianSetupCurve, uniformity, boardSpeed);
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

        return curvature;
    }

    private float GetAvgCurvature(float start, float end)
    {
        List<float> nextCurveYList = new List<float>();

        for (float i = start; i < end; i += 0.001f)
        {
            int nextCurveIndex = SplineUtility.SplineToCurveT(splineContainer.Spline, i, out float curveT);

            float nextCurveY = CalculateCurvatureY(curveT, splineContainer.Spline.GetCurve(nextCurveIndex));

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
            int nextCurveIndex = SplineUtility.SplineToCurveT(splineContainer.Spline, i, out float curveT);

            float nextCurveY = CalculateCurvatureY(curveT, splineContainer.Spline.GetCurve(nextCurveIndex));

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

    List<float> CalculateDeviations(float avg, float start, float end)
    {
        List<float> deviations = new List<float>();

        List<float> nextCurveYList = new List<float>();

        for (float i = start; i < end; i += 0.001f)
        {
            int nextCurveIndex = SplineUtility.SplineToCurveT(splineContainer.Spline, i, out float curveT);

            float nextCurveY = CalculateCurvatureY(curveT, splineContainer.Spline.GetCurve(nextCurveIndex));

            nextCurveYList.Add(nextCurveY);
        }

        foreach (var value in nextCurveYList)
        {
            deviations.Add(Mathf.Abs(value - avg));
        }
        return deviations;
    }

    float CalculateStandardDeviation(List<float> values)
    {
        float avg = avgSetupCurve;
        float sumSquaredDeviations = 0;
        foreach (var value in values)
        {
            sumSquaredDeviations += Mathf.Pow(value - avg, 2);
        }
        return Mathf.Sqrt(sumSquaredDeviations / values.Count);
    }
}
