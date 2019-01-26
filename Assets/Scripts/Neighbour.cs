using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Neighbour {
    public int[] index;
    public int[] triangles;
    public int[] verticles;
    public Edge[] edges;

    public Neighbour(List<int> index, List<int> triangles, List<int> verticles, List<Edge> edges) {
        this.index = index.ToArray();
        this.triangles = triangles.ToArray();
        this.verticles = verticles.ToArray();
        this.edges = edges.ToArray();
    }
}
