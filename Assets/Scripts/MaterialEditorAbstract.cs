using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MaterialEditorAbstract : MonoBehaviour {
    public String layerName;

    protected MaterialEditorAbstract higherLayer;
    protected MaterialEditorAbstract lowerLayer;
    private int runCount = -1;

    void Start() {
        fillLayers();
        init();
    }

    void Update() {
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

        foreach (MaterialEditorAbstract ma in GetComponents(typeof(MaterialEditorAbstract))) {
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

    protected void setTextures() {
        if (higherLayer == null) {
            Material material = GetComponent<Renderer>().material;
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetTexture("_BumpMap", getNormalMap());
            material.SetTexture("_MainTex", getColorMap());
        }
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

    public virtual int getUsedPassesCount() {
        if (lowerLayer != null) { return lowerLayer.getUsedPassesCount(); } else { return 0; }
    }

    public virtual void updateDistortedMap(PlanarMesh planarMesh = null) { if (lowerLayer != null) { lowerLayer.updateDistortedMap(planarMesh); } }
}
