using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class MaterialEditor : MaterialEditorAbstract {
    // resolution of raytraced maps
    public int itemResolution = 512;
    // resolution of final texture for object
    public int textureResolution = 1024;
    // 3D object that will be raytraced to textures
    public GameObject item;
    public Texture2D objectMap;
    // additional height for layer to shift elements that are on other ones
    public bool heightShift = false;
    public Vector3 stampRotation = new Vector3(-90, 0, 0);
    public int normalizationSteps = 10;
    public float normalizationStrength = 0.5f;

    private RingGenerator generator;
    private PlanarMesh planarMesh;

    private Texture2D heightMap;
    private Texture2D normalMap;
    private Texture2D edgeMap;
    private Texture2D distortedColorMap;
    private Texture2D distortedHeightMap;
    private Texture2D distortedNormalMap;

    private Mesh mesh3d;

    public override Texture2D getHeightMap() { return distortedHeightMap; }

    public override Texture2D getColorMap() { return distortedColorMap; }

    public override Texture2D getNormalMap() { return distortedNormalMap; }

    public override void init() {
        mesh3d = GetComponent<MeshFilter>().mesh;

        // generates raytraced maps from 3D object
        generator = new RingGenerator(item, itemResolution, stampRotation);

        // raytracing stamp textures from 3D object
        heightMap = generator.getHeightMap();
        normalMap = generator.getNormalMap();
        edgeMap = generator.getEdgeMap(new Color(.7f, .7f, .72f, 1), 15, 3);

        // creation of resusable textures that will be used on model
        distortedHeightMap = new Texture2D(textureResolution, textureResolution);
        distortedNormalMap = new Texture2D(textureResolution, textureResolution);
        distortedColorMap = new Texture2D(textureResolution, textureResolution);

        planarMesh = new PlanarMesh(mesh3d, objectMap, normalizationSteps, normalizationStrength);
    }

    void OnDestroy() {
        // map is also saved in asset files so we can use it in other places
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_DistortedNormalMap.png", distortedNormalMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_DistortedHeightMap.png", distortedHeightMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_DistortedColorMap.png", distortedColorMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_NormalMap.png", normalMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_HeightMap.png", heightMap.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets/Maps/" + layerName + "_EdgeMap.png", edgeMap.EncodeToPNG());

        String path = "Assets/Normalization/";
        if (Directory.Exists(path)) { Directory.Delete(path, true); }
        Directory.CreateDirectory(path);
        int i = 0;
        foreach (Texture2D tex in planarMesh.texList) {
            System.IO.File.WriteAllBytes("Assets/Normalization/" + layerName + "_step_" + i + ".png", tex.EncodeToPNG());
            i++;
        }
    }

    public override void updateDistortedMap(PlanarMesh planarMesh = null) {
        if (planarMesh == null) {
            // prevents calculating mesh update in every layer
            planarMesh = this.planarMesh;
            planarMesh.updateMesh(mesh3d);
        }
        if (lowerLayer != null) { lowerLayer.updateDistortedMap(planarMesh); }
        int passShift = getUsedPassesCount() - 3;

        if (heightShift && lowerLayer != null && lowerLayer.getHeightMap() != null) {
            planarMesh.renderDistortedMap(heightMap, distortedHeightMap, lowerLayer.getHeightMap(), 0, passShift);
            planarMesh.renderDistortedMap(normalMap, distortedNormalMap, lowerLayer.getNormalMap(), 1, passShift);
        } else {
            planarMesh.renderDistortedMap(heightMap, distortedHeightMap, new Color(0, 0, 0, 1), 0, passShift);
            planarMesh.renderDistortedMap(normalMap, distortedNormalMap, new Color(.5f, .5f, 1, 1), 1, passShift);
        }

        if (lowerLayer != null) {
            if (lowerLayer.getHeightMap() != null && !heightShift) {
                planarMesh.renderDistortedMap(edgeMap, distortedColorMap, new Color(0, 0, 0, 1), 2, passShift);

                // merge lower layers into distorted maps
                bool[] mask = getMask();
                Color32[] combinedColor = distortedColorMap.GetPixels32();
                Color32[] combinedHeight = distortedHeightMap.GetPixels32();
                Color32[] combinedNormal = distortedNormalMap.GetPixels32();
                Color32[] lowerColor = getLowerLayerColorMapPixels();
                Color32[] lowerHeight = getLowerLayerHeightMapPixels();
                Color32[] lowerNormal = getLowerLayerNormalMapPixels();

                for (int i = 0; i < combinedColor.Length; i++) {
                    if (mask[i]) {
                        combinedColor[i] = lowerColor[i];
                        combinedHeight[i] = lowerHeight[i];
                        combinedNormal[i] = lowerNormal[i];
                    }
                }
                distortedColorMap.SetPixels32(combinedColor);
                distortedColorMap.Apply();
                distortedNormalMap.SetPixels32(combinedNormal);
                distortedNormalMap.Apply();
                distortedHeightMap.SetPixels32(combinedHeight);
                distortedHeightMap.Apply();
            } else { planarMesh.renderDistortedMap(edgeMap, distortedColorMap, lowerLayer.getColorMap(), 2, passShift); }
        } else { planarMesh.renderDistortedMap(edgeMap, distortedColorMap, new Color(0, 0, 0, 1), 2, passShift); }

        setTextures();
    }

    public override int getUsedPassesCount() {
        if (lowerLayer != null) { return lowerLayer.getUsedPassesCount() + 3; } else { return 3; }
    }

    private Color32[] getLowerLayerColorMapPixels() {
        try {
            return lowerLayer.getColorMap().GetPixels32();
        } catch (Exception ignored) {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            PlanarMesh.SetTextureImporterFormat(lowerLayer.getColorMap(), true);
            return lowerLayer.getColorMap().GetPixels32();
        }
    }

    private Color32[] getLowerLayerHeightMapPixels() {
        try {
            return lowerLayer.getHeightMap().GetPixels32();
        } catch (Exception ignored) {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            PlanarMesh.SetTextureImporterFormat(lowerLayer.getHeightMap(), true);
            return lowerLayer.getHeightMap().GetPixels32();
        }
    }

    private Color32[] getLowerLayerNormalMapPixels() {
        try {
            return lowerLayer.getNormalMap().GetPixels32();
        } catch (Exception ignored) {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            PlanarMesh.SetTextureImporterFormat(lowerLayer.getNormalMap(), true);
            return lowerLayer.getNormalMap().GetPixels32();
        }
    }

    private bool[] getMask() {
        Color32[] heightCurrent = getHeightMap().GetPixels32();
        bool[] result = new bool[heightCurrent.Length];

        Color32[] heightLower = getLowerLayerHeightMapPixels();
        if (heightLower.Length == heightCurrent.Length) {
            for (int i = 0; i < result.Length; i++) {
                result[i] = heightCurrent[i].r <= heightLower[i].r;
            }
        } else { throw new Exception("bottom layer texture is smaller"); }

        return result;
    }

}
