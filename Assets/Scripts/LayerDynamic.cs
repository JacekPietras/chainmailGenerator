using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public class LayerDynamic : LayerAbstract {
    // resolution of raytraced maps
    public int itemResolution = 512;
    // resolution of final texture for object
    public int textureResolution = 1024;
    // 3D object that will be raytraced to textures
    public GameObject item;
    // additional height for layer to shift elements that are on other ones
    public bool heightShift = false;
    public Vector3 stampRotation = new Vector3(-90, 0, 0);
    public int normalizationSteps = 10;
    public float normalizationStrength = 0.7f;
    public bool showingNormalization = false;
    public int neighbourRadius = 1;
    public bool detectOverlappingOnAllTriangles = false;
    public bool detectOverlappingOnAllEdges = true;
    public bool useStrength = false;
    public bool alwaysBuildBestMesh = false;
    public bool DEBUG_TRIANGLES = false;

    private RingGenerator generator;
    private PlanarMesh planarMesh;
    private bool multipleArrangers;

    private Texture2D heightMap;
    private Texture2D normalMap;
    private Texture2D normalMapFromHeight;
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

        if(item == null) {
            return;
        }

        // generates raytraced maps from 3D object
        generator = new RingGenerator(item, itemResolution, stampRotation);

        // raytracing stamp textures from 3D object
        heightMap = generator.getHeightMap();
        normalMap = generator.getNormalMap();
        normalMapFromHeight = generator.getNormalMapFromHeight();
        edgeMap = generator.getEdgeMap(new Color(.7f, .7f, .72f, 1), 15, 3);

        // creation of resusable textures that will be used on model
        distortedHeightMap = new Texture2D(textureResolution, textureResolution);
        distortedNormalMap = new Texture2D(textureResolution, textureResolution);
        distortedColorMap = new Texture2D(textureResolution, textureResolution);

        planarMesh = new PlanarMesh(
            GetComponent<Transform>(),
            mesh3d,
            getArranger(),
            normalizationSteps,
            normalizationStrength,
            showingNormalization,
            neighbourRadius,
            detectOverlappingOnAllTriangles,
            detectOverlappingOnAllEdges,
            useStrength,
            alwaysBuildBestMesh);
    }

    void OnDestroy() {
        if (distortedNormalMap != null) {
            String outputPath = "Assets/OutputMaps/";
            if (Directory.Exists(outputPath) && lowerLayer == null) {
                Directory.Delete(outputPath, true);
            }
            Directory.CreateDirectory(outputPath);
            // map is also saved in asset files so we can use it in other places
            System.IO.File.WriteAllBytes(outputPath + layerName + "_DistortedNormalMap.png", distortedNormalMap.EncodeToPNG());
            System.IO.File.WriteAllBytes(outputPath + layerName + "_DistortedHeightMap.png", distortedHeightMap.EncodeToPNG());
            System.IO.File.WriteAllBytes(outputPath + layerName + "_DistortedColorMap.png", distortedColorMap.EncodeToPNG());
            //System.IO.File.WriteAllBytes(outputPath + layerName + "_NormalMap.png", normalMap.EncodeToPNG());
            //System.IO.File.WriteAllBytes(outputPath + layerName + "_NormalMapFromHeight.png", normalMapFromHeight.EncodeToPNG());
            //System.IO.File.WriteAllBytes(outputPath + layerName + "_HeightMap.png", heightMap.EncodeToPNG());
            //System.IO.File.WriteAllBytes(outputPath + layerName + "_EdgeMap.png", edgeMap.EncodeToPNG());

            String normalizationPath = outputPath + "Normalization/";
            if (Directory.Exists(normalizationPath)) {
                Directory.Delete(normalizationPath, true);
            }
            if (showingNormalization) {
                Directory.CreateDirectory(normalizationPath);
                for (int j = 0; j < planarMesh.texObjectsCount; j++) {
                    for (int i = 0; i < normalizationSteps + 1; i++) {
                        Texture2D tex = planarMesh.texList[j, i];
                        if (tex == null) continue;
                        String prefix = "";
                        if (planarMesh.texObjectsCount > 1) {
                            prefix = "_" + j;
                        }
                        try {
                            System.IO.File.WriteAllBytes(normalizationPath + layerName + prefix + "_step_" + i + ".png", tex.EncodeToPNG());
                        } catch (IOException e) {
                            Debug.LogError(e.Data);
                        }
                    }
                }
            }
        } else {
            Debug.Log("no maps for " + layerName);
        }
    }

    public override void updateDistortedMap(PlanarMesh planarMesh = null) {
        if(heightMap == null) {
            return;
        }

        if (multipleArrangers) {
            planarMesh = null;
        }
        if (planarMesh == null) {
            // prevents calculating mesh update in every layer
            planarMesh = this.planarMesh;
            planarMesh.updateMesh(mesh3d);
        }

        if (lowerLayer != null) { lowerLayer.updateDistortedMap(planarMesh); }
        int passShift = getUsedPassesCount() - 3;
        planarMesh.DEBUG_TRIANGLES = DEBUG_TRIANGLES;


        if (heightShift && lowerLayer != null && lowerLayer.getHeightMap() != null) {
            planarMesh.renderDistortedMap(heightMap, distortedHeightMap, lowerLayer.getHeightMap(), 0, passShift);
            planarMesh.renderDistortedMap(normalMap, distortedNormalMap, lowerLayer.getNormalMap(), 1, passShift);
        } else {
            planarMesh.renderDistortedMap(heightMap, distortedHeightMap, new Color(0, 0, 0, 1), 0, passShift);
            planarMesh.renderDistortedMap(normalMap, distortedNormalMap, new Color(.5f, .5f, 1, 1), 1, passShift);
        }

        Color bg = new Color(0, 0, 0, 0);
        if (DEBUG_TRIANGLES) {
            bg = new Color(0, 0, 0, 1);
        }

        if (lowerLayer != null) {
            if (lowerLayer.getHeightMap() != null && !heightShift) {
                // there is LayerDynamic below, we need tom merge into it looking at height of pixels

                planarMesh.renderDistortedMap(edgeMap, distortedColorMap, bg, 2, passShift);

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
            } else {
                // there is LayerStatic below, we can just draw color over it
                planarMesh.renderDistortedMap(edgeMap, distortedColorMap, lowerLayer.getColorMap(), 2, passShift);
            }
        } else {
            // there is nothing below, we can draw over nothing
            planarMesh.renderDistortedMap(edgeMap, distortedColorMap, bg, 2, passShift);
        }

        setTextures();
    }

    public override int getUsedPassesCountVal() {
        return 3;
    }

    private Arranger getArranger() {
        int whichDynamicIAm = 0;
        int whichArrangerItIs = 0;
        multipleArrangers = GetComponents(typeof(Arranger)).Length > 1;


        foreach (LayerDynamic ma in GetComponents(typeof(LayerDynamic))) {
            whichDynamicIAm++;
            if (ma == this) {
                break;
            }
        }

        foreach (Arranger arranger in GetComponents(typeof(Arranger))) {
            whichArrangerItIs++;
            if (whichArrangerItIs == whichDynamicIAm) {
                Debug.Log("Found arranger " + whichArrangerItIs + " " + whichDynamicIAm);
                return arranger;
            }
        }

        ArrangerSpray spray = new ArrangerSpray();
        spray.size = 0;
        return spray;
    }

    public void sendVerticles(Vector3[] verticles) {
        Debug.Log("sendVerticles " + verticles.Length);
        mesh3d.vertices = verticles;
    }
}
