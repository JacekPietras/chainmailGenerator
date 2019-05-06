using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ArrangerMesh : Arranger {
    private List<DynamicObject> objects;
    private List<DynamicObject> objectOriginals;
    [Range(1, 100)]
    public int sizeX = 10;
    [Range(1, 100)]
    public int sizeY = 10;
    [Range(0, 1)]
    public float shiftX = 0;
    [Range(0, 1)]
    public float shiftY = 0;
    [Range(0, 0.1f)]
    public float paddingX = 0;
    [Range(0, 0.1f)]
    public float paddingY = 0;

    public override List<DynamicObject> getObjects() {
        if (objects != null) {
            return objects;
        }

        objects = new List<DynamicObject>();
        for (float i = shiftX; i < sizeX; i++) {
            for (float j = shiftY; j < sizeY; j++) {
                DynamicObject obj = new DynamicObject(
                    paddingX + (i / (float)sizeX) * (1 - paddingX * 2),
                    paddingY + (j / (float)sizeY) * (1 - paddingY * 2),
                    Color.white);
                //Debug.Log("Found object (" + obj.x + ", " + obj.y + ")");
                objects.Add(obj);
            }
        }
        Debug.Log("Found " + objects.Count + " objects");
        return objects;
    }
}
