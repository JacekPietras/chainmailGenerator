using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class MaterialEditor : MonoBehaviour
{
    // resolution of raytraced maps
    public int itemResolution = 512;
    // resolution of final texture for object
    public int textureResolution = 1024;
    // 3D object that will be raytraced to textures
    public GameObject item;
    public Texture2D objectMap;

    private RingGenerator generator;
    private PlanarMesh planarMesh;

    private Texture2D heightMap;
    private Texture2D normalMap;
    private Texture2D distortedHeightMap;
    private Texture2D distortedNormalMap;

    private Material material;
    private Mesh mesh3d;

    void Start()
    {
        material = GetComponent<Renderer>().material;
        mesh3d = GetComponent<MeshFilter>().mesh;

        // generates raytraced maps from 3D object
        generator = new RingGenerator(item, itemResolution);

        // raytracing stamp textures from 3D object
        heightMap = generator.getHeightMap();
        normalMap = generator.getNormalMap(30);

        // creation of resusable textures that will be used on model
        distortedHeightMap = new Texture2D(textureResolution, textureResolution);
        distortedNormalMap = new Texture2D(textureResolution, textureResolution);

        setTextures();

        planarMesh = new PlanarMesh(mesh3d, objectMap);
    }

    private void OnDestroy()
    {
        // map is also saved in asset files so we can use it in other places
        System.IO.File.WriteAllBytes("Assets/Maps/DistortedNormalMap.png", distortedNormalMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/DistortedHeightMap.png", distortedHeightMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/NormalMap.png", normalMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/HeightMap.png", heightMap.EncodeToPNG());
    }

    void Update()
    {
        if(this == null)
        {
            Debug.Log("not attached");
            return;
        }
        updateDistortedMap();
        Debug.Log("update of planar mesh");
    }

    private void updateDistortedMap()
    {
        planarMesh.updateMesh(mesh3d);
        planarMesh.renderDistortedMap(heightMap, distortedHeightMap, new Color(0, 0, 0, 1), 0, 2);
        planarMesh.renderDistortedMap(normalMap, distortedNormalMap, new Color(.5f, .5f, 1, 1), 1, 2);
    }

    private void setTextures()
    {
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICGLOSSMAP");
        material.SetTexture("_BumpMap", distortedNormalMap);
        material.SetTexture("_MainTex", distortedHeightMap);
    }
}
