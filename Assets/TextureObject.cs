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

    public TextureObject(float x, float y, Color color)
    {
        this.x = x;
        this.y = y;
        this.rotation = color.r;
        this.scale = color.g/15;
        this.unused = color.b;
    }

    public Vector2 toVector2()
    {
        return new Vector2(x,y);
    }
}
