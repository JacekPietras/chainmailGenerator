using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshFlat {
    public float NORMALIZATION_STRENGTH = 0.8f;
    private Vector3 CENTER = new Vector3(.5f, .5f, 0);

    public Vector3[] vertices;
    public int[] triangles;
    public Edge[] edges;

    public void rotateAndFlattenMesh() {
        Vector3 cross = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);
        Quaternion qAngle = Quaternion.LookRotation(cross);

        //Debug.Log("(" + p.p1.x + ", " + p.p1.y + ", " + p.p1.z + ") (" + p.p2.x + ", " + p.p2.y + ", " + p.p2.z + ") (" + p.p3.x + ", " + p.p3.y + ", " + p.p3.z + ")");

        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] = qAngle * vertices[i];
            vertices[i].z = 0;
        }
    }

    public void normalizeFlatMesh(int times) {
        if (times > 0 && separateOverLappingVerticles()) {
            times--;
        }
        for (int i = 0; i < times; i++)
            normalizeFlatMesh();
    }

    public bool separateOverLappingVerticles() {
        bool separated = false;
        foreach (Edge edge in edges) {
            Vector3 move = vertices[edge.to] - vertices[edge.from];
            float currentLength = Mathf.Abs(move.magnitude);
            if (currentLength == 0) {
                // vector to outside of triangle
                move =
                     -(vertices[0] - vertices[edge.from]
                     + vertices[1] - vertices[edge.from]
                     + vertices[2] - vertices[edge.from]);
                currentLength = Mathf.Abs(move.magnitude);
                float wantedLength = currentLength + (edge.length - currentLength) * NORMALIZATION_STRENGTH;
                vertices[edge.to] = vertices[edge.from] + move * (wantedLength / currentLength);
                separated = true;
            }
        }

        return separated;
    }

    public void normalizeFlatMesh() {
        foreach (Edge edge in edges) {
            Vector3 move = vertices[edge.to] - vertices[edge.from];
            float currentLength = Mathf.Abs(move.magnitude);
            float wantedLength = currentLength + (edge.length - currentLength) * NORMALIZATION_STRENGTH;
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
