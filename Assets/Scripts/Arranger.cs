using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

public class Arranger : MonoBehaviour {
    // returns list of texture objects on inputed texture
    // they have relative position, rotation, scale
    public virtual List<DynamicObject> getObjects() {
        List<DynamicObject> objects = new List<DynamicObject>();
        for (int i = 0; i < 10; i++) {
            DynamicObject obj = new DynamicObject(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Color.white);
            objects.Add(obj);
        }
        return objects;
    }

    public virtual bool isDynamic() {
        return false;
    }
}
