using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlanarMesh
{
    private Mesh mesh;
    private List<TextureObject>[] textureObjectsOnTriangles;
    private Barycentric[] barycentrics;

    // todo remove that and replace with real data
    private Triangle2D input = new Triangle2D(
               new Vector2(1, 0),
               new Vector2(1, 1),
               new Vector2(0, 1)
           );

    public PlanarMesh(Mesh mesh3d, Texture2D objectMap)
    {
        createPlanarMesh(mesh3d, createObjects(objectMap));
    }

    public Mesh getMesh()
    {
        return mesh;
    }

    private void createPlanarMesh(Mesh mesh3d, List<TextureObject> objects)
    {
        mesh = new Mesh();
        Debug.Log("Triangles count " + mesh3d.triangles.Length);
        Debug.Log("UV count " + mesh3d.uv.LongLength);
        Debug.Log("Vertices count " + mesh3d.vertices.Length);

        int[] triangles = new int[mesh3d.triangles.Length];
        Vector3[] vertices = new Vector3[mesh3d.triangles.Length];
        Vector3[] normals = new Vector3[mesh3d.triangles.Length];
        textureObjectsOnTriangles = new List<TextureObject>[mesh3d.triangles.Length / 3];
        barycentrics = new Barycentric[mesh3d.triangles.Length / 3];


        // iterating through every triangle
        for (int i = 0; i < mesh3d.triangles.Length; i += 3)
        {
            // UV triangle from 3D model
            Vector2 u1 = mesh3d.uv[mesh3d.triangles[i + 0]];
            Vector2 u2 = mesh3d.uv[mesh3d.triangles[i + 1]];
            Vector2 u3 = mesh3d.uv[mesh3d.triangles[i + 2]];

            // verticles of new mesh will be created from UV points of 3D object
            // we need to normalize Y value because it's for texture
            vertices[i] = new Vector3(u1.x, 1 - u1.y);
            vertices[i + 1] = new Vector3(u2.x, 1 - u2.y);
            vertices[i + 2] = new Vector3(u3.x, 1 - u3.y);

            foreach (TextureObject obj in objects)
            {
                Barycentric a = new Barycentric(u1, u2, u3, obj.toVector2());
                if (a.IsInside)
                {
                    if (textureObjectsOnTriangles[i / 3] == null)
                    {
                        textureObjectsOnTriangles[i / 3] = new List<TextureObject>();
                    }
                    textureObjectsOnTriangles[i / 3].Add(obj);
                    barycentrics[i / 3] = a;
                }
            }

            // filling positions of points in triangle
            triangles[i] = i;
            triangles[i + 1] = i + 1;
            triangles[i + 2] = i + 2;

            // all normals will be fixed
            normals[i] = Vector3.forward;
            normals[i + 1] = Vector3.forward;
            normals[i + 2] = Vector3.forward;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
    }

    public void updateMesh(Mesh mesh3d)
    {
        Vector2[] inputArray = input.toArray();
        Vector2 nothing = new Vector2(0, 0);
        Vector2[] uv = new Vector2[mesh3d.triangles.Length];
        float size = 0.5f;

        // iterating through every triangle
        for (int i = 0; i < mesh3d.triangles.Length; i += 3)
        {
            // 3D triangle from 3D model
            Vector3 p1 = mesh3d.vertices[mesh3d.triangles[i + 0]];
            Vector3 p2 = mesh3d.vertices[mesh3d.triangles[i + 1]];
            Vector3 p3 = mesh3d.vertices[mesh3d.triangles[i + 2]];

            uv[i] = nothing;
            uv[i + 1] = nothing;
            uv[i + 2] = nothing;

            // for now uv of output is static
            // TODO It need to be dynamic!
            if (textureObjectsOnTriangles[i / 3] != null)
            {
                foreach (TextureObject obj in textureObjectsOnTriangles[i / 3])
                {
                    Triangle3D p = new Triangle3D(p1, p2, p3);
                    Triangle2D planarP = p.planar2();
                    planarP.applyScale(1 / obj.scale);
                    // that's interpolated center of ring on planar 3d triangle
                    Vector2 interpolated = barycentrics[i / 3].Interpolate(planarP);
                    Debug.Log("triangle (" + p.p1.x + ", " + p.p1.y + ", " + p.p1.z + ") (" + p.p2.x + ", " + p.p2.y + ", " + p.p2.z + ") (" + p.p3.x + ", " + p.p3.y + ", " + p.p3.z + ")");
                    Debug.Log("Planar triangle (" + planarP.p1.x + ", " + planarP.p1.y + ") (" + planarP.p2.x + ", " + planarP.p2.y + ") (" + planarP.p3.x + ", " + planarP.p3.y + ")");
                    Debug.Log("Interpolated (" + interpolated.x + ", " + interpolated.y + ")");
                    
                    planarP.applyTranslation(-interpolated.x + size, -interpolated.y + size);

                    uv[i] = planarP.p1;
                    uv[i + 1] = planarP.p2;
                    uv[i + 2] = planarP.p3;

                    Debug.Log("UV triangle (" + planarP.p1.x + ", " + planarP.p1.y + ") (" + planarP.p2.x + ", " + planarP.p2.y + ") (" + planarP.p3.x + ", " + planarP.p3.y + ")");
                }
            }
        }
        mesh.uv = uv;
    }

    private List<TextureObject> createObjects(Texture2D objectMap)
    {
        try
        {
            Color color;
            List<TextureObject> objects = new List<TextureObject>();
            for (int i = 0; i < objectMap.width; i++)
            {
                for (int j = 0; j < objectMap.height; j++)
                {
                    if ((color = objectMap.GetPixel(i, j)) != Color.black)
                    {
                        TextureObject obj = new TextureObject(i / (float)objectMap.width, j / (float)objectMap.height, color);
                        Debug.Log("Found object (" + obj.x + ", " + obj.y + ")");
                        objects.Add(obj);
                    }
                }
            }
            return objects;
        }
        catch (Exception ignored)
        {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            SetTextureImporterFormat(objectMap, true);
            return createObjects(objectMap);
        }
    }

    // Used for possible probme when texture wasn't imported successfully
    public static void SetTextureImporterFormat(Texture2D texture, bool isReadable)
    {
        if (null == texture) return;

        string assetPath = AssetDatabase.GetAssetPath(texture);
        var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (tImporter != null)
        {
            tImporter.textureType = TextureImporterType.Default;

            tImporter.isReadable = isReadable;

            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
        }
    }
}
