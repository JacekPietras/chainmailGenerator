using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Neighbour {
    public int[] index;
    public int[] triangles;
    public int[] verticles;
    public List<int> usedTriangles;

    public Neighbour(List<int> index, List<int> triangles, List<int> verticles) {
        this.index = index.ToArray();
        this.triangles = triangles.ToArray();
        this.verticles = verticles.ToArray();
    }

    public void setUsedTriangles(List<int> usedTriangles) {
        this.usedTriangles = usedTriangles;
    }
}
