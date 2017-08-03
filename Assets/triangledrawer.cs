using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triangledrawer : MonoBehaviour
{
    private Texture2D texture;
    private static int max = 1024;

    private Vector3 p1 = new Vector3(0, 0, 0);
    private Vector3 p2 = new Vector3(0.8f, 0, 20.0f);
    private Vector3 p3 = new Vector3(0.5f, 0.5f, 0);
    private Triangle2D triangle2d;
    private Triangle3D triangle3d;

    // Use this for initialization
    void Start()
    {
        texture = new Texture2D(max, max, TextureFormat.ARGB32, false);
        triangle3d = new Triangle3D(p1, p2, p3);
        triangle2d = triangle3d.planar();
        colorizeTriangles();
        texture.Apply();
        GetComponent<Renderer>().material.mainTexture = texture;
    }


    void colorizeTriangles()
    {
        for (int i = 0; i < texture.width; i++)
        {
            for (int j = 0; j < texture.height; j++)
            {
                float px = i, py = j;
                px /= texture.width;
                py /= texture.height;
                if (triangle2d.pointInside(new Vector2(px, py)))
                {
                    texture.SetPixel(i, j, Color.red);
                }
                else
                    texture.SetPixel(i, j, Color.blue);
            }
        }

    }

}
