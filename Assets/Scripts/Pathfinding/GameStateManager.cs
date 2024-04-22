using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class GameStateManager : MonoBehaviour
{
    [SerializeField] GameObject playerCam;
    [SerializeField] public GameObject snowboard;
    [SerializeField] SpherePositionRecorder recorder;
    [SerializeField] GameObject pathCam;
    [SerializeField] GameObject orbitingCam;
    [SerializeField] GameObject menuCanvas;
    [SerializeField] GameObject playCanvas;
    [SerializeField] TerrainGenerator terrainGen;
    [SerializeField] SplineGenerator splineGen;
    public Vector3 startPos;

    // Start is called before the first frame update
    void Start()
    {
        StartMenu();
        terrainGen.GenerateEntireMountain();
        terrainGen.ConstructCombinedMesh();
        terrainGen.ConstructNodesAlternative();
    }

    public void StartMenu()
    {
        orbitingCam.SetActive(true);
        menuCanvas.SetActive(true);
        playCanvas.SetActive(false);

        pathCam.SetActive(false);
        playerCam.SetActive(false);
        snowboard.SetActive(false);
    }

    public void StartPlay()
    {

        playerCam.SetActive(true);
        snowboard.SetActive(true);

        startPos = transform.TransformPoint(splineGen.splineContainer.Spline.EvaluatePosition(0)) + new Vector3(0, 15, 0);


        snowboard.GetComponent<Rigidbody>().isKinematic = true;
        snowboard.GetComponent<Rigidbody>().position = startPos;
        snowboard.transform.position = startPos;
        snowboard.GetComponent<Rigidbody>().isKinematic = false;

        playerCam.transform.position = startPos + new Vector3(0, 1, 0);

        Vector3 startTangent = splineGen.splineContainer.Spline.EvaluateTangent(0);

        startTangent = startTangent.normalized * 20;
        snowboard.GetComponent<Rigidbody>().velocity = startTangent;
        snowboard.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        Quaternion lookAt = new Quaternion();

        IEnumerable<BezierKnot> knots = splineGen.splineContainer.Spline.Knots;

        lookAt = knots.ElementAt(0).Rotation;

        //snowboard.transform.LookAt(startTangent.normalized);
        //snowboard.transform.Rotate(new Vector3(-90, 0, 0), Space.Self);
        snowboard.transform.rotation = lookAt;
        snowboard.GetComponent<Rigidbody>().rotation = lookAt;


        menuCanvas.SetActive(false);
        playCanvas.SetActive(true);
        pathCam.SetActive(false);
        orbitingCam.SetActive(false);
    }
}
