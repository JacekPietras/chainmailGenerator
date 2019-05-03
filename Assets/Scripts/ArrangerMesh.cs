using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ArrangerMesh : Arranger {
    private List<DynamicObject> objects;
    private List<DynamicObject> objectOriginals;
    [Range(1, 100)]
    public int size = 10;

    public override List<DynamicObject> getObjects() {
        if (objects != null) {
            return objects;
        }

        objects = new List<DynamicObject>();
        for (float i = 0.5f; i < size; i++) {
            for (float j = 0.5f; j < size; j++) {
                DynamicObject obj = new DynamicObject(
                    i / (float)size,
                    j / (float)size,
                    Color.white);
                Debug.Log("Found object (" + obj.x + ", " + obj.y + ")");
                objects.Add(obj);
            }
        }
        Debug.Log("Found "+ objects.Count + " objects");
        return objects;
    }
}
