using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshFlat {
    public float NORMALIZATION_STRENGTH = 0.8f;
    private Vector3 CENTER = new Vector3(.5f, .5f, 0);

    public Vector3[] vertices;
    public int[] triangles;
    public List<Edge> edges;
    private bool[,] edgeConnections;
    private Vector3 crossMain;
    private Triangle2D motherTriangle;

    public MeshFlat(Mesh mesh3d, Neighbour neighbour, float normalizationStrength) {
        vertices = new Vector3[neighbour.verticles.Length];
        for (int j = 0; j < vertices.Length; j++) {
            vertices[j] = mesh3d.vertices[neighbour.verticles[j]];
        }

        NORMALIZATION_STRENGTH = normalizationStrength;
        triangles = neighbour.triangles;
        rotateMesh();

        crossMain = getCross(0);
    }

    public void makeEdges(List<int> usedTriangles) {
        edgeConnections = new bool[triangles.Length, triangles.Length];
        edges = new List<Edge>();

        foreach (int k in usedTriangles) {
            addEdges(k,
                triangles[k + 0],
                triangles[k + 1],
                triangles[k + 2]);
        }
        
        edges.Sort((x, y) => y.strength.CompareTo(x.strength));

        // filling edge expected length with knowledge of current 3D object
        // (it's length from 3D, not flattened. But in best scenario
        // we want flattened edge to have same length as one from 3D)
        fillEdgeLength();
        flattenMesh();
    }

    // filling list of edges that will be used to normalization of triangles
    // list won't contain for example edges in main triangle, because we don't wanna to distort him
    public void addEdges(int i, params int[] indexOfNP) {
        // we need to iterate through indexOfNP (3 elements)
        for (int k = 0; k < indexOfNP.Length; k++) {
            // index need to be less than 3 because indexes 0,1,2 are for mother triangle
            if (indexOfNP[k] >= 3) {
                // we need to iterate through indexOfNP again
                // but choose all point that are not current k
                for (int j = 0; j < indexOfNP.Length; j++) {
                    if (k != j) {
                        // checking if that edge already exist
                        if (!edgeConnections[indexOfNP[j], indexOfNP[k]]) {
                            // not existing, we need to create that edge from j to k
                            // and mark that edge as created
                            // notice that j->k is different than k->j
                            edges.Add(new Edge(indexOfNP[j], indexOfNP[k], getStrength(i)));

                            Debug.Log("adding edge " + indexOfNP[j] + " " + indexOfNP[k]+" s"+ getStrength(i));

                            edgeConnections[indexOfNP[j], indexOfNP[k]] = true;
                        }
                    }
                }
            }
        }
    }

    private Vector3 getCross(int k) {
        return Vector3.Cross(
            vertices[triangles[k + 1]] - vertices[triangles[k + 0]],
            vertices[triangles[k + 2]] - vertices[triangles[k + 0]]);
    }

    // returns variable [0..1] that is saying how much that triangle 
    // is pararell to triangle with current object
    private float getStrength(int i) {
        Vector3 cross = getCross(i);
        float strength = 1 - Vector3.Angle(crossMain, cross) / 180;
        // we need to make ones close to 1 more important 
        return strength * strength;
    }

    public void rotateMesh() {
        Vector3[] cross = new Vector3[triangles.Length / 3];
        for (int k = 0; k < triangles.Length; k += 3) {
            cross[k / 3] = Vector3.Cross(vertices[triangles[k + 1]] - vertices[triangles[k + 0]], vertices[triangles[k + 2]] - vertices[triangles[k + 0]]);
        }

        //Debug.Log("(" + p.p1.x + ", " + p.p1.y + ", " + p.p1.z + ") (" + p.p2.x + ", " + p.p2.y + ", " + p.p2.z + ") (" + p.p3.x + ", " + p.p3.y + ", " + p.p3.z + ")");

        Quaternion qAngle = Quaternion.LookRotation(cross[0]);

        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] = qAngle * vertices[i];
        }
    }

    public void flattenMesh() {
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i].z = 0;
        }

        motherTriangle = new Triangle2D(vertices[triangles[0]], vertices[triangles[1]], vertices[triangles[2]]);
    }

    public void normalizeFlatMesh(int times) {
        while (times > 0 && separateOverLappingVerticles()) {
            times--;
        }
        for (int i = 0; i < times; i++) {
            separateOverLappingFaces();
            normalizeFlatMesh();
        }
    }

    public bool separateOverLappingVerticles() {
        bool separated = false;
        foreach (Edge edge in edges) {
            Vector3 move = vertices[edge.to] - vertices[edge.from];
            if (move.magnitude == 0) {
                // vector to outside of triangle
                move =
                     -(vertices[0] - vertices[edge.from]
                     + vertices[1] - vertices[edge.from]
                     + vertices[2] - vertices[edge.from]);
                float currentLength = Mathf.Abs(move.magnitude);
                float wantedLength = currentLength + (edge.length - currentLength) * NORMALIZATION_STRENGTH * edge.strength;
                vertices[edge.to] = vertices[edge.from] + move * (wantedLength / currentLength);
                separated = true;
            }
        }

        return separated;
    }

    public void separateOverLappingFaces() {
        foreach (Edge edge in edges) {
            if (motherTriangle.pointInside(vertices[edge.to])) {
                // vector to outside of triangle
                Vector3 move =
                     -(vertices[0] - vertices[edge.from]
                     + vertices[1] - vertices[edge.from]
                     + vertices[2] - vertices[edge.from]);
                float currentLength = Mathf.Abs(move.magnitude);
                float wantedLength = currentLength + (edge.length - currentLength) * NORMALIZATION_STRENGTH * edge.strength;
                vertices[edge.to] = vertices[edge.from] + move * (wantedLength / currentLength);
            }
        }
    }

    public void normalizeFlatMesh() {
        foreach (Edge edge in edges) {
            Vector3 move = vertices[edge.to] - vertices[edge.from];
            float currentLength = Mathf.Abs(move.magnitude);
            float wantedLength = currentLength + (edge.length - currentLength) * NORMALIZATION_STRENGTH * edge.strength;
            vertices[edge.to] = vertices[edge.from] + move * (wantedLength / currentLength);
        }
    }

    public void fillEdgeLength() {
        foreach (Edge edge in edges) {
            edge.length = Mathf.Abs(Vector3.Distance(vertices[edge.from], vertices[edge.to]));
        }
    }

    public void setCenter(Vector3 center) {
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] -= center;
        }
    }

    public Vector3[] getTransformedByObject(TextureObject obj) {
        // that's interpolated center of ring on planar 3d triangle
        Vector3 interpolated = obj.barycentric.Interpolate(vertices[0], vertices[1], vertices[2]);
        Vector3[] transformedVerticles = new Vector3[vertices.Length];

        for (int k = 0; k < vertices.Length; k++) {
            // moving to center of coords
            Vector3 transformed = vertices[k] - interpolated;
            // scaling 
            transformed *= (1 / obj.scale);
            // rotation
            transformed = rotatePoint(transformed, obj.rotation);
            // setting center as center of bitmap
            transformed += CENTER;

            transformedVerticles[k] = transformed;
        }

        return transformedVerticles;
    }

    private Vector3 rotatePoint(Vector3 pointToRotate, Vector3 centerPoint, float angleInDegrees) {
        float angleInRadians = angleInDegrees * 360 * (Mathf.PI / 180);
        float cosTheta = Mathf.Cos(angleInRadians);
        float sinTheta = Mathf.Sin(angleInRadians);
        return new Vector3(
                (cosTheta * (pointToRotate.x - centerPoint.x) - sinTheta * (pointToRotate.y - centerPoint.y) + centerPoint.x),
                (sinTheta * (pointToRotate.x - centerPoint.x) + cosTheta * (pointToRotate.y - centerPoint.y) + centerPoint.y),
                pointToRotate.z
        );
    }

    private Vector3 rotatePoint(Vector3 pointToRotate, float angleInDegrees) {
        float angleInRadians = angleInDegrees * 360 * (Mathf.PI / 180);
        float cosTheta = Mathf.Cos(angleInRadians);
        float sinTheta = Mathf.Sin(angleInRadians);
        return new Vector3(
                (cosTheta * (pointToRotate.x) - sinTheta * (pointToRotate.y)),
                (sinTheta * (pointToRotate.x) + cosTheta * (pointToRotate.y)),
                pointToRotate.z
        );
    }
}
