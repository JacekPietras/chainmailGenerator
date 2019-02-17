using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Neighbour {
    // indexes of verticles in 3D object
    public int[] index;
    public int[] triangles;
    public int[] verticles;
    public List<int> usedTriangles;
    public List<DynamicObject> objects = new List<DynamicObject>();

    public Neighbour(List<int> index, List<int> triangles, List<int> verticles) {
        this.index = index.ToArray();
        this.triangles = triangles.ToArray();
        this.verticles = verticles.ToArray();
    }

    public void setUsedTriangles(List<int> usedTriangles) {
        this.usedTriangles = usedTriangles;
    }
}
