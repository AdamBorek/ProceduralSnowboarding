using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

public enum MountainType
{
    Mellow,
    Steep,
    Extreme
}

public class TerrainGenerator : MonoBehaviour
{
    public GameStateManager gameStateManager;

    public bool printTimers;

    [Header("Mesh Settings")]
    public int mapSize = 255;
    public float scale = 20;
    public float elevationScale = 10;
    public Material material;

    [Header("Base mountain")]
    public AnimationCurve shape;
    public float baseScale = 1f;

    [Header("Mountain side")]
    public AnimationCurve side;
    public float sideScale = 1f;

    public bool isMountainSide = false;

    // Internal
    public float[] combinedMap;
    float[] baseMap;
    float[] sideMap;
    float[] noiseMap;
    Mesh mesh;

    public MountainType type;
    [SerializeField] Dropdown dropDown;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    [SerializeField] BoxCollider[] boxColliders;

    [SerializeField] GameObject fenceHolder;
    [SerializeField] List<GameObject> fenceList;
    public float pathWidth;
    public float fenceDensity;

    [SerializeField] StuntPlacer stuntPlacer;

    // Pathfinding
    public int nodeMapSize = 100;
    public int maxAngle = 60;
    public int pathSegmentNum = 3;
    private List<Node> nodes = new List<Node>();

    public List<Node> path = new List<Node>();

    [SerializeField] GameObject pathNodesHolder;

    private int[] randomEndPoint;

    private int[] randomStartPoint;

    public bool randomStart;

    private PathfindingRemastered pathfinding;

    private DownhillPathfinder downhillPathfinder;

    public float speedVariable = 2.5f;
    public float slowdownVariable = 5f;

    public AnimationCurve slopeFunction;
    public int preferredSlope;
    public int slopeThreshold;
    public float curvatureCostMultiplier = 1f;

    public bool euclideanCostOn = false;
    public bool slopeCostOn = false;
    public bool directionCostOn = false;
    public bool curvatureCostOn = false;

    public void BoolToggle(bool value)
    {
        value = !value;
    }

    public bool shouldSpawnNodes = false;

    public Material nodeMaterial;
    public Material pathMaterial;
    public Material wallMaterial;

    public SpherePositionRecorder recorder;

    public SplineGenerator splineGenerator;

    [SerializeField] GameObject playButton;

    public void GenerateHeightMap()
    {
        noiseMap = FindObjectOfType<HeightMapGenerator>().GenerateHeightMap(mapSize);
    }

    public void GenerateMountainBase()
    {
        baseMap = FindObjectOfType<HeightMapGenerator>().GenerateMountainBaseShape(mapSize, shape, baseScale);
    }

    public void  GenerateMountainSide()
    {
        sideMap = FindObjectOfType<HeightMapGenerator>().GenerateMountainSide(mapSize, side, sideScale);
    }

    public void SetType()
    {
        switch (dropDown.value)
        {
            case 0:
                type = MountainType.Mellow;
                break;

            case 1:
                type = MountainType.Steep;
                break;

            case 2:
                type = MountainType.Extreme;
                break;
        }
    }

    public void SetValues()
    {
        switch (type)
        {
            case MountainType.Mellow:
                maxAngle = 44;
                elevationScale = 625;
                break;

            case MountainType.Steep:
                maxAngle = 51;
                elevationScale = 775;
                break;

            case MountainType.Extreme:
                maxAngle = 58;
                elevationScale = 925;
                break;
        }
    }

    public void GenerateEntireMountain()
    {
        SetType();
        SetValues();

        DeleteNodes();
        recorder.DeletePaths();
        DeletePath();
        splineGenerator.ClearSpline();

        // Generate base (entire mountain or side)
        if (!isMountainSide)
        {
            GenerateMountainBase();
        }
        else
        {
            GenerateMountainSide();
        }

        // Generate height map
        GenerateHeightMap();

        // Combine the maps
        CombineEntireMountain(isMountainSide);

        playButton.SetActive(false);
    }

    void CombineEntireMountain(bool side)
    {
        int length = mapSize * mapSize;
        combinedMap = new float[length];

        for (int i = 0; i < length; i++)
        {
            if (!side)
            {
                combinedMap[i] = baseMap[i] + noiseMap[i];
            }
            else
            {
                combinedMap[i] = sideMap[i] + noiseMap[i];
            }
        }
    }

    public void ConstructNoiseMesh()
    {
        Vector3[] verts = new Vector3[mapSize * mapSize];
        int[] triangles = new int[(mapSize - 1) * (mapSize - 1) * 6];
        int t = 0;

        for (int i = 0; i < mapSize * mapSize; i++)
        {
            int x = i % mapSize;
            int y = i / mapSize;
            int mapIndex = y * mapSize + x;
            int meshMapIndex = y * mapSize + x;

            Vector2 percent = new Vector2(x / (mapSize - 1f), y / (mapSize - 1f));
            Vector3 pos = new Vector3(percent.x * 2 - 1, 0, percent.y * 2 - 1) * scale;

            float normalizedHeight = noiseMap[mapIndex];
            pos += Vector3.up * normalizedHeight * elevationScale;
            verts[meshMapIndex] = pos;

            // Construct triangles
            if (x != mapSize - 1 && y != mapSize - 1)
            {
                t = (y * (mapSize - 1) + x) * 3 * 2;

                triangles[t + 0] = meshMapIndex + mapSize;
                triangles[t + 1] = meshMapIndex + mapSize + 1;
                triangles[t + 2] = meshMapIndex;

                triangles[t + 3] = meshMapIndex + mapSize + 1;
                triangles[t + 4] = meshMapIndex + 1;
                triangles[t + 5] = meshMapIndex;
                t += 6;
            }
        }

        if (mesh == null)
        {
            mesh = new Mesh();
        }
        else
        {
            mesh.Clear();
        }
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        AssignMeshComponents();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
        meshCollider.sharedMesh = mesh;

        material.SetFloat("_MaxHeight", elevationScale);
    }

    public void ConstructBaseMesh()
    {
        Vector3[] verts = new Vector3[mapSize * mapSize];
        int[] triangles = new int[(mapSize - 1) * (mapSize - 1) * 6];
        int t = 0;

        for (int i = 0; i < mapSize * mapSize; i++)
        {
            int x = i % mapSize;
            int y = i / mapSize;
            int borderedMapIndex = y * mapSize + x;
            int meshMapIndex = y * mapSize + x;

            Vector2 percent = new Vector2(x / (mapSize - 1f), y / (mapSize - 1f));
            Vector3 pos = new Vector3(percent.x * 2 - 1, 0, percent.y * 2 - 1) * scale;

            float normalizedHeight = baseMap[borderedMapIndex];
            pos += Vector3.up * normalizedHeight * elevationScale;
            verts[meshMapIndex] = pos;

            // Construct triangles
            if (x != mapSize - 1 && y != mapSize - 1)
            {
                t = (y * (mapSize - 1) + x) * 3 * 2;

                triangles[t + 0] = meshMapIndex + mapSize;
                triangles[t + 1] = meshMapIndex + mapSize + 1;
                triangles[t + 2] = meshMapIndex;

                triangles[t + 3] = meshMapIndex + mapSize + 1;
                triangles[t + 4] = meshMapIndex + 1;
                triangles[t + 5] = meshMapIndex;
                t += 6;
            }
        }

        if (mesh == null)
        {
            mesh = new Mesh();
        }
        else
        {
            mesh.Clear();
        }
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        AssignMeshComponents();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
        meshCollider.sharedMesh = mesh;

        material.SetFloat("_MaxHeight", elevationScale);
    }

    public void ConstructSideMesh()
    {
        Vector3[] verts = new Vector3[mapSize * mapSize];
        int[] triangles = new int[(mapSize - 1) * (mapSize - 1) * 6];
        int t = 0;

        for (int i = 0; i < mapSize * mapSize; i++)
        {
            int x = i % mapSize;
            int y = i / mapSize;
            int borderedMapIndex = y * mapSize + x;
            int meshMapIndex = y * mapSize + x;

            Vector2 percent = new Vector2(x / (mapSize - 1f), y / (mapSize - 1f));
            Vector3 pos = new Vector3(percent.x * 2 - 1, 0, percent.y * 2 - 1) * scale;

            float normalizedHeight = sideMap[borderedMapIndex];
            pos += Vector3.up * normalizedHeight * elevationScale;
            verts[meshMapIndex] = pos;

            // Construct triangles
            if (x != mapSize - 1 && y != mapSize - 1)
            {
                t = (y * (mapSize - 1) + x) * 3 * 2;

                triangles[t + 0] = meshMapIndex + mapSize;
                triangles[t + 1] = meshMapIndex + mapSize + 1;
                triangles[t + 2] = meshMapIndex;

                triangles[t + 3] = meshMapIndex + mapSize + 1;
                triangles[t + 4] = meshMapIndex + 1;
                triangles[t + 5] = meshMapIndex;
                t += 6;
            }
        }

        if (mesh == null)
        {
            mesh = new Mesh();
        }
        else
        {
            mesh.Clear();
        }
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        AssignMeshComponents();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
        meshCollider.sharedMesh = mesh;

        material.SetFloat("_MaxHeight", elevationScale);
    }

    public void ConstructCombinedMesh()
    {
        Vector3[] verts = new Vector3[mapSize * mapSize];
        int[] triangles = new int[(mapSize - 1) * (mapSize - 1) * 6];
        int t = 0;

        for (int i = 0; i < mapSize * mapSize; i++)
        {
            int x = i % mapSize;
            int y = i / mapSize;
            int mapIndex = y * mapSize + x;
            int meshMapIndex = y * mapSize + x;

            Vector2 percent = new Vector2(x / (mapSize - 1f), y / (mapSize - 1f));
            Vector3 pos = new Vector3(percent.x * 2 - 1, 0, percent.y * 2 - 1) * scale;

            float normalizedHeight = combinedMap[mapIndex];
            pos += Vector3.up * normalizedHeight * elevationScale;
            verts[meshMapIndex] = pos;

            // Construct triangles
            if (x != mapSize - 1 && y != mapSize - 1)
            {
                t = (y * (mapSize - 1) + x) * 3 * 2;

                triangles[t + 0] = meshMapIndex + mapSize;
                triangles[t + 1] = meshMapIndex + mapSize + 1;
                triangles[t + 2] = meshMapIndex;

                triangles[t + 3] = meshMapIndex + mapSize + 1;
                triangles[t + 4] = meshMapIndex + 1;
                triangles[t + 5] = meshMapIndex;
                t += 6;
            }
        }

        if (mesh == null)
        {
            mesh = new Mesh();
        }
        else
        {
            mesh.Clear();
        }
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        AssignMeshComponents();
        AddBoxColliders();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
        meshCollider.sharedMesh = mesh;

        material.SetFloat("_MaxHeight", elevationScale);

    }

    public void ConstructNodesAlternative()
    {
        DeleteNodes();
        recorder.DeletePaths();
        splineGenerator.ClearSpline();
        DeletePath();

        List<Node> tempNodes = new List<Node>();


        Bounds bounds = mesh.bounds;
        int gridSizeX = nodeMapSize;
        int gridSizeZ = nodeMapSize;

        float xStep = bounds.size.x / (float)(gridSizeX - 1);
        float zStep = bounds.size.z / (float)(gridSizeZ - 1);

        RaycastHit[] hits = new RaycastHit[1]; // Array to store raycast hits
        Vector3 raycastOrigin = new Vector3();

        for (int i = 0; i < gridSizeX; i++)
        {
            for (int j = 0; j < gridSizeZ; j++)
            {
                float x = bounds.min.x + i * xStep;
                float z = bounds.min.z + j * zStep;

                raycastOrigin.Set(x, bounds.max.y, z);

                // Use Physics.RaycastNonAlloc to avoid unnecessary memory allocations
                int numHits = Physics.RaycastNonAlloc(raycastOrigin, Vector3.down, hits, Mathf.Infinity);

                if (numHits > 0)
                {
                    float angle = Vector3.Angle(hits[0].normal, Vector3.up);

                    Node node = new Node
                    {
                        position = hits[0].point,
                        normal = hits[0].normal,
                        x = i,
                        y = j,
                        wall = angle > maxAngle
                    };

                    tempNodes.Add(node);
                }
            }
        }

        nodes = tempNodes;

        //pathfinding = new PathfindingRemastered(nodeMapSize, nodeMapSize, pathSegmentNum, slopeFunction, curvatureConstraint, curvatureCostMultiplier, moveCostOn);
        downhillPathfinder = new DownhillPathfinder(nodeMapSize, nodeMapSize, pathSegmentNum, slopeFunction, speedVariable, slowdownVariable, maxAngle, euclideanCostOn, slopeCostOn, directionCostOn, curvatureCostOn);
        SetWalkableNodes(nodes);
        // if (shouldSpawnNodes)
        // {
        //     SpawnNodes();
        // }
    }


    private int[] GetRandomPointInCircle(int mapWidth, int mapHeight, float minCircleRadius, float maxCircleRadius)
    {
        // Clamp the provided minCircleRadius to ensure it doesn't exceed the maximum possible radius
        minCircleRadius = Mathf.Clamp(minCircleRadius, 0, maxCircleRadius);

        // Clamp the provided maxCircleRadius to ensure it doesn't exceed the maximum possible radius
        maxCircleRadius = Mathf.Clamp(maxCircleRadius, minCircleRadius, 1f);

        // Calculate the random radius within the specified range
        float randomRadius = Mathf.Lerp(minCircleRadius, maxCircleRadius, UnityEngine.Random.value);

        // Generate a random angle in radians
        float randomAngle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);

        // Calculate the coordinates of the point on the outer circle
        float x = mapWidth * 0.5f + randomRadius * mapWidth * 0.5f * Mathf.Cos(randomAngle);
        float y = mapHeight * 0.5f + randomRadius * mapHeight * 0.5f * Mathf.Sin(randomAngle);

        // Round to the nearest integer to get the grid coordinates
        int gridX = Mathf.RoundToInt(x);
        int gridY = Mathf.RoundToInt(y);

        // Clamp the coordinates to ensure they are within the grid bounds
        gridX = Mathf.Clamp(gridX, 0, mapWidth - 1);
        gridY = Mathf.Clamp(gridY, 0, mapHeight - 1);

        int[] randomPoint = new int[2];

        randomPoint[0] = gridX;
        randomPoint[1] = gridY;

        return randomPoint;
    }

    private void SetWalkableNodes(List<Node> nodes)
    {
        foreach (var node in nodes)
        {
            //pathfinding.GetNode(node.x, node.y).SetIsWalkable(!node.wall);
            //pathfinding.GetNode(node.x, node.y).pos = node.position;
            //pathfinding.GetNode(node.x, node.y).normal = node.normal;

            downhillPathfinder.GetNode(node.x, node.y).SetIsWalkable(!node.wall);
            downhillPathfinder.GetNode(node.x, node.y).pos = node.position;
            downhillPathfinder.GetNode(node.x, node.y).normal = node.normal;
        }
    }

    public void RandomStartPoint()
    {
        randomStartPoint = GetRandomPointInCircle(nodeMapSize, nodeMapSize, 0f, 0.15f);

        while (!downhillPathfinder.GetNode(randomStartPoint[0], randomStartPoint[1]).isWalkable)
        {
            randomStartPoint = GetRandomPointInCircle(nodeMapSize, nodeMapSize, 0f, 0.15f);
        }
    }

    public void RandomEndPoint()
    {
        GameObject objectToDelete = GameObject.Find("End node");

        if (objectToDelete != null)
        {
            Destroy(objectToDelete);
        }

        randomEndPoint = GetRandomPointInCircle(nodeMapSize, nodeMapSize, 0.9f, 0.95f);

        while (!pathfinding.GetNode(randomEndPoint[0], randomEndPoint[1]).isWalkable)
        {
            randomEndPoint = GetRandomPointInCircle(nodeMapSize, nodeMapSize, 0.9f, 0.95f);
        }

        GameObject EndNode = GameObject.CreatePrimitive(PrimitiveType.Cube);
        EndNode.name = "End node";
        EndNode.transform.position = new Vector3(nodes[randomEndPoint[0] * nodeMapSize + randomEndPoint[1]].position.x, nodes[randomEndPoint[0] * nodeMapSize + randomEndPoint[1]].position.y, nodes[randomEndPoint[0] * nodeMapSize + randomEndPoint[1]].position.z);
        EndNode.GetComponent<Renderer>().material.color = Color.green;
        DestroyImmediate(EndNode.GetComponent<BoxCollider>());
        EndNode.transform.localScale = new Vector3(100, 100, 100);
    }

    public void FindPath(float minLength)
    {
        DeletePath();
        while (splineGenerator.splineContainer.Spline.GetLength() < minLength + elevationScale / 2)
        {
            FindDownPath();
        }
    }

    public void FindDownPath()
    {
        DeletePath();

        ReconstructSlopeFunction();
        downhillPathfinder = new DownhillPathfinder(nodeMapSize, nodeMapSize, pathSegmentNum, slopeFunction, speedVariable, slowdownVariable, maxAngle, euclideanCostOn, slopeCostOn, directionCostOn, curvatureCostOn);
        SetWalkableNodes(nodes);

        // Start recording time
        Stopwatch stopwatch = Stopwatch.StartNew();

        List<NewPathNode> AstarPath;


        if (!randomStart)
        {
            AstarPath = downhillPathfinder.FindPath(nodeMapSize / 2, nodeMapSize / 2);
        }
        else
        {
            RandomStartPoint();
            AstarPath = downhillPathfinder.FindPath(randomStartPoint[0], randomStartPoint[1]);
        }

        while (AstarPath[AstarPath.Count - 2].pos.y < AstarPath[AstarPath.Count - 1].pos.y)
        {
            AstarPath.RemoveAt(AstarPath.Count - 1);
        }


        // Stop recording time
        stopwatch.Stop();

        if (AstarPath == null)
        {
            //UnityEngine.Debug.Log("Path not found");
            return;
        }

        foreach (NewPathNode pathNode in AstarPath)
        {

            int nodeNumber = pathNode.x * nodeMapSize + pathNode.y;

            nodes[nodeNumber].path = true;
            nodes[nodeNumber].estimatedSpeed = pathNode.estimatedSpeed;
            nodes[nodeNumber].nodeNumber = pathNode.nodeNumber;
            //UnityEngine.Debug.Log(nodes[nodeNumber].gameObject.name + " estimated speed: " + nodes[nodeNumber].estimatedSpeed);

            path.Add(nodes[nodeNumber]);
        }

        gameStateManager.startPos = path[0].position;

        SpawnPath();

        // Print the elapsed time in milliseconds
        UnityEngine.Debug.Log("Pathfinding took: " + stopwatch.ElapsedMilliseconds + " ms");
    }

    public void FindPath()
    {
        DeletePath();

        //pathfinding = new PathfindingRemastered(nodeMapSize, nodeMapSize, pathSegmentNum, slopeFunction, curvatureConstraint, curvatureCostMultiplier, moveCostOn);
        SetWalkableNodes(nodes);

        // Start recording time
        Stopwatch stopwatch = Stopwatch.StartNew();

        List<PathNode> AstarPath = pathfinding.FindPath(nodeMapSize / 2, nodeMapSize / 2, randomEndPoint[0], randomEndPoint[1]);

        // Stop recording time
        stopwatch.Stop();

        if (AstarPath == null)
        {
            //UnityEngine.Debug.Log("Path not found");
            return;
        }

        foreach (PathNode pathNode in AstarPath)
        {

            int nodeNumber = pathNode.x * nodeMapSize + pathNode.y;

            nodes[nodeNumber].path = true;

            path.Add(nodes[nodeNumber]);
        }

        SpawnPath();

        // Print the elapsed time in milliseconds
        UnityEngine.Debug.Log("Pathfinding took: " + stopwatch.ElapsedMilliseconds + " ms");
    }

    public void ReconstructSlopeFunction()
    {
        Keyframe[] ks = new Keyframe[3];

        float minKeyframeValue = preferredSlope - slopeThreshold;
        Mathf.Clamp(minKeyframeValue, 0.0f, preferredSlope);
        ks[0] = new Keyframe(minKeyframeValue, 2);
        ks[0].outTangent = 0f;

        ks[1] = new Keyframe(preferredSlope, 1);
        ks[1].inTangent = 0f;
        ks[1].outTangent = 0f;

        float maxKeyframeValue = preferredSlope + slopeThreshold;
        Mathf.Clamp(maxKeyframeValue, preferredSlope, maxAngle);
        ks[2] = new Keyframe(maxKeyframeValue, 2);
        ks[2].inTangent = 0f;

        slopeFunction = new AnimationCurve(ks);
    }

    private Node FindNodeWithCoordinates(int x, int y)
    {
        return nodes.Find(node => node.x == x && node.y == y);
    }

    private void SpawnPath()
    {

        List<Vector3> splineVector3 = new List<Vector3>();
        foreach (Node n in path)
        {
            splineVector3.Add(n.position);
        }

        splineGenerator.ConstructSpline(splineVector3, TangentMode.AutoSmooth);

        //UnityEngine.Debug.Log("Lenght: " + splineGenerator.splineContainer.Spline.GetLength());

        // Spawn nodes
        for (int i = 0; i < path.Count; i++)
        {
            string name = "path node #" + i.ToString();
            //GameObject nodeObject = new GameObject();
            GameObject pathNodeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pathNodeObject.name = name;

            // Set the GameObject's position to the square's position
            pathNodeObject.transform.position = path[i].position;

            // Use LookAt to align the local up vector with squareData.normal
            pathNodeObject.transform.LookAt(pathNodeObject.transform.position + path[i].normal);

            pathNodeObject.layer = 2;

            // Attach the Square component to the GameObject
            Node nodeComponent = pathNodeObject.AddComponent<Node>();
            nodeComponent.position = path[i].position;
            nodeComponent.corners = path[i].corners;
            nodeComponent.normal = path[i].normal;
            nodeComponent.wall = path[i].wall;
            nodeComponent.x = path[i].x;
            nodeComponent.y = path[i].y;
            nodeComponent.path = path[i].path;
            nodeComponent.estimatedSpeed = path[i].estimatedSpeed;
            nodeComponent.nodeNumber = path[i].nodeNumber;
            nodeComponent.board = gameStateManager.snowboard;

            pathNodeObject.GetComponent<Renderer>().material = pathMaterial;
            pathNodeObject.transform.parent = pathNodesHolder.transform;
            pathNodeObject.GetComponent<BoxCollider>().isTrigger = true;
            pathNodeObject.transform.localScale = new Vector3(1, 1, 1);

            pathNodeObject.SetActive(false);
        }

        // spawn fence alongside spline
        float increment = fenceDensity / splineGenerator.splineContainer.Spline.GetLength();

        for (float i  = 0; i < 1; i += increment)
        {
            // get position
            float3 pos = splineGenerator.splineContainer.Spline.EvaluatePosition(i);
            Vector3 convertedPos = new Vector3(pos.x, pos.y, pos.z);

            // get tangent
            float3 dir = splineGenerator.splineContainer.Spline.EvaluateTangent(i);
            Vector3 convertedDir = new Vector3(dir.x, dir.y, dir.z);

            // set orientation
            Quaternion orientation = new Quaternion();
            orientation.SetLookRotation(convertedDir);

            // get both sides
            Vector3 right = Quaternion.AngleAxis(90, Vector3.up) * convertedDir.normalized;
            Vector3 left = Quaternion.AngleAxis(-90, Vector3.up) * convertedDir.normalized;

            Vector3 rightFenceRaycast = convertedPos + right * pathWidth + Vector3.up * 100;
            Vector3 leftFenceRaycast = convertedPos + left * pathWidth + Vector3.up * 100;

            // spawn at the raycast hit pos
            RaycastHit rightHit;
            if (Physics.Raycast(rightFenceRaycast, Vector3.down, out rightHit) && rightHit.transform.tag != "Fence")
            {
                // spawn right side

                // select random fence

                int randFenceId = UnityEngine.Random.Range(0, fenceList.Count - 1);

                GameObject fence = Instantiate(fenceList[randFenceId], rightHit.point, orientation, fenceHolder.transform);
                fence.transform.Rotate(new Vector3(0, 90, 0), Space.Self);
            }

            RaycastHit leftHit;
            if (Physics.Raycast(leftFenceRaycast, Vector3.down, out leftHit) && leftHit.transform.tag != "Fence")
            {
                // spawn left side

                // select random fence

                int randFenceId = UnityEngine.Random.Range(0, fenceList.Count - 1);

                GameObject fence = Instantiate(fenceList[randFenceId], leftHit.point, orientation, fenceHolder.transform);
                fence.transform.Rotate(new Vector3(0, 90, 0), Space.Self);
            }
        }
    }

    public Node[] GenerateNodes(Vector3[] verts, int[] triangles, int mapSize)
    {
        DeleteNodes();

        int numNodes = (mapSize - 1) * (mapSize - 1);
        //int numSquares = (mapSize) * (mapSize);
        Node[] nodes = new Node[numNodes];

        for (int i = 0; i < numNodes; i++)
        {
            int startIndex = i * 6;
            Node node = new Node();

            node.corners = new Vector3[4];

            // Calculate the four corners of the square
            node.corners[0] = verts[triangles[startIndex + 2]];
            node.corners[1] = verts[triangles[startIndex + 4]];
            node.corners[2] = verts[triangles[startIndex + 1]];
            node.corners[3] = verts[triangles[startIndex + 0]];

            // Calculate the square's position as the average of its corners
            Vector3 nodePosition = Vector3.zero;
            for (int j = 0; j < 4; j++)
            {
                nodePosition += node.corners[j];
            }
            nodePosition /= 4;
            node.position = nodePosition;

            Vector3 rayCastStart = nodePosition + new Vector3(0, 30, 0);

            RaycastHit hitInfo = new RaycastHit();

            Physics.Raycast(rayCastStart, Vector3.down, out hitInfo, Mathf.Infinity);

            node.position = hitInfo.point;

            // Calculate the normal of the square
            Vector3 v0 = node.corners[1] - node.corners[0];
            Vector3 v1 = node.corners[2] - node.corners[0];
            node.normal = -Vector3.Cross(v0, v1).normalized;

            nodes[i] = node;

        }

        //// Calculate neighboring squares for each square
        //for (int x = 0; x < mapSize - 1; x++)
        //{
        //    for (int y = 0; y < mapSize - 1; y++)
        //    {
        //        int index = y * (mapSize - 1) + x;
        //        Node node = nodes[index];

        //        // Calculate neighbors for this square
        //        //square.neighbours = new Square[8];

        //        // Calculate neighbors in top row
        //        if (y > 0)
        //        {
        //            node.neighbours[0] = nodes[index - (mapSize - 1)];
        //            if (x < mapSize - 2)
        //                node.neighbours[1] = nodes[index - (mapSize - 1) + 1];
        //        }

        //        // Calculate neighbors in the middle row
        //        if (x > 0)
        //            node.neighbours[2] = nodes[index - 1];
        //        if (x < mapSize - 2)
        //            node.neighbours[3] = nodes[index + 1];

        //        // Calculate neighbors in the bottom row
        //        if (y < mapSize - 2)
        //        {
        //            if (x > 0)
        //                node.neighbours[4] = nodes[index + (mapSize - 1)];
        //            if (x < mapSize - 2)
        //                node.neighbours[5] = nodes[index + (mapSize - 1) + 1];
        //        }

        //        // Calculate diagonal neighbors
        //        if (x > 0 && y > 0)
        //            node.neighbours[6] = nodes[index - (mapSize - 1) - 1];
        //        if (x < mapSize - 2 && y > 0)
        //            node.neighbours[7] = nodes[index - (mapSize - 1) + 2];
        //    }
        //}

        return nodes;
    }

    public void SpawnNodes()
    {
        DeleteNodes();
        DeletePath();


        // Try to find the "Nodes" GameObject in the scene
        GameObject nodesObject = GameObject.Find("Nodes");

        // If it doesn't exist, create an empty GameObject named "Nodes"
        if (nodesObject == null)
        {
            nodesObject = new GameObject("Nodes");
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            string name = "Node #" + i.ToString();
            //GameObject nodeObject = new GameObject();
            GameObject nodeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nodeObject.name = name;

            // Set the GameObject's position to the square's position
            nodeObject.transform.position = nodes[i].position;

            // Use LookAt to align the local up vector with squareData.normal
            nodeObject.transform.LookAt(nodeObject.transform.position + nodes[i].normal);

            // Attach the Square component to the GameObject
            Node nodeComponent = nodeObject.AddComponent<Node>();
            nodeComponent.position = nodes[i].position;
            nodeComponent.corners = nodes[i].corners;
            nodeComponent.normal = nodes[i].normal;
            nodeComponent.wall = nodes[i].wall;
            nodeComponent.x = nodes[i].x;
            nodeComponent.y = nodes[i].y;
            nodeComponent.path = nodes[i].path;

            // Set squaresObject as the parent of squareObject
            nodeObject.transform.SetParent(nodesObject.transform);

            if (nodeComponent.wall)
            {
                nodeObject.GetComponent<Renderer>().material = wallMaterial;
            }
            else if (nodeComponent.path)
            {
                nodeObject.GetComponent<Renderer>().material = pathMaterial;
            }
            else
            {
                nodeObject.GetComponent<Renderer>().material = nodeMaterial;
            }
            DestroyImmediate(nodeObject.GetComponent<BoxCollider>());
            nodeObject.transform.localScale = new Vector3(5, 5, 5);
        }
    }

    public void DeleteNodes()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Node #"))
            {
                DestroyImmediate(obj);
            }
        }
    }

    public void DeletePath()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("path node #"))
            {
                DestroyImmediate(obj);
            }
        }

        path.Clear();
        splineGenerator.ClearSpline();

        // delete everything under fences
        DestroyChildrenOf(fenceHolder);

        // delete eveyrthing under stunts
        stuntPlacer.DeleteAllStunts();
    }

    void DestroyChildrenOf(GameObject parent)
    {
        int childCount = parent.transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;
            Destroy(child);
        }
    }

    void AssignMeshComponents()
    {
        // Find/create mesh holder object in children
        string meshHolderName = "Mesh Holder";
        Transform meshHolder = transform.Find(meshHolderName);
        if (meshHolder == null)
        {
            meshHolder = new GameObject(meshHolderName).transform;
            meshHolder.transform.parent = transform;
            meshHolder.transform.localPosition = Vector3.zero;
            meshHolder.transform.localRotation = Quaternion.identity;
        }

        // Ensure mesh renderer and filter components are assigned
        if (!meshHolder.gameObject.GetComponent<MeshFilter>())
        {
            meshHolder.gameObject.AddComponent<MeshFilter>();
        }
        if (!meshHolder.GetComponent<MeshRenderer>())
        {
            meshHolder.gameObject.AddComponent<MeshRenderer>();
        }
        if (!meshHolder.GetComponent<MeshCollider>())
        {
            meshHolder.gameObject.AddComponent<MeshCollider>();
        }

        meshRenderer = meshHolder.GetComponent<MeshRenderer>();
        meshFilter = meshHolder.GetComponent<MeshFilter>();
        meshCollider = meshHolder.GetComponent<MeshCollider>();
    }

    void AddBoxColliders()
    {

        boxColliders[0].center = new Vector3(scale - scale / 16, scale / 8, 0.0f);
        boxColliders[0].size = new Vector3(scale / 6, scale / 4, scale * 2);
        boxColliders[0].isTrigger = true;

        boxColliders[1].center = new Vector3(-scale + scale / 16, scale / 8, 0.0f);
        boxColliders[1].size = new Vector3(scale / 6, scale / 4, scale * 2);
        boxColliders[1].isTrigger = true;

        boxColliders[2].center = new Vector3(0.0f, scale / 8, scale - scale / 16);
        boxColliders[2].size = new Vector3(scale * 2, scale / 4, scale / 6);
        boxColliders[2].isTrigger = true;

        boxColliders[3].center = new Vector3(0.0f, scale / 8, -scale + scale / 16);
        boxColliders[3].size = new Vector3(scale * 2, scale / 4, scale / 6);
        boxColliders[3].isTrigger = true;

    }

}