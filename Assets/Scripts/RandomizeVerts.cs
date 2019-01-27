using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeVerts : MonoBehaviour {
    [Range(0.01f, 0.1f)]
    public float skewFactor = 0.05f;
    [Range(0f, 2.0f)]
    public float speedFactor = 1f;
    [Range(0, Mathf.PI / 2)]
    public float seed = 0;
    private Vector3[] orginalVertices;
    private Vector3[] sinFactors;


    void Start() {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        orginalVertices = (Vector3[])mesh.vertices.Clone();
        sinFactors = new Vector3[orginalVertices.Length];

        int i = 0;
        while (i < orginalVertices.Length) {
            sinFactors[i].x = Random.Range(-1, 1);
            sinFactors[i].y = Random.Range(-1, 1);
            sinFactors[i].z = Random.Range(-1, 1);

            int j = 0;
            while (j < i) {
                if (orginalVertices[j] == orginalVertices[i]) {
                    sinFactors[i] = sinFactors[j];
                    break;
                }
                j++;
            }
            i++;
        }
    }

    void Update() {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        float mult = Mathf.Sin((seed == 0 ? Time.realtimeSinceStartup : seed) * speedFactor);
        //Debug.Log(mult+" "+ Time.realtimeSinceStartup);

        int i = 0;
        while (i < vertices.Length) {
            vertices[i] = orginalVertices[i] + sinFactors[i] * skewFactor * mult;
            i++;
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }
}
