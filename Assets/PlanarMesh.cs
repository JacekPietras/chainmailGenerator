using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlanarMesh
{
    private Mesh mesh;
    private Mesh cleaningMesh;

    // lists of information corresponding every 3D triangle from source 3D object
    private List<TextureObject>[] textureObjectsOnTriangles;
    private Vector3[] gridVertices;

    public PlanarMesh(Mesh mesh3d, Texture2D objectMap)
    {
        createPlanarMesh(mesh3d, createObjects(objectMap));
        createCleaningMesh();
    }

    private void createCleaningMesh()
    {
        cleaningMesh = new Mesh();

        int[] triangles = new int[6];
        Vector3[] normals = new Vector3[6];
        Vector3[] vertices = new Vector3[6];
        Vector2[] uv = new Vector2[6];

        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(1, 1, 1);
        vertices[2] = new Vector3(0, 1, 1);
        vertices[3] = new Vector3(0, 0, 0);
        vertices[4] = new Vector3(1, 0, 0);
        vertices[5] = new Vector3(1, 1, 1);

        // iterating through every point
        for (int i = 0; i < vertices.Length; i++)
        {
            uv[i] = new Vector2(0, 0);
            triangles[i] = i;
            normals[i] = Vector3.forward;
        }

        cleaningMesh.vertices = vertices;
        cleaningMesh.uv = uv;
        cleaningMesh.triangles = triangles;
        cleaningMesh.normals = normals;
    }

    private void createPlanarMesh(Mesh mesh3d, List<TextureObject> objects)
    {
        mesh = new Mesh();
        //Debug.Log("Triangles count " + mesh3d.triangles.Length);
        //Debug.Log("UV count " + mesh3d.uv.LongLength);
        //Debug.Log("Vertices count " + mesh3d.vertices.Length);

        gridVertices = new Vector3[mesh3d.triangles.Length];
        textureObjectsOnTriangles = new List<TextureObject>[mesh3d.triangles.Length / 3];

        // iterating through every triangle
        for (int i = 0; i < mesh3d.triangles.Length; i += 3)
        {
            // UV triangle from 3D model
            Vector2 u1 = mesh3d.uv[mesh3d.triangles[i + 0]];
            Vector2 u2 = mesh3d.uv[mesh3d.triangles[i + 1]];
            Vector2 u3 = mesh3d.uv[mesh3d.triangles[i + 2]];

            // verticles of new mesh will be created from UV points of 3D object
            // we need to normalize Y value because it's for texture
            gridVertices[i] = new Vector3(u1.x, 1 - u1.y);
            gridVertices[i + 1] = new Vector3(u2.x, 1 - u2.y);
            gridVertices[i + 2] = new Vector3(u3.x, 1 - u3.y);

            foreach (TextureObject obj in objects)
            {
                Barycentric a = new Barycentric(u1, u2, u3, obj.toVector2());
                if (a.IsInside)
                {
                    if (textureObjectsOnTriangles[i / 3] == null)
                    {
                        textureObjectsOnTriangles[i / 3] = new List<TextureObject>();
                    }
                    textureObjectsOnTriangles[i / 3].Add(obj.copyWithBarycentric(a));
                }
            }
        }
    }

    public void updateMesh(Mesh mesh3d)
    {
        List<Vector2> uvList = new List<Vector2>();
        List<Vector3> vertList = new List<Vector3>();

        // iterating through every triangle
        for (int i = 0; i < mesh3d.triangles.Length; i += 3)
        {
            if (textureObjectsOnTriangles[i / 3] != null)
            {
                // 3D triangle from 3D model
                Triangle3D p = new Triangle3D(
                    mesh3d.vertices[mesh3d.triangles[i + 0]],
                    mesh3d.vertices[mesh3d.triangles[i + 1]],
                    mesh3d.vertices[mesh3d.triangles[i + 2]]);

                foreach (TextureObject obj in textureObjectsOnTriangles[i / 3])
                {
                    Triangle2D planarP = p.planar2();
                    planarP.applyScale(1 / obj.scale);
                    // that's interpolated center of ring on planar 3d triangle
                    Vector2 interpolated = obj.barycentric.Interpolate(planarP);

                    //Debug.Log("triangle (" + p.p1.x + ", " + p.p1.y + ", " + p.p1.z + ") (" + p.p2.x + ", " + p.p2.y + ", " + p.p2.z + ") (" + p.p3.x + ", " + p.p3.y + ", " + p.p3.z + ")");
                    //Debug.Log("Planar triangle (" + planarP.p1.x + ", " + planarP.p1.y + ") (" + planarP.p2.x + ", " + planarP.p2.y + ") (" + planarP.p3.x + ", " + planarP.p3.y + ")");
                    //Debug.Log("Interpolated (" + interpolated.x + ", " + interpolated.y + ")");

                    planarP.applyTranslation(-interpolated.x + 0.5f, -interpolated.y + 0.5f);

                    uvList.Add(planarP.p1);
                    uvList.Add(planarP.p2);
                    uvList.Add(planarP.p3);

                    vertList.Add(gridVertices[i]);
                    vertList.Add(gridVertices[i + 1]);
                    vertList.Add(gridVertices[i + 2]);

                    //Debug.Log("UV triangle (" + planarP.p1.x + ", " + planarP.p1.y + ") (" + planarP.p2.x + ", " + planarP.p2.y + ") (" + planarP.p3.x + ", " + planarP.p3.y + ")");
                    //Debug.Log("UV triangle (" + gridVertices[i].x + ", " + gridVertices[i].y + ", " + gridVertices[i].y + ") (" + gridVertices[i + 1].x + ", " + gridVertices[i + 1].y + ", " + gridVertices[i+1].y + ") (" + gridVertices[i + 2].x + ", " + gridVertices[i + 2].y + ", " + gridVertices[i+2].y + ")");
                }
            }
        }

        mesh.vertices = vertList.ToArray();
        mesh.uv = uvList.ToArray();
        fillMeshTriangles();
    }

    public void fillMeshTriangles()
    {
        int[] triangles = new int[mesh.vertices.Length];
        Vector3[] normals = new Vector3[mesh.vertices.Length];
        // iterating through every point
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            // filling positions of points in triangle
            triangles[i] = i;
            // all normals will be fixed
            normals[i] = Vector3.forward;
        }
        mesh.triangles = triangles;
        mesh.normals = normals;
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

    public Texture2D renderDistortedMap(Texture2D stamp, Texture2D output, Color background, int pass, int passCount)
    {
        // get a temporary RenderTexture. It will be canvas for rendering on it, but not output 
        RenderTexture renderTexture = RenderTexture.GetTemporary(output.width * passCount, output.height);

        // set the RenderTexture as global target (that means GL too)
        RenderTexture.active = renderTexture;

        // render GL immediately to the active render texture
        renderPlanarMeshOnTexture(stamp, background, pass, passCount);

        // read the active RenderTexture into a new Texture2D
        output.ReadPixels(new Rect(pass * output.width, 0, output.width + pass * output.width, output.height), 0, 0);

        // clean up after the party
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        // return the goods
        output.Apply();

        return output;
    }

    private void setMaterialByTexture(Texture2D stamp)
    {
        // create material for distortedMap rendering
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = stamp;
        material.SetPass(0);
    }

    private void setMaterialByColor(Color color)
    {
        // create material for distortedMap rendering
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.color = color;
        material.SetPass(0);
    }

    private void renderPlanarMeshOnTexture(Texture2D stamp, Color background, int pass, int passCount)
    {
        GL.PushMatrix();
        GL.LoadPixelMatrix(0 - pass, passCount - pass, 1, 0);
        setMaterialByColor(background);
        Graphics.DrawMeshNow(cleaningMesh, Vector3.zero, Quaternion.identity);
        setMaterialByTexture(stamp);
        Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
        setMaterialByTexture(null);
        GL.PopMatrix();
    }
}
