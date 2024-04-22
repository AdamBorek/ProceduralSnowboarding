using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGenerator))]
public class MeshEditor : Editor
{

    TerrainGenerator terrainGenerator;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Regenerate Slope Function"))
        {
            terrainGenerator.ReconstructSlopeFunction();
        }

        if (GUILayout.Button("Generate Noise Mesh"))
        {
            terrainGenerator.GenerateHeightMap();
            terrainGenerator.ConstructNoiseMesh();
        }

        if (GUILayout.Button("Generate Mountain Base Mesh"))
        {
            terrainGenerator.GenerateMountainBase();
            terrainGenerator.ConstructBaseMesh();
        }

        if (GUILayout.Button("Generate Mountainside Mesh"))
        {
            terrainGenerator.GenerateMountainSide();
            terrainGenerator.ConstructSideMesh();
        }

        if (GUILayout.Button("Generate Combined Mesh"))
        {
            terrainGenerator.GenerateEntireMountain();
            terrainGenerator.ConstructCombinedMesh();
        }

        if (GUILayout.Button("Generate Nodes"))
        {
            //terrainGenerator.ConstructNodes();
            terrainGenerator.ConstructNodesAlternative();
        }

        //if (GUILayout.Button("Generate Everything!"))
        //{
        //    terrainGenerator.GenerateEntireMountain();
        //    terrainGenerator.ConstructCombinedMesh();
        //    terrainGenerator.ConstructNodes();

        //}
    }

    void OnEnable()
    {
        terrainGenerator = (TerrainGenerator)target;
        Tools.hidden = true;
    }

    void OnDisable()
    {
        Tools.hidden = false;
    }
}