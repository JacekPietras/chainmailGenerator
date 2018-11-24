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
    private int[][] neighbourtriangles;
    private int[][] neighbourVerticles;
    private Edge[][] neighbourEdges;

    public PlanarMesh()
    {
        createCleaningMesh();
    }

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
            uv[i] = new Vector2(vertices[i].x, 1 - vertices[i].y);
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
        Debug.Log("Triangles count " + mesh3d.triangles.Length / 3);
        Debug.Log("UV count " + mesh3d.uv.LongLength);
        Debug.Log("Vertices count " + mesh3d.vertices.Length);

        gridVertices = new Vector3[mesh3d.triangles.Length];
        textureObjectsOnTriangles = new List<TextureObject>[mesh3d.triangles.Length / 3];
        neighbourtriangles = new int[mesh3d.triangles.Length / 3][];
        neighbourVerticles = new int[mesh3d.triangles.Length / 3][];
        neighbourEdges = new Edge[mesh3d.triangles.Length / 3][];

        // iterating through every triangle
        for (int i = 0; i < mesh3d.triangles.Length; i += 3)
        {
            // UV triangle from 3D model
            Vector2 u1 = mesh3d.uv[mesh3d.triangles[i + 0]];
            Vector2 u2 = mesh3d.uv[mesh3d.triangles[i + 1]];
            Vector2 u3 = mesh3d.uv[mesh3d.triangles[i + 2]];

            Triangle3D p = new Triangle3D(
                mesh3d.vertices[mesh3d.triangles[i + 0]],
                mesh3d.vertices[mesh3d.triangles[i + 1]],
                mesh3d.vertices[mesh3d.triangles[i + 2]]);


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

                    if (neighbourtriangles[i / 3] == null)
                    {
                        // Debug.Log("looking for neighbours");
                        fillNeighbours(p, mesh3d, i);
                    }
                }
            }
        }
    }

    private void fillNeighbours(Triangle3D p, Mesh mesh3d, int current)
    {
        // list of unique verts around neighbours, values there can change after update
        // we need to recreatethat list using triVert array
        List<Vector3> vert = new List<Vector3>();
        // list of shortcuts to verticles from 3D mesh, that only indexes so it shouldn't change
        // we cannot use just verticle list because they are doubled on edges because of uv and normals
        List<int> triVert = new List<int>();
        // list of triangles. Verticles should be accessed from mesh3d.vertices
        // with mapping from triVert array
        List<int> tri = new List<int>();
        List<Edge> edges = new List<Edge>();

        // filling first triangle
        vert.Add(p.p1);
        vert.Add(p.p2);
        vert.Add(p.p3);
        triVert.Add(mesh3d.triangles[current + 0]);
        triVert.Add(mesh3d.triangles[current + 1]);
        triVert.Add(mesh3d.triangles[current + 2]);
        tri.Add(0);
        tri.Add(1);
        tri.Add(2);

        // iterating through every triangle
        for (int i = 0; i < mesh3d.triangles.Length; i += 3)
        {
            if (i == current) continue;

            Triangle3D n = new Triangle3D(
                mesh3d.vertices[mesh3d.triangles[i + 0]],
                mesh3d.vertices[mesh3d.triangles[i + 1]],
                mesh3d.vertices[mesh3d.triangles[i + 2]]);

            if (n.isNeighbour(p))
            {
                int indexOfNP1 = vert.IndexOf(n.p1);
                int indexOfNP2 = vert.IndexOf(n.p2);
                int indexOfNP3 = vert.IndexOf(n.p3);

                if (indexOfNP1 == -1)
                {
                    vert.Add(n.p1);
                    triVert.Add(mesh3d.triangles[i + 0]);
                    indexOfNP1 = vert.Count - 1;
                }
                if (indexOfNP2 == -1)
                {
                    vert.Add(n.p2);
                    triVert.Add(mesh3d.triangles[i + 1]);
                    indexOfNP2 = vert.Count - 1;
                }
                if (indexOfNP3 == -1)
                {
                    vert.Add(n.p3);
                    triVert.Add(mesh3d.triangles[i + 2]);
                    indexOfNP3 = vert.Count - 1;
                }
                tri.Add(indexOfNP1);
                tri.Add(indexOfNP2);
                tri.Add(indexOfNP3);

                // filling list of edges that need to be normalized
                // index need to be less than 3 because indexes 0,1,2 are for mothet triangle
                if (indexOfNP1 < 3)
                {
                    edges.Add(new Edge(indexOfNP2, indexOfNP1));
                    edges.Add(new Edge(indexOfNP3, indexOfNP1));
                }
                if (indexOfNP2 < 3)
                {
                    edges.Add(new Edge(indexOfNP1, indexOfNP2));
                    edges.Add(new Edge(indexOfNP3, indexOfNP2));
                }
                if (indexOfNP3 < 3)
                {
                    edges.Add(new Edge(indexOfNP1, indexOfNP3));
                    edges.Add(new Edge(indexOfNP2, indexOfNP3));
                }
            }
        }
        neighbourtriangles[current / 3] = tri.ToArray();
        neighbourVerticles[current / 3] = triVert.ToArray();
        neighbourEdges[current / 3] = edges.ToArray();
        //Debug.Log("neighbour: " + (current / 3 + 1) + ", triangles " + (tri.Count / 3 - 1) + ", vert " + (vert.Count));
    }

    // mesh of triangle with his neighbours
    private MeshFlat BuildLocalMesh(int triangle, Mesh mesh3d)
    {
        Vector3[] vertices = new Vector3[neighbourVerticles[triangle].Length];
        for (int j = 0; j < vertices.Length; j++)
        {
            vertices[j] = mesh3d.vertices[neighbourVerticles[triangle][j]];
        }

        MeshFlat localMesh = new MeshFlat();
        localMesh.vertices = vertices;
        localMesh.triangles = neighbourtriangles[triangle];
        localMesh.edges = neighbourEdges[triangle];
        localMesh.fillEdgeLength();

        return localMesh;
    }

    public void updateMesh(Mesh mesh3d)
    {
        Debug.Log("-------------------------------------");
        List<Vector2> uvList = new List<Vector2>();
        List<Vector3> vertList = new List<Vector3>();
        Vector3 center = new Vector3(.5f, .5f, 0);

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

                MeshFlat localMesh = BuildLocalMesh(i / 3, mesh3d);
                localMesh.rotateAndFlattenMesh();
                localMesh.normalizeFlatMesh(5);

                //todo - normalizuj
                //todo - interpoluj barycentryczne
                //todo - przesun aby byla na srodku
                //todo - dodaj do listy renderowania

                foreach (TextureObject obj in textureObjectsOnTriangles[i / 3])
                {
                    //TODO remove that sin rotation
                    obj.rotation = Mathf.Sin(Time.realtimeSinceStartup);

                    // that's interpolated center of ring on planar 3d triangle
                    Vector3 interpolated = obj.barycentric.Interpolate(localMesh.vertices[0], localMesh.vertices[1], localMesh.vertices[2]);
                    Vector3[] transformedVerticles = new Vector3[localMesh.vertices.Length];

                    for (int k = 0; k < localMesh.vertices.Length; k++)
                    {
                        // moving to center of coords
                        Vector3 transformed = localMesh.vertices[k] - interpolated;
                        // scaling 
                        transformed *= (1 / obj.scale);
                        // rotation
                        transformed = rotatePoint(transformed, obj.rotation);
                        // setting center as center of bitmap
                        transformed += center;

                        transformedVerticles[k] = transformed;
                    }

                    for (int k = 0; k < localMesh.triangles.Length; k += 3)
                    {
                        uvList.Add(localMesh.vertices[localMesh.triangles[k + 0]]);
                        uvList.Add(localMesh.vertices[localMesh.triangles[k + 1]]);
                        uvList.Add(localMesh.vertices[localMesh.triangles[k + 2]]);

                        vertList.Add(gridVertices[i]);
                        vertList.Add(gridVertices[i + 1]);
                        vertList.Add(gridVertices[i + 2]);??nope
                    }



                    //Debug.Log("triangle (" + p.p1.x + ", " + p.p1.y + ", " + p.p1.z + ") (" + p.p2.x + ", " + p.p2.y + ", " + p.p2.z + ") (" + p.p3.x + ", " + p.p3.y + ", " + p.p3.z + ")");
                    //Debug.Log("Planar triangle (" + planarP.p1.x + ", " + planarP.p1.y + ") (" + planarP.p2.x + ", " + planarP.p2.y + ") (" + planarP.p3.x + ", " + planarP.p3.y + ")");
                    //Debug.Log("Interpolated (" + interpolated.x + ", " + interpolated.y + ")");
                    /*
                                        uvList.Add(planarP.p1);
                                        uvList.Add(planarP.p2);
                                        uvList.Add(planarP.p3);

                                        vertList.Add(gridVertices[i]);
                                        vertList.Add(gridVertices[i + 1]);
                                        vertList.Add(gridVertices[i + 2]);*/

                    //Debug.Log("UV triangle (" + planarP.p1.x + ", " + planarP.p1.y + ") (" + planarP.p2.x + ", " + planarP.p2.y + ") (" + planarP.p3.x + ", " + planarP.p3.y + ")");
                    //Debug.Log("UV triangle (" + gridVertices[i].x + ", " + gridVertices[i].y + ", " + gridVertices[i].y + ") (" + gridVertices[i + 1].x + ", " + gridVertices[i + 1].y + ", " + gridVertices[i+1].y + ") (" + gridVertices[i + 2].x + ", " + gridVertices[i + 2].y + ", " + gridVertices[i+2].y + ")");
                }
            }
        }

        mesh.vertices = vertList.ToArray();
        mesh.uv = uvList.ToArray();
        fillMeshTriangles();
    }

    private Vector3 rotatePoint(Vector3 pointToRotate, Vector3 centerPoint, float angleInDegrees)
    {
        float angleInRadians = angleInDegrees * 360 * (Mathf.PI / 180);
        float cosTheta = Mathf.Cos(angleInRadians);
        float sinTheta = Mathf.Sin(angleInRadians);
        return new Vector3(
                (cosTheta * (pointToRotate.x - centerPoint.x) - sinTheta * (pointToRotate.y - centerPoint.y) + centerPoint.x),
                (sinTheta * (pointToRotate.x - centerPoint.x) + cosTheta * (pointToRotate.y - centerPoint.y) + centerPoint.y),
                pointToRotate.z
        );
    }

    private Vector3 rotatePoint(Vector3 pointToRotate, float angleInDegrees)
    {
        float angleInRadians = angleInDegrees * 360 * (Mathf.PI / 180);
        float cosTheta = Mathf.Cos(angleInRadians);
        float sinTheta = Mathf.Sin(angleInRadians);
        return new Vector3(
                (cosTheta * (pointToRotate.x) - sinTheta * (pointToRotate.y)),
                (sinTheta * (pointToRotate.x) + cosTheta * (pointToRotate.y)),
                pointToRotate.z
        );
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

    public void renderDistortedMap(Texture2D stamp, Texture2D output, Color background, int pass, int row)
    { renderDistortedMap(stamp, output, background, null, pass, row); }

    public void renderDistortedMap(Texture2D stamp, Texture2D output, Texture2D background, int pass, int row)
    { renderDistortedMap(stamp, output, Color.black, background, pass, row); }

    public void renderDistortedMap(Texture2D stamp, Texture2D output, Color backgroundC, Texture2D backgroundT, int pass, int row)
    {
        // get a temporary RenderTexture. It will be canvas for rendering on it, but not output 
        RenderTexture renderTexture = RenderTexture.GetTemporary(output.width * (pass + 1), output.height * (row + 1));

        // set the RenderTexture as global target (that means GL too)
        RenderTexture.active = renderTexture;

        // render GL immediately to the active render texture
        renderPlanarMeshOnTexture(stamp, backgroundC, backgroundT, pass, row);

        // read the active RenderTexture into a new Texture2D
        output.ReadPixels(new Rect(output.width * pass, output.height * row, output.width * (pass + 1), output.height * (row + 1)), 0, 0);

        // clean up after the party
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        // return the goods
        output.Apply();
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

    private void renderPlanarMeshOnTexture(Texture2D stamp, Color backgroundC, Texture2D backgroundT, int pass, int row)
    {
        GL.PushMatrix();
        GL.LoadPixelMatrix(0 - pass, 1, 1, 0 - row);
        if (backgroundT == null) { setMaterialByColor(backgroundC); }
        else { setMaterialByTexture(backgroundT); }
        Graphics.DrawMeshNow(cleaningMesh, Vector3.zero, Quaternion.identity);
        setMaterialByTexture(stamp);
        Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
        GL.PopMatrix();
    }

    public void renderMapOver(Texture2D stamp, Texture2D output, Texture2D backgroundT, int pass, int row)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(output.width * (pass + 1), output.height * (row + 1));
        RenderTexture.active = renderTexture;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0 - pass, 1, 1, 0);
        setMaterialByTexture(backgroundT);
        Graphics.DrawMeshNow(cleaningMesh, Vector3.zero, Quaternion.identity);
        setMaterialByTexture(stamp);
        Graphics.DrawMeshNow(cleaningMesh, Vector3.zero, Quaternion.identity);
        GL.PopMatrix();

        output.ReadPixels(new Rect(output.width * pass, output.height * row, output.width * (pass + 1), output.height * (row + 1)), 0, 0);
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);
        output.Apply();
    }

    public void WeldVertices(Mesh aMesh, float aMaxDelta = 0.001f)
    {
        var verts = aMesh.vertices;
        var normals = aMesh.normals;
        var uvs = aMesh.uv;
        List<int> newVerts = new List<int>();
        int[] map = new int[verts.Length];
        // create mapping and filter duplicates.
        for (int i = 0; i < verts.Length; i++)
        {
            var p = verts[i];
            var n = normals[i];
            var uv = uvs[i];
            bool duplicate = false;
            for (int i2 = 0; i2 < newVerts.Count; i2++)
            {
                int a = newVerts[i2];
                if (
                    (verts[a] - p).sqrMagnitude <= aMaxDelta && // compare position
                    Vector3.Angle(normals[a], n) <= aMaxDelta && // compare normal
                    (uvs[a] - uv).sqrMagnitude <= aMaxDelta // compare first uv coordinate
                    )
                {
                    map[i] = i2;
                    duplicate = true;
                    break;
                }
            }
            if (!duplicate)
            {
                map[i] = newVerts.Count;
                newVerts.Add(i);
            }
        }
        // create new vertices
        var verts2 = new Vector3[newVerts.Count];
        var normals2 = new Vector3[newVerts.Count];
        var uvs2 = new Vector2[newVerts.Count];
        for (int i = 0; i < newVerts.Count; i++)
        {
            int a = newVerts[i];
            verts2[i] = verts[a];
            normals2[i] = normals[a];
            uvs2[i] = uvs[a];
        }
        // map the triangle to the new vertices
        var tris = aMesh.triangles;
        for (int i = 0; i < tris.Length; i++)
        {
            tris[i] = map[tris[i]];
        }
        aMesh.vertices = verts2;
        aMesh.normals = normals2;
        aMesh.uv = uvs2;
        aMesh.triangles = tris;
    }
}
