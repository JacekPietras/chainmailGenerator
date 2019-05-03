using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrangerTexture : Arranger {
    public Texture2D objectMap;
    
    // returns list of texture objects on inputed texture
    // they have relative position, rotation, scale
    public override List<DynamicObject> getObjects() {
        if(objectMap == null) {
            return new List<DynamicObject>();
        }
        try {
            Color color;
            List<DynamicObject> objects = new List<DynamicObject>();
            for (int i = 0; i < objectMap.width; i++) {
                for (int j = 0; j < objectMap.height; j++) {
                    if ((color = objectMap.GetPixel(i, j)) != Color.black) {
                        DynamicObject obj = new DynamicObject(
                            i / (float)objectMap.width,
                            j / (float)objectMap.height,
                            color);
                        //Debug.Log("Found object (" + obj.x + ", " + obj.y + ")");
                        objects.Add(obj);
                    }
                }
            }
            Debug.Log("Found "+objects.Count+" objects");
            return objects;
        } catch (Exception e) {
            Debug.LogError(e.Data);
            // use in case of error with importer.
            DebugUtils.SetTextureImporterFormat(objectMap, true);
            return getObjects();
        }
    }
}
