using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshFlat
{
    private const float NORMALIZATION_STRENGTH = 0.5f;

    public Vector3[] vertices;
    public int[] triangles;
    public Edge[] edges;

    public void rotateAndFlattenMesh()
    {
        Vector3 cross = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);
        Quaternion qAngle = Quaternion.LookRotation(cross);

        //Debug.Log("(" + p.p1.x + ", " + p.p1.y + ", " + p.p1.z + ") (" + p.p2.x + ", " + p.p2.y + ", " + p.p2.z + ") (" + p.p3.x + ", " + p.p3.y + ", " + p.p3.z + ")");

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = qAngle * vertices[i];
            vertices[i].z = 0;
        }
    }

    public void normalizeFlatMesh(int times)
    {
        for (int i = 0; i < times; i++)
            normalizeFlatMesh();
    }

    public void normalizeFlatMesh()
    {
        foreach (Edge edge in edges)
        {
            Vector3 move = vertices[edge.to] - vertices[edge.from];
            float currentLength = Mathf.Abs(move.magnitude);
            float wantedLength;
            if (currentLength == 0)
            {
                // vector to outside of triangle
                move =
                     -(vertices[0] - vertices[edge.from]
                     + vertices[1] - vertices[edge.from]
                     + vertices[2] - vertices[edge.from]);
                currentLength = Mathf.Abs(move.magnitude);
                wantedLength = edge.length;
            }
            else
            {
                wantedLength = currentLength + (edge.length - currentLength) * NORMALIZATION_STRENGTH;
            }
            vertices[edge.to] = vertices[edge.from] + move * (wantedLength / currentLength);
        }
    }

    public void fillEdgeLength()
    {
        foreach (Edge edge in edges)
        {
            edge.length = Mathf.Abs(Vector3.Distance(vertices[edge.from], vertices[edge.to]));
        }
    }

    public void setCenter(Vector3 center)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= center;
        }
    }
}
