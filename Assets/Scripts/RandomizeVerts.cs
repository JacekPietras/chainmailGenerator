using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeVerts : MonoBehaviour
{
    public float skewFactor = 0.05f;
    private Vector3[] orginalVertices;
    private Vector3[] sinFactors;

    
    void Start ()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        orginalVertices = (Vector3[])mesh.vertices.Clone();
        sinFactors = new Vector3[orginalVertices.Length];
     
        int i = 0;
        while (i < orginalVertices.Length)
        {
            sinFactors[i].x = Random.Range(-skewFactor, skewFactor);
            sinFactors[i].y = Random.Range(-skewFactor, skewFactor);
            sinFactors[i].z = Random.Range(-skewFactor, skewFactor);

            int j = 0;
            while (j < i)
            {
                if (orginalVertices[j] == orginalVertices[i])
                {
                    sinFactors[i] = sinFactors[j];
                    break;
                }
                j++;
            }
            i++;
        }
    }

    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        float mult = Mathf.Sin(Time.realtimeSinceStartup);
        //Debug.Log(mult+" "+ Time.realtimeSinceStartup);

        int i = 0;
        while (i < vertices.Length)
        {
            vertices[i] = orginalVertices[i] + sinFactors[i] * mult;
            i++;
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }
}
