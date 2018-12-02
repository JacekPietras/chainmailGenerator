using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangle3D {
    public Vector3 p1 = new Vector3(0, 0, 0);
    public Vector3 p2 = new Vector3(0, 0, 0);
    public Vector3 p3 = new Vector3(0, 0, 0);

    public Triangle3D(Vector3 p1, Vector3 p2, Vector3 p3) {
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;
    }

    // creation of planar triangle started in point (0,0) and longest edge on X axis
    // from 3D triangle in 3D space
    public Triangle2D planar() {
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

    public Triangle2D planar2() {
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

    public Triangle2D planar3() {
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

    public bool isNeighbour(Triangle3D p) {
        return (p1 == p.p1 || p1 == p.p2 || p1 == p.p3 || p2 == p.p1 || p2 == p.p2 || p2 == p.p3 || p3 == p.p1 || p3 == p.p2 || p3 == p.p3);
    }

    public void print(string prefix = "triangle") {
        if (p1.z != 0 || p2.z != 0 || p3.z != 0)
            Debug.Log(prefix + " Z axis not zero !!!!!!!!!!!!");
        Debug.Log(prefix + " (" + p1.x + ", " + p1.y + ") (" + p2.x + ", " + p2.y + ") (" + p3.x + ", " + p3.y + ")");
    }

    public bool isOnTexture2() {
        return isOnTexture(p1) || isOnTexture(p2) || isOnTexture(p3) || pointInside(0, 0) || pointInside(0, 1) || pointInside(1, 0) || pointInside(1, 1);
    }

    // some lines have intersection, or at least one point from triangle is in rect, or at least one point of rect is in triangle
    public bool isOnTexture() {
        Vector2 a = new Vector2(0,0);
        Vector2 b = new Vector2(0,1);
        Vector2 c = new Vector2(1,0);
        Vector2 d = new Vector2(1,1);

        return isOnTexture(p1) || pointInside(0, 0) 
            || intersection(p1,p2, a,b)
            || intersection(p1, p2, a, c)
            || intersection(p1, p2, b, d)
            || intersection(p1, p2, c, d)

            || intersection(p1, p3, a, b)
            || intersection(p1, p3, a, c)
            || intersection(p1, p3, b, d)
            || intersection(p1, p3, c, d)

            || intersection(p2, p3, a, b)
            || intersection(p2, p3, a, c)
            || intersection(p2, p3, b, d)
            || intersection(p2, p3, c, d)
            ;
    }

    private bool intersection2(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4) {
        return intersection(p1.x, p1.y, p2.x, p2.y, p3.x, p3.y, p4.x, p4.y);
    }

    static bool intersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) {
        Vector2 b = a2 - a1;
        Vector2 d = b2 - b1;
        float bDotDPerp = b.x * d.y - b.y * d.x;

        // if b dot d == 0, it means the lines are parallel so have infinite intersection points
        if (bDotDPerp == 0)
            return false;

        Vector2 c = b1 - a1;
        float t = (c.x * d.y - c.y * d.x) / bDotDPerp;
        if (t < 0 || t > 1)
            return false;

        float u = (c.x * b.y - c.y * b.x) / bDotDPerp;
        if (u < 0 || u > 1)
            return false;

        return true;
    }

    private bool intersection(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) {
        float x12 = x1 - x2;
        float x34 = x3 - x4;
        float y12 = y1 - y2;
        float y34 = y3 - y4;

        float c = x12 * y34 - y12 * x34;

        return Mathf.Abs(c) >= 0.01;
    }

    private bool isOnTexture(Vector3 p) {
        return (p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1);
    }

    private bool pointInside(float x, float y) {
        return pointInside(x, y, p1, p2, p3);
    }

    private bool pointInside(float x, float y, Vector3 a, Vector3 b, Vector3 c) {
        float as_x = x - a.x;
        float as_y = y - a.y;

        bool s_ab = (b.x - a.x) * as_y - (b.y - a.y) * as_x > 0;

        if ((c.x - a.x) * as_y - (c.y - a.y) * as_x > 0 == s_ab)
            return false;

        if ((c.x - b.x) * (y - b.y) - (c.y - b.y) * (x - b.x) > 0 != s_ab)
            return false;

        return true;
    }
}
