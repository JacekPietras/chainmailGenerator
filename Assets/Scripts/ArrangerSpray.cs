using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrangerSpray : Arranger {
    private List<DynamicObject> objects;
    private List<DynamicObject> objectOriginals;
    private float speedFactor = 0.5f;
    [Range(1, 100)]
    public int size = 10;

    public override List<DynamicObject> getObjects() {
        if (objects != null) {
            return objects;
        }

        objects = new List<DynamicObject>();
        objectOriginals = new List<DynamicObject>();
        for (float i = 0.5f; i < size; i++) {
            for (float j = 0.5f; j < size; j++) {
                DynamicObject obj = new DynamicObject(
                    i / (float)size,
                    j / (float)size,
                    Color.white);
                objects.Add(obj);

                DynamicObject original = new DynamicObject(
                    i / (float)size,
                    j / (float)size,
                    Color.white);
                objectOriginals.Add(original);
            }
        }
        return objects;
    }

    void Update() {
        for (int i = 0; i < objects.Count; i++) {
            DynamicObject obj = objects[i];
            DynamicObject original = objectOriginals[i];

            obj.x = original.x + Mathf.Sin(Time.realtimeSinceStartup * speedFactor) /15f;
            obj.y = original.y + Mathf.Cos(Time.realtimeSinceStartup * speedFactor) /15f;
            while (obj.x > 1) {
                obj.x -= 1;
            }
            while (obj.y > 1) {
                obj.y -= 1;
            }
            while (obj.x < 0) {
                obj.x += 1;
            }
            while (obj.y < 0) {
                obj.y += 1;
            }

           // if (i == 0) {
          //      Debug.Log(obj.x + " " + obj.y);
           // }
        }
    }

    public override bool isDynamic() {
        return true;
    }
}
