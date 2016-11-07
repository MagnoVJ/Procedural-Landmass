using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor {

    public override void OnInspectorGUI() {

        MapGenerator mapGen = target as MapGenerator;

        if (DrawDefaultInspector())
            if (mapGen.autoUpdate)
                mapGen.DrawMapInEditor();

        if (GUILayout.Button("Generate"))
            mapGen.DrawMapInEditor();


    }

}