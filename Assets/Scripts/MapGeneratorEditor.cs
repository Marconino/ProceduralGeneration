using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapGenerator = (MapGenerator)target;
        DrawDefaultInspector();

        if (GUILayout.Button("Generate"))
        {
            if (mapGenerator.transform.childCount > 0)
                DeleteChildren(mapGenerator);

            mapGenerator.Generate();
        }

        if (GUILayout.Button("Delete children"))
        {
            DeleteChildren(mapGenerator);
        }
    }

    void DeleteChildren(MapGenerator _mapGenerator)
    {
        _mapGenerator.GetNoiseGenerator().ClearOldNoises();
        _mapGenerator.GetMarchingCubesGenerator().ClearOldMC();

        Transform[] children = _mapGenerator.transform.Cast<Transform>().ToArray();
        foreach (Transform child in children)
        {
            DestroyImmediate(child.gameObject);
        }
    }
}
