using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Neighbour
{
    public int[] index;
    public int[] triangles;
    public int[] verticles;
    public Edge[] edges;

    public Neighbour(List<int> index, List<int> tri, List<int> triVert, List<Edge> edges)
    {
        this.index = index.ToArray();
        this.triangles = tri.ToArray();
        this.verticles = triVert.ToArray();
        this.edges = edges.ToArray();
    }
}
