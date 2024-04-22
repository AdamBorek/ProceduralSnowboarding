using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Splines;

public class SpherePositionRecorder : MonoBehaviour
{
    //public float recordingInterval = 0.1f; // Interval at which to record the position
    public float distanceCutoff = 50f;
    public float timeScale;

    public List<Vector3> recordedPositions = new List<Vector3>();
    private List<GameObject> pathPoints = new List<GameObject>();

    public GameObject sphere;
    Rigidbody sphereRb;

    [SerializeField] MeshFilter terrain;
    [SerializeField] Material sphereMat;
    [SerializeField] Material pathMat;

    [SerializeField] BoxCollider[] finishLines;

    private Vector3[] vertices;
    private Vector3 highestPos;

    int currentPathNum;

    public bool useMiddle = false;

    public bool recording;

    public bool finishedRecording = false;

    public SplineGenerator splineGenerator;

    public float failSafeTimer = 5;
    public float currentFailSafeTime = 0;

    [SerializeField] TerrainGenerator terrainGen;

    private void Start()
    {
        //failSafeTimer *= 50;
    }

    public void StartRecording()
    {
        DeletePaths();
        terrainGen.DeleteNodes();

        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.gameObject.tag = "Sphere";
        sphere.GetComponent<Renderer>().material = sphereMat;
        sphereRb = sphere.AddComponent<Rigidbody>();
        sphereRb.mass = 1f;
        sphereRb.drag = 0.1f;
        //sphereRb.angularDrag = 0.0f;
        sphere.transform.localScale = new Vector3(5, 5, 5);
        highestPos = new Vector3(0, 0, 0);
        vertices = terrain.sharedMesh.vertices;
        FindHighPoint();

        if (useMiddle)
        {
            sphere.transform.position = vertices[vertices.Length / 2 + Mathf.RoundToInt(Mathf.Sqrt(vertices.Length)) / 2] + new Vector3(0, 2, 0);
        }
        else
        {
            sphere.transform.position = highestPos + new Vector3(0, 2, 0);
        }

        currentPathNum = 0;
        recording = true;

        Time.timeScale = timeScale;
    }

    private void Update()
    {
        if (recording)
        {

            currentFailSafeTime += Time.deltaTime;

            Debug.Log(" Current failsafe time: " + currentFailSafeTime);

            DetectFailSafe();

            RecordPosition();

            DetectFinish();
        }
    }

    private void FindHighPoint()
    {
        for (int i = 0; i < vertices.Length; i++) 
        { 
            if (vertices[i].y > highestPos.y)
            {
                highestPos = vertices[i];
            }
        }
    }

    public void DeletePaths()
    {
        GameObject[] objectsInScene = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in objectsInScene)
        {
            if (obj.name.Contains("pathnum"))
            {
                Destroy(obj);
            }
        }
    }

    private void RecordPosition()
    {

        Vector3 objectPosition = sphere.transform.position;

        // get last recorded position and if its too close, dont add

        Vector3 lastPos;

        if (recordedPositions.Count > 0) 
        {
            lastPos = recordedPositions[recordedPositions.Count - 1];
        }
        else
        {
            float radius = sphere.GetComponent<SphereCollider>().radius;
            Vector3 minus = new Vector3(0, radius, 0);
            recordedPositions.Add(objectPosition - minus);

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = new Vector3(8, 8, 8);
            cube.GetComponent<Renderer>().material = pathMat;
            currentPathNum++;
            cube.name = "pathnum " + currentPathNum;
            cube.GetComponent<Collider>().enabled = false;
            cube.transform.position = objectPosition - minus;

            pathPoints.Add(cube);

            lastPos = objectPosition;

            currentFailSafeTime = 0f;
        }


        if (recordedPositions.Count > 0)
        {
            if (Vector3.Distance(objectPosition, lastPos) > distanceCutoff)
            {
                float radius = sphere.GetComponent<SphereCollider>().radius;
                Vector3 minus = new Vector3(0, radius, 0);
                recordedPositions.Add(objectPosition - minus);

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.localScale = new Vector3(8, 8, 8);
                cube.GetComponent<Renderer>().material = pathMat;
                currentPathNum++;
                cube.name = "pathnum " + currentPathNum;
                cube.GetComponent<Collider>().enabled = false;
                cube.transform.position = objectPosition - minus;

                pathPoints.Add(cube);

                currentFailSafeTime = 0f;

            }
        }
    }

    public List<Vector3> GetRecordedPositions()
    {
        return recordedPositions;
    }

    void DetectFailSafe()
    {
        if (currentFailSafeTime > failSafeTimer)
        {
            Time.timeScale = 1f;
            Debug.Log("Sphere has reached finish line!");
            recording = false;
            finishedRecording = true;
            Destroy(sphere);
            splineGenerator.ConstructSpline(recordedPositions, TangentMode.AutoSmooth);
            currentFailSafeTime = 0f;
            return;
        }
    }

    void DetectFinish()
    {
        for (int i = 0; i < finishLines.Length; i++)
        {
            if (finishLines[i].bounds.Contains(sphere.transform.position))
            {
                //foreach (GameObject cube in pathPoints)
                //{
                //    cube.transform.localScale = new Vector3(5, 5, 5);
                //}

                Time.timeScale = 1f;
                Debug.Log("Sphere has reached finish line!");
                recording = false;
                finishedRecording = true;
                Destroy(sphere);

                splineGenerator.ConstructSpline(recordedPositions, TangentMode.AutoSmooth);

                currentFailSafeTime = 0f;
                return;
            }
        }
    }

    public void ScalePathPoints(float scale)
    {
        foreach (GameObject cube in pathPoints)
        {
            cube.transform.localScale = new Vector3(scale, scale, scale);
        }
    }

    //void ConstructSpline()
    //{
    //    spline.Spline.Clear();
    //    for (int i = 0; i < recordedPositions.Count; i++)
    //    {
    //        Vector3 pos = recordedPositions[i];
    //        BezierKnot knot = new BezierKnot(pos);
    //        spline.Spline.Add(knot, TangentMode.AutoSmooth);
    //    }

    //    //splineInstantiater.UpdateInstances();

    //}
}
