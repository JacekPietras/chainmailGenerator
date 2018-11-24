using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge
{
    public int from;
    public int to;
    public float length;

    public Edge(int from, int to)
    {
        this.from = from;
        this.to = to;
    }
}
