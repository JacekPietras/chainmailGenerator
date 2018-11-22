using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialEditorBackground : MaterialEditorAbstract
{
    public Texture2D colorMap;
    private bool drawingOver = false;
    private Texture2D distortedColorMap;
    private PlanarMesh planarMesh;

    public override Texture2D getColorMap()
    {
        if (colorMap == null && lowerLayer != null) { return lowerLayer.getColorMap(); }
        else if (distortedColorMap != null) { return distortedColorMap; }
        else { return colorMap; }
    }

    public override void updateDistortedMap()
    {
        if (lowerLayer != null)
        {
            lowerLayer.updateDistortedMap();

            if (lowerLayer.getColorMap() != null && colorMap != null)
            {
                if (distortedColorMap == null) { distortedColorMap = new Texture2D(lowerLayer.getColorMap().width, lowerLayer.getColorMap().height); }
                if (planarMesh == null) { planarMesh = new PlanarMesh(); }
                
                drawingOver = true;
                planarMesh.renderMapOver(colorMap, distortedColorMap, lowerLayer.getColorMap(), getUsedPassesCount() - 1);
            }
            setTextures();
        }
    }

    public override int getUsedPassesCount()
    {
        if (lowerLayer != null) { return lowerLayer.getUsedPassesCount() + (drawingOver ? 1 : 0); }
        else { return drawingOver ? 1 : 0; }
    }
}
