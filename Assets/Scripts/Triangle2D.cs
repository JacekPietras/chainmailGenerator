using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Triangle2D
{
    public Vector2 p1 = new Vector2(0, 0);
    public Vector2 p2 = new Vector2(0, 0);
    public Vector2 p3 = new Vector2(0, 0);

    public Triangle2D(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;
    }

    public bool pointInside(Vector2 s)
    {
        return pointInside(s, p1, p2, p3);
    }

    public static bool pointInside(Vector2 s, Triangle2D triangle2d)
    {
        return pointInside(s, triangle2d.p1, triangle2d.p2, triangle2d.p3);
    }

    public static bool pointInside(Vector2 s, Vector2 a, Vector2 b, Vector2 c)
    {
        float as_x = s.x - a.x;
        float as_y = s.y - a.y;

        bool s_ab = (b.x - a.x) * as_y - (b.y - a.y) * as_x > 0;

        if ((c.x - a.x) * as_y - (c.y - a.y) * as_x > 0 == s_ab)
            return false;

        if ((c.x - b.x) * (s.y - b.y) - (c.y - b.y) * (s.x - b.x) > 0 != s_ab)
            return false;

        return true;
    }

    public Vector2[] toArray()
    {
        return new Vector2[] {
            p1,
            p2,
            p3
        };
    }

    public Vector3[] toArray3()
    {
        return new Vector3[] {
            new Vector3 (p1.x, p1.y),
            new Vector3 (p2.x, p2.y),
            new Vector3 (p3.x, p3.y)
        };
    }

    // creates a triangle with normalized points to square x=0..1 y=0..1
    public Triangle2D normalize()
    {
        Triangle2D a = new Triangle2D(
                           new Vector2(p1.x, p1.y),
                           new Vector2(p2.x, p2.y),
                           new Vector2(p3.x, p3.y));

        float minX = a.p1.x, minY = a.p1.y;
        if (a.p2.x < minX)
            minX = a.p2.x;
        if (a.p3.x < minX)
            minX = a.p3.x;
        if (a.p2.y < minY)
            minY = a.p2.y;
        if (a.p3.y < minY)
            minY = a.p3.y;

        //moving to center of coordinate system
        a.p1.x -= minX;
        a.p2.x -= minX;
        a.p3.x -= minX;
        a.p1.y -= minY;
        a.p2.y -= minY;
        a.p3.y -= minY;

        float farthest = p1.x;
        if (a.p2.x > farthest)
            farthest = a.p2.x;
        if (a.p3.x > farthest)
            farthest = a.p3.x;
        if (a.p1.y > farthest)
            farthest = a.p2.y;
        if (a.p2.y > farthest)
            farthest = a.p2.y;
        if (a.p3.y > farthest)
            farthest = a.p3.y;

        //scaling to values 0..1
        a.p1.x /= farthest;
        a.p2.x /= farthest;
        a.p3.x /= farthest;
        a.p1.y /= farthest;
        a.p2.y /= farthest;
        a.p3.y /= farthest;

        return a;
    }

    public void applyScale(float scale)
    {
        p1 *= scale;
        p2 *= scale;
        p3 *= scale;
    }

    public void applyTranslation(float Tx, float Ty)
    {
        p1.x += Tx;
        p2.x += Tx;
        p3.x += Tx;
        p1.y += Ty;
        p2.y += Ty;
        p3.y += Ty;
    }

    public void rotate(float angle, Vector2 center)
    {
        p1 = rotatePoint(p1, center, angle);
        p2 = rotatePoint(p2, center, angle);
        p3 = rotatePoint(p3, center, angle);
    }

    private Vector2 rotatePoint(Vector2 pointToRotate, Vector2 centerPoint, float angleInDegrees)
    {
        float angleInRadians = angleInDegrees * 360 * (Mathf.PI / 180);
        float cosTheta = Mathf.Cos(angleInRadians);
        float sinTheta = Mathf.Sin(angleInRadians);
        return new Vector2(
                (cosTheta * (pointToRotate.x - centerPoint.x) - sinTheta * (pointToRotate.y - centerPoint.y) + centerPoint.x),
                (sinTheta * (pointToRotate.x - centerPoint.x) + cosTheta * (pointToRotate.y - centerPoint.y) + centerPoint.y)
        );
    }
}
