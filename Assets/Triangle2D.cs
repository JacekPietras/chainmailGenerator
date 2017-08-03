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

        if ((c.x - a.x) * as_y - (c.y - a.y) * as_x > 0 == s_ab) return false;

        if ((c.x - b.x) * (s.y - b.y) - (c.y - b.y) * (s.x - b.x) > 0 != s_ab) return false;

        return true;
    }
}
