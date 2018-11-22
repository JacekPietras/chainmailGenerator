using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class MaterialEditor : MaterialEditorAbstract
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
    private Texture2D edgeMap;
    private Texture2D distortedColorMap;
    private Texture2D distortedHeightMap;
    private Texture2D distortedNormalMap;

    private Mesh mesh3d;

    private MaterialEditorAbstract higherLayer;
    private MaterialEditorAbstract lowerLayer;

    public override Texture2D getHeightMap() { return distortedHeightMap; }

    public override Texture2D getColorMap() { return distortedColorMap; }

    public override Texture2D getNormalMap() { return distortedNormalMap; }

    void Start()
    {
        fillLayers();

        mesh3d = GetComponent<MeshFilter>().mesh;

        // generates raytraced maps from 3D object
        generator = new RingGenerator(item, itemResolution);

        // raytracing stamp textures from 3D object
        heightMap = generator.getHeightMap();
        normalMap = generator.getNormalMap();
        edgeMap = generator.getEdgeMap(15, 3);

        // creation of resusable textures that will be used on model
        distortedHeightMap = new Texture2D(textureResolution, textureResolution);
        distortedNormalMap = new Texture2D(textureResolution, textureResolution);
        distortedColorMap = new Texture2D(textureResolution, textureResolution);

        setTextures();

        planarMesh = new PlanarMesh(mesh3d, objectMap);
    }

    void OnDestroy()
    {
        // map is also saved in asset files so we can use it in other places
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_DistortedNormalMap.png", distortedNormalMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_DistortedHeightMap.png", distortedHeightMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_DistortedColorMap.png", distortedColorMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_NormalMap.png", normalMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_HeightMap.png", heightMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_EdgeMap.png", edgeMap.EncodeToPNG());
    }

    void Update()
    {
        if (this == null)
        {
            Debug.Log("not attached");
            return;
        }
        updateDistortedMap();
        Debug.Log("update of planar mesh");
    }

    private void fillLayers()
    {
        bool foundSelf = false;

        foreach (MaterialEditorAbstract ma in GetComponents(typeof(MaterialEditorAbstract)))
        {
            if (ma == this) { foundSelf = true; }
            else
            {
                if (foundSelf)
                {
                    Debug.Log("found higher layer " + ma.layerName + " (in " + layerName + ")");
                    higherLayer = ma;
                    return;
                }
                else
                {
                    Debug.Log("found lower layer " + ma.layerName + " (in " + layerName + ")");
                    lowerLayer = ma;
                }
            }
        }
    }

    private void updateDistortedMap()
    {
        planarMesh.updateMesh(mesh3d);
        planarMesh.renderDistortedMap(heightMap, distortedHeightMap, new Color(0, 0, 0, 1), 0);
        planarMesh.renderDistortedMap(normalMap, distortedNormalMap, new Color(.5f, .5f, 1, 1), 1);
        planarMesh.renderDistortedMap(edgeMap, distortedColorMap, new Color(0, 0, 0, 1), 0);

        // merge lower layers into distorted maps
        if (lowerLayer.getColorMap() != null)
        {
            bool[] mask = getMask();
            Color32[] combinedColor = distortedColorMap.GetPixels32();
            Color32[] lowerColor = getLowerLayerColorMapPixels();

            for (int i = 0; i < combinedColor.Length; i++)
            {
                if (mask[i]) { combinedColor[i] = lowerColor[i]; }
            }
            distortedColorMap.SetPixels32(combinedColor);
            distortedColorMap.Apply();

            if (lowerLayer.getHeightMap() != null)
            {
                Color32[] combinedHeight = distortedHeightMap.GetPixels32();
                Color32[] combinedNormal = distortedNormalMap.GetPixels32();
                Color32[] lowerHeight = getLowerLayerHeightMapPixels();
                Color32[] lowerNormal = getLowerLayerNormalMapPixels();
                
                for (int i = 0; i < combinedColor.Length; i++)
                {
                    if (mask[i])
                    {
                        combinedHeight[i] = lowerHeight[i];
                        combinedNormal[i] = lowerNormal[i];
                    }
                }
                distortedNormalMap.SetPixels32(combinedColor);
                distortedNormalMap.Apply();
                distortedHeightMap.SetPixels32(combinedHeight);
                distortedHeightMap.Apply();
            }
        }
    }

    private Color32[] getLowerLayerColorMapPixels()
    {
        try
        {
            return lowerLayer.getColorMap().GetPixels32();
        }
        catch (Exception ignored)
        {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            PlanarMesh.SetTextureImporterFormat(lowerLayer.getColorMap(), true);
            return lowerLayer.getColorMap().GetPixels32();
        }
    }

    private Color32[] getLowerLayerHeightMapPixels()
    {
        try
        {
            return lowerLayer.getHeightMap().GetPixels32();
        }
        catch (Exception ignored)
        {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            PlanarMesh.SetTextureImporterFormat(lowerLayer.getHeightMap(), true);
            return lowerLayer.getHeightMap().GetPixels32();
        }
    }

    private Color32[] getLowerLayerNormalMapPixels()
    {
        try
        {
            return lowerLayer.getHeightMap().GetPixels32();
        }
        catch (Exception ignored)
        {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            PlanarMesh.SetTextureImporterFormat(lowerLayer.getHeightMap(), true);
            return lowerLayer.getHeightMap().GetPixels32();
        }
    }

    private bool[] getMask()
    {
        Color32[] heightCurrent = getHeightMap().GetPixels32();
        bool[] result = new bool[heightCurrent.Length];

        if (lowerLayer.getHeightMap() != null)
        {
            Color32[] heightLower = getLowerLayerHeightMapPixels();
            if (heightLower.Length == heightCurrent.Length)
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = heightCurrent[i].r < heightLower[i].r;
                }
            }
            else { throw new Exception("bottom layer texture is smaller"); }
        }
        else
        {
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = heightCurrent[i].r == 0;
            }
        }

        return result;
    }

    private void setTextures()
    {
        if (higherLayer == null)
        {
            Material material = GetComponent<Renderer>().material;
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetTexture("_BumpMap", distortedNormalMap);
            material.SetTexture("_MainTex", distortedColorMap);
        }
    }
}
