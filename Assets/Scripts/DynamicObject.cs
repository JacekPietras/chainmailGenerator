using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Read from objectTexture, point where on mesh we should create object
public class DynamicObject {
    public float x;
    public float y;
    public float rotation;
    public float scale;
    public float unused;
    public Barycentric barycentric;
    public bool assigned;

    public DynamicObject(float x, float y, Color color) {
        this.x = x;
        this.y = y;
        this.rotation = color.r;
        this.scale = color.g / 5;
        this.unused = color.b;
    }

    public DynamicObject(DynamicObject original) {
        this.x = original.x;
        this.y = original.y;
        this.rotation = original.rotation;
        this.scale = original.scale;
        this.unused = original.unused;
    }

    public Vector2 toVector2() {
        return new Vector2(x, y);
    }

    //public DynamicObject copyWithBarycentric(Barycentric barycentric) {
    //    DynamicObject copy = new DynamicObject(this);
    //    copy.barycentric = barycentric;
    //    return copy;
    //}
}
