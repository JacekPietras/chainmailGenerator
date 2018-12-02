using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangle3D
{
    public Vector3 p1 = new Vector3(0, 0, 0);
    public Vector3 p2 = new Vector3(0, 0, 0);
    public Vector3 p3 = new Vector3(0, 0, 0);

    public Triangle3D(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;
    }

    // creation of planar triangle started in point (0,0) and longest edge on X axis
    // from 3D triangle in 3D space
    public Triangle2D planar()
    {
        float a = Vector3.Distance(p1, p2);
        float b = Vector3.Distance(p2, p3);
        float c = Vector3.Distance(p1, p3);

        float scale = 1 / Mathf.Max(a, b, c);
        float angle = Mathf.Deg2Rad * Vector3.Angle(new Vector3(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z), new Vector3(p3.x - p1.x, p3.y - p1.y, p3.z - p1.z));

        return
            new Triangle2D(
            new Vector2(0, 0),
            new Vector2(0, a * scale),
            new Vector2(Mathf.Sin(angle) * c * scale, Mathf.Cos(angle) * c * scale));
    }

    public Triangle2D planar2()
    {
        float x1p = Mathf.Sqrt((p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y) + (p2.z - p1.z) * (p2.z - p1.z));
        float y1p = 0;

        float x2p = ((p2.x - p1.x) * (p3.x - p1.x) + (p2.y - p1.y) * (p3.y - p1.y) + (p2.z - p1.z) * (p3.z - p1.z)) / x1p;
        float y2p = Mathf.Sqrt((p3.x - p1.x) * (p3.x - p1.x) + (p3.y - p1.y) * (p3.y - p1.y) + (p3.z - p1.z) * (p3.z - p1.z) - x2p * x2p);

        return
        new Triangle2D(
            new Vector2(0, 0),
            new Vector2(x1p, y1p),
            new Vector2(x2p, y2p));
    }

    public Triangle2D planar3()
    {
        float a = Vector3.Distance(p1, p2);
        float b = Vector3.Distance(p2, p3);
        float c = Vector3.Distance(p1, p3);

        float scale = 1 / Mathf.Max(a, b, c);
        float angle = Mathf.Deg2Rad * Vector3.Angle(new Vector3(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z), new Vector3(p3.x - p1.x, p3.y - p1.y, p3.z - p1.z));

        return
            new Triangle2D(
            new Vector2(0, 0),
            new Vector2(a, 0),
            new Vector2(Mathf.Sin(angle) * c * scale, Mathf.Cos(angle) * c * scale));
    }

    public bool isNeighbour(Triangle3D p)
    {
        return (p1 == p.p1 || p1 == p.p2 || p1 == p.p3 || p2 == p.p1 || p2 == p.p2 || p2 == p.p3 || p3 == p.p1 || p3 == p.p2 || p3 == p.p3);
    }

    public void print(string prefix = "triangle")
    {
        if (p1.z != 0 || p2.z != 0 || p3.z != 0)
            Debug.Log(prefix + " Z axis not zero !!!!!!!!!!!!");
        Debug.Log(prefix + " (" + p1.x + ", " + p1.y + ") (" + p2.x + ", " + p2.y + ") (" + p3.x + ", " + p3.y + ")");
    }
}
