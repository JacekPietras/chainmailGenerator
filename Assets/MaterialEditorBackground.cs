using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialEditorBackground : MaterialEditorAbstract
{
    public Texture2D colorMap;

    public override Texture2D getColorMap()
    {
        if (colorMap == null && lowerLayer != null) { return lowerLayer.getNormalMap(); }
        else { return colorMap; }
    }
}
