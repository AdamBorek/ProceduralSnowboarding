using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Splines;

public class SplineGenerator : MonoBehaviour
{

    public SplineContainer splineContainer;

    public static SplineGenerator instance;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        splineContainer = GetComponent<SplineContainer>();
    }

    public void ConstructSpline(List<Vector3> positions, TangentMode tangentMode)
    {
        ClearSpline();

        for (int i = 0; i < positions.Count; i++)
        {
            BezierKnot knot = new BezierKnot(positions[i]);
            splineContainer.Spline.Add(knot, tangentMode);
        }
    }

    public void ClearSpline()
    {
        splineContainer.Spline.Clear();
    }
}
