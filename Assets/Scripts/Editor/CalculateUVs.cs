using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Script for calculating UVs for a selected mesh
/// </summary>
public class CalculateUVs : ScriptableWizard
{
    [MenuItem("Tools/Calculate UVs")]
    private static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<CalculateUVs>("Calculate UVs", "Calculate");
    }
    
    private void OnWizardCreate()
    {
        try
        {
            Transform[] transforms = Selection.transforms;

            foreach (Transform transform in transforms)
            {
                GameObject go = transform.gameObject;

                if (go == null) continue;

                Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
                Vector3[] vertices = mesh.vertices;
                Vector2[] uvs = new Vector2[vertices.Length];

                for (int i = 0; i < uvs.Length; i++)
                {
                    uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
                }

                mesh.uv = uvs;
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }
}