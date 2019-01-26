using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge {
    public int from;
    public int to;
    public float length;
    public float strength;

    public Edge(int from, int to, float strength) {
        this.from = from;
        this.to = to;
        this.strength = strength;
    }
}
