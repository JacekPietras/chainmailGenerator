using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialEditorBackground : MaterialEditorAbstract
{
    public Texture2D colorMap;

    public override Texture2D getHeightMap() { return null; }

    public override Texture2D getColorMap() { return colorMap; }

    public override Texture2D getNormalMap() { return null; }
}
