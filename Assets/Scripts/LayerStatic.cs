using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public class LayerStatic : LayerAbstract {
    public Texture2D colorMap;
    private bool drawingOver = false;
    private Texture2D distortedColorMap;
    private Texture2D distortedHeightMap;
    private Texture2D distortedNormalMap;
    private PlanarMesh planarMesh;

    public override Texture2D getColorMap() {
        if (colorMap == null && lowerLayer != null) { return lowerLayer.getColorMap(); } else if (distortedColorMap != null) { return distortedColorMap; } else { return colorMap; }
    }

    public override Texture2D getHeightMap() {
        if (distortedHeightMap != null) { return distortedHeightMap; } else if (lowerLayer != null) { return lowerLayer.getHeightMap(); } else { return null; }
    }

    public override Texture2D getNormalMap() {
        if (distortedNormalMap != null) { return distortedNormalMap; } else if (lowerLayer != null) { return lowerLayer.getNormalMap(); } else { return null; }
    }

    void OnDestroy() {
        String outputPath = createOutputDirectory();

        // map is also saved in asset files so we can use it in other places
        if (distortedNormalMap != null)
            System.IO.File.WriteAllBytes(outputPath + layerName + "_DistortedNormalMap.png", distortedNormalMap.EncodeToPNG());
        if (distortedHeightMap != null)
            System.IO.File.WriteAllBytes(outputPath + layerName + "_DistortedHeightMap.png", distortedHeightMap.EncodeToPNG());
        if (distortedColorMap != null)
            System.IO.File.WriteAllBytes(outputPath + layerName + "_DistortedColorMap.png", distortedColorMap.EncodeToPNG());
    }

    public override void updateDistortedMap(PlanarMesh planarMesh = null) {
        if (lowerLayer != null) {
            lowerLayer.updateDistortedMap(planarMesh);
            if (colorMap != null) {
                if (lowerLayer.getColorMap() != null) {
                    if (distortedColorMap == null) { distortedColorMap = new Texture2D(lowerLayer.getColorMap().width, lowerLayer.getColorMap().height); }
                    if (planarMesh == null) { planarMesh = new PlanarMesh(); }

                    drawingOver = true;
                    planarMesh.renderMapOver(colorMap, distortedColorMap, lowerLayer.getColorMap(), 0, getUsedPassesCount() - 1);
                }
                if (lowerLayer.getHeightMap() != null) {
                    if (distortedHeightMap == null) { distortedHeightMap = new Texture2D(lowerLayer.getHeightMap().width, lowerLayer.getHeightMap().height); }
                    if (distortedNormalMap == null) { distortedNormalMap = new Texture2D(lowerLayer.getNormalMap().width, lowerLayer.getNormalMap().height); }

                    Color[] combinedHeight = getColorMapPixels();
                    Color[] lowerHeight = lowerLayer.getHeightMap().GetPixels();
                    for (int i = 0; i < combinedHeight.Length; i++) {
                        float color = Mathf.Min(1, lowerHeight[i].r + combinedHeight[i].a);
                        combinedHeight[i] = new Color(color, color, color, 1);
                    }
                    distortedHeightMap.SetPixels(combinedHeight);
                    distortedHeightMap.Apply();

                    StampGenerator.printNormalMap(distortedNormalMap, distortedHeightMap);
                }
                setTextures();
            }
        } else if (colorMap != null)  {
            setTextures();
        }
    }

    private Color[] getColorMapPixels() {
        try {
            return colorMap.GetPixels();
        } catch (Exception ignored) {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            DebugUtils.SetTextureImporterFormat(colorMap, true);
            return colorMap.GetPixels();
        }
    }
    
    public override int getUsedPassesCountVal() {
        return drawingOver ? 1 : 0;
    }
}
