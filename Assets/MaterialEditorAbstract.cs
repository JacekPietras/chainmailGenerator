using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MaterialEditorAbstract : MonoBehaviour
{
    public String layerName;

    public abstract Texture2D getHeightMap();

    public abstract Texture2D getColorMap();

    public abstract Texture2D getNormalMap();
}
