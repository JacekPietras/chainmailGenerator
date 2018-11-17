using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MaterialEditor : MonoBehaviour
{
    // 3D object that will be raytraced to textures
    public GameObject item;
    // resolution of raytraced maps
    public int itemResolution = 512;
    // resolution of final texture for object
    public int textureResolution = 1024;
    public Texture2D objectMap;

    private RingGenerator generator;
    private PlanarMesh planarMesh;

    void Start()
    {
        // generates raytraced maps from 3D object
        generator = new RingGenerator(item, itemResolution);

        // raytracing textures from 3D object
        Texture2D heightMap = generator.getHeightMap();
        Texture2D normalMap = generator.getNormalMap(30);

        // generating distorted texture
        Texture2D distortedMap = paintAllTriangles(normalMap);

        // map is also saved in asset files so we can use it in other places
        System.IO.File.WriteAllBytes("Assets/Maps/DistortedMap.png", distortedMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/NormalMap.png", normalMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/HeightMap.png", heightMap.EncodeToPNG());

        // Setting generated texture to object
        GetComponent<Renderer>().material.mainTexture = distortedMap;
    }

    void Update()
    {
        Texture2D distortedMap = paintAllTriangles(generator.getNormalMap(30));
        GetComponent<Renderer>().material.mainTexture = distortedMap;
    }

    Texture2D paintAllTriangles(Texture2D source)
    {
        Mesh mesh3d = GetComponent<MeshFilter>().mesh;

        return RenderToTexture(mesh3d, source);
    }

    private Texture2D RenderToTexture(Mesh mesh3d, Texture2D source)
    {
        // create material for distortedMap rendering
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = source;
        material.SetPass(0);

        // get a temporary RenderTexture. It will be canvas for rendering on it, but not output 
        RenderTexture renderTexture = RenderTexture.GetTemporary(textureResolution, textureResolution);
        renderTexture.wrapMode = TextureWrapMode.Clamp;

        // set the RenderTexture as global target (that means GL too)
        RenderTexture.active = renderTexture;

        // render GL immediately to the active render texture
        renderTriangle(mesh3d);

        // read the active RenderTexture into a new Texture2D
        Texture2D newTexture = new Texture2D(textureResolution, textureResolution);
        newTexture.wrapMode = TextureWrapMode.Clamp;
        newTexture.ReadPixels(new Rect(0, 0, textureResolution, textureResolution), 0, 0);

        // clean up after the party
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        // return the goods
        newTexture.Apply();
        return newTexture;
    }

    private void renderTriangle(Mesh mesh3d)
    {
        if (planarMesh == null)
        {
            planarMesh = new PlanarMesh(mesh3d, objectMap);
        }
        planarMesh.updateMesh(mesh3d);

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, 1, 1, 0);
        Graphics.DrawMeshNow(planarMesh.getMesh(), Vector3.zero, Quaternion.identity);
        GL.PopMatrix();
    }
}
