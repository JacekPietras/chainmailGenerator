using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public abstract class LayerAbstract : MonoBehaviour {
    public String layerName;

    protected LayerAbstract higherLayer;
    protected LayerAbstract lowerLayer;
    private int runCount = -1;
    public int nothingAtAll = 0;

    void Start() {
        fillLayers();
        init();
    }

    void Update() {
        nothingAtAll++;

        if (this == null) {
            Debug.Log("not attached");
            return;
        }
        if (higherLayer == null && runCount != 0) {
            updateDistortedMap();
            runCount--;
        }
        //Debug.Log("update of planar mesh");
    }

    public virtual void init() { }

    protected void fillLayers() {
        bool foundSelf = false;

        foreach (LayerAbstract ma in GetComponents(typeof(LayerAbstract))) {
            if (ma == this) { foundSelf = true; } else {
                if (foundSelf) {
                    Debug.Log("found higher layer " + ma.layerName + " (in " + layerName + ")");
                    higherLayer = ma;
                    return;
                } else {
                    Debug.Log("found lower layer " + ma.layerName + " (in " + layerName + ")");
                    lowerLayer = ma;
                }
            }
        }
    }

    protected String createOutputDirectory() {
        String outputPath = "Assets/OutputMaps/";
        if (Directory.Exists(outputPath) && lowerLayer == null) {
            Directory.Delete(outputPath, true);
        }
        Directory.CreateDirectory(outputPath);
        return outputPath;
    }

    protected void setTextures() {
        if (higherLayer == null) {
            Material material = GetComponent<Renderer>().material;
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetTexture("_BumpMap", getNormalMap());
            material.SetTexture("_MainTex", getColorMap());
            material.SetTexture("_MetallicGlossMap", getColorMap());
            material.SetTexture("_ParallaxMap", getHeightMap());
        }
    }

    protected Color32[] getLowerLayerColorMapPixels() {
        try {
            return lowerLayer.getColorMap().GetPixels32();
        } catch (Exception ignored) {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            DebugUtils.SetTextureImporterFormat(lowerLayer.getColorMap(), true);
            return lowerLayer.getColorMap().GetPixels32();
        }
    }

    protected Color32[] getLowerLayerHeightMapPixels() {
        try {
            return lowerLayer.getHeightMap().GetPixels32();
        } catch (Exception ignored) {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            DebugUtils.SetTextureImporterFormat(lowerLayer.getHeightMap(), true);
            return lowerLayer.getHeightMap().GetPixels32();
        }
    }

    protected Color32[] getLowerLayerNormalMapPixels() {
        try {
            return lowerLayer.getNormalMap().GetPixels32();
        } catch (Exception ignored) {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            DebugUtils.SetTextureImporterFormat(lowerLayer.getNormalMap(), true);
            return lowerLayer.getNormalMap().GetPixels32();
        }
    }

    protected bool[] getMask() {
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

    public virtual Texture2D getHeightMap() {
        if (lowerLayer != null) { return lowerLayer.getHeightMap(); } else { return null; }
    }

    public virtual Texture2D getNormalMap() {
        if (lowerLayer != null) { return lowerLayer.getNormalMap(); } else { return null; }
    }

    public virtual Texture2D getColorMap() {
        if (lowerLayer != null) { return lowerLayer.getNormalMap(); } else { return null; }
    }

    public int getUsedPassesCount() {
        if (lowerLayer != null) {
            return lowerLayer.getUsedPassesCount() + getUsedPassesCountVal();
        } else {
            return getUsedPassesCountVal();
        }
    }

    public virtual int getUsedPassesCountVal() {
        return 0;
    }

    public virtual void updateDistortedMap(PlanarMesh planarMesh = null) {
        if (lowerLayer != null) {
            lowerLayer.updateDistortedMap(planarMesh);
        }
    }
}
