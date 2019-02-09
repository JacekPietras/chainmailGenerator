using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Read from objectTexture, point where on mesh we should create object
public class TextureObject {
    public float x;
    public float y;
    public float rotation;
    public float scale;
    public float unused;
    public Barycentric barycentric;

    public TextureObject(float x, float y, Color color) {
        this.x = x;
        this.y = y;
        this.rotation = color.r;
        this.scale = color.g / 5;
        this.unused = color.b;
    }

    public TextureObject(TextureObject original) {
        this.x = original.x;
        this.y = original.y;
        this.rotation = original.rotation;
        this.scale = original.scale;
        this.unused = original.unused;
    }

    public Vector2 toVector2() {
        return new Vector2(x, y);
    }

    public TextureObject copyWithBarycentric(Barycentric barycentric) {
        TextureObject copy = new TextureObject(this);
        copy.barycentric = barycentric;
        return copy;
    }
}
