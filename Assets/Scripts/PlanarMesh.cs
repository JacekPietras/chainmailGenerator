﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlanarMesh {
    private Mesh mesh;
    private Mesh cleaningMesh;
    private int normalizationStepMax = 10;
    // debug option to draw out progress of normalization in bitmaps
    private bool showingNormalization = false;
    public Texture2D[] texList;
    private int normalizationStep = 0;
    private float normalizationStrength = 0.5f;
    private int neighbourRadius = 1;
    private bool detectOverlappingOnAllTriangles = false;
    private bool detectOverlappingOnAllEdges = false;
    private bool distortMother = false;
    private bool useStrength = true;
    private bool alwaysBuildBestMesh = false;

    // lists of information corresponding every 3D triangle from source 3D object
    private List<TextureObject>[] textureObjectsOnTriangles;
    // for every point from mesh3d.triangles assigns 
    private Vector3[] gridVertices;
    private Neighbour[] neighbours;

    public PlanarMesh() {
        createCleaningMesh();
    }

    public PlanarMesh(
            Mesh mesh3d,
            Texture2D objectMap,
            int normalizationSteps,
            float normalizationStrength,
            bool showingNormalization,
            int neighbourRadius,
            bool detectOverlappingOnAllTriangles,
            bool detectOverlappingOnAllEdges,
            bool distortMother,
            bool useStrength,
            bool alwaysBuildBestMesh) {
        this.neighbourRadius = neighbourRadius;
        this.normalizationStepMax = normalizationSteps;
        this.normalizationStrength = normalizationStrength;
        this.showingNormalization = showingNormalization;
        this.detectOverlappingOnAllTriangles = detectOverlappingOnAllTriangles;
        this.detectOverlappingOnAllEdges = detectOverlappingOnAllEdges;
        this.distortMother = distortMother;
        this.useStrength = useStrength;
        this.alwaysBuildBestMesh = alwaysBuildBestMesh;

        createPlanarMesh(mesh3d, createObjects(objectMap));
        createCleaningMesh();
        texList = new Texture2D[normalizationStepMax + 1];
    }

    private void createCleaningMesh() {
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
        for (int i = 0; i < vertices.Length; i++) {
            uv[i] = new Vector2(vertices[i].x, 1 - vertices[i].y);
            triangles[i] = i;
            normals[i] = Vector3.forward;
        }

        cleaningMesh.vertices = vertices;
        cleaningMesh.uv = uv;
        cleaningMesh.triangles = triangles;
        cleaningMesh.normals = normals;
    }

    private void createPlanarMesh(Mesh mesh3d, List<TextureObject> objects) {
        mesh = new Mesh();
        Debug.Log("Triangles count " + mesh3d.triangles.Length / 3);
        Debug.Log("UV count " + mesh3d.uv.LongLength);
        Debug.Log("Vertices count " + mesh3d.vertices.Length);

        gridVertices = new Vector3[mesh3d.triangles.Length];
        textureObjectsOnTriangles = new List<TextureObject>[mesh3d.triangles.Length / 3];
        neighbours = new Neighbour[mesh3d.triangles.Length / 3];

        // iterating through every triangle
        for (int i = 0; i < mesh3d.triangles.Length; i += 3) {
            // UV triangle from 3D model
            Vector2 u1 = mesh3d.uv[mesh3d.triangles[i + 0]];
            Vector2 u2 = mesh3d.uv[mesh3d.triangles[i + 1]];
            Vector2 u3 = mesh3d.uv[mesh3d.triangles[i + 2]];
            //Triangle3D vvv = new Triangle3D(u1, u2, u3);
            //vvv.print("index " + i / 3);

            // verticles of new mesh will be created from UV points of 3D object
            // we need to normalize Y value because it's for texture
            gridVertices[i + 0] = new Vector3(u1.x, 1 - u1.y);
            gridVertices[i + 1] = new Vector3(u2.x, 1 - u2.y);
            gridVertices[i + 2] = new Vector3(u3.x, 1 - u3.y);

            foreach (TextureObject obj in objects) {
                Barycentric a = new Barycentric(u1, u2, u3, obj.toVector2());
                if (a.IsInside) {
                    if (textureObjectsOnTriangles[i / 3] == null)
                        textureObjectsOnTriangles[i / 3] = new List<TextureObject>();
                    textureObjectsOnTriangles[i / 3].Add(obj.copyWithBarycentric(a));

                    if (neighbours[i / 3] == null) {
                        neighbours[i / 3] = new NeighbourCreator(mesh3d, i, neighbourRadius).create();
                    }
                }
            }
        }
    }

    // recalculate positions of planar mesh verticles
    public void updateMesh(Mesh mesh3d) {
       // if (showingNormalization && normalizationStep >= normalizationStepMax) {
       //     return;
       // } else
        if (showingNormalization) {
            Debug.Log("-------------- " + normalizationStep + " ----------------");
        } else {
            Debug.Log("-------------------------------------");
        }
        List<Vector2> uvList = new List<Vector2>();
        List<Vector3> vertList = new List<Vector3>();

        // iterating through every triangle
        for (int i = 0; i < mesh3d.triangles.Length; i += 3) {
            // we should do operations only if there is at least one point on current triangle
            if (textureObjectsOnTriangles[i / 3] != null) {
                Neighbour neighbour = neighbours[i / 3];
                // mesh of triangle with his neighbours
                MeshFlat localMesh = new MeshFlat(
                    mesh3d,
                    neighbour,
                    normalizationStrength,
                    detectOverlappingOnAllTriangles,
                    detectOverlappingOnAllEdges,
                    distortMother,
                    useStrength,
                    textureObjectsOnTriangles[i / 3]);

                if (showingNormalization || neighbour.usedTriangles == null || alwaysBuildBestMesh) {
                    // calculations with selecting which triangles we should use
                    // for optimization reasons normally choosed once

                    localMesh.buildFirstUsedTriangles();

                    do {
                        localMesh.makeEdges();

                        if (showingNormalization) {
                            localMesh.normalizeFlatMesh(normalizationStep);
                        } else {
                            localMesh.normalizeFlatMesh(normalizationStepMax);
                        }

                    } while (!localMesh.checkForOutsiders());

                    neighbour.setUsedTriangles(localMesh.usedTriangles);
                } else {
                    // we know which triangles we should use, but we need to normalize them again
                    // 3D mesh is in motion

                    localMesh.makeEdges();
                    localMesh.normalizeFlatMesh(normalizationStepMax);
                }

                localMesh.printError();

                foreach (TextureObject obj in localMesh.objects) {
                    Vector3[] transformedVerticles = localMesh.getTransformedByObject(obj);
                    Texture2D tex = createTextureForDebug();

                    foreach (int k in neighbour.usedTriangles) {
                        Triangle3D triangle = new Triangle3D(transformedVerticles[localMesh.triangles[k + 0]],
                                                    transformedVerticles[localMesh.triangles[k + 1]],
                                                    transformedVerticles[localMesh.triangles[k + 2]]);

                        uvList.Add(triangle.p1);
                        uvList.Add(triangle.p2);
                        uvList.Add(triangle.p3);

                        vertList.Add(gridVertices[neighbour.index[k + 0]]);
                        vertList.Add(gridVertices[neighbour.index[k + 1]]);
                        vertList.Add(gridVertices[neighbour.index[k + 2]]);

                        drawDebugTriangle(tex, triangle, k);
                    }
                    applyDebugTexture(tex);
                }
            }
        }

        if (normalizationStep < normalizationStepMax) {
            normalizationStep++;
        }

        int[] triangles = new int[vertList.Count];
        Vector3[] normals = new Vector3[vertList.Count];
        Vector3[] vertices = vertList.ToArray();
        // iterating through every point
        for (int i = 0; i < vertList.Count; i++) {
            // filling positions of points in triangle
            triangles[i] = i;
            // all normals will be fixed
            normals[i] = Vector3.forward;
        }
        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvList.ToArray();
    }

    private Texture2D createTextureForDebug() {
        if (!showingNormalization) {
            return null;
        }
        Texture2D tex = new Texture2D(1024, 1024);
        DrawEdge(tex, 0, 0, 0, 1, -1);
        DrawEdge(tex, 0, 0, 1, 0, -1);
        DrawEdge(tex, 1, 1, 1, 0, -1);
        DrawEdge(tex, 1, 1, 0, 1, -1);
        return tex;
    }

    private void applyDebugTexture(Texture2D tex) {
        if (!showingNormalization) {
            return;
        }
        tex.Apply();
        texList[normalizationStep] = tex;
    }

    private void drawDebugTriangle(Texture2D tex, Triangle3D tri, int color) {
        DrawEdge(tex, tri.p1.x, tri.p1.y, tri.p2.x, tri.p2.y, color);
        DrawEdge(tex, tri.p1.x, tri.p1.y, tri.p3.x, tri.p3.y, color);
        DrawEdge(tex, tri.p3.x, tri.p3.y, tri.p2.x, tri.p2.y, color);
    }

    void DrawEdge(Texture2D tex, float x0, float y0, float x1, float y1, int index) {
        if (!showingNormalization) {
            return;
        }
        int padding = (int)(tex.width * 0.45f);
        int size = tex.width - 2 * padding;
        Color color = Color.blue;

        if (index == -1) {
            color = Color.black;
        } else if (index == -2) {
            color = Color.gray;
        } else if (index == 0) {
            color = Color.red;
        }

        DrawLine(tex, padding + (int)(size * x0), padding + (int)(size * y0), padding + (int)(size * x1), padding + (int)(size * y1), color);
    }

    void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col) {
        int dy = (int)(y1 - y0);
        int dx = (int)(x1 - x0);
        int stepx, stepy;

        if (dy < 0) { dy = -dy; stepy = -1; } else { stepy = 1; }
        if (dx < 0) { dx = -dx; stepx = -1; } else { stepx = 1; }
        dy <<= 1;
        dx <<= 1;

        float fraction = 0;
        if (x0 >= 0 && x0 <= 1024 && y0 >= 0 && y0 <= 1024)
            tex.SetPixel(x0, y0, col);
        if (dx > dy) {
            fraction = dy - (dx >> 1);
            while (Mathf.Abs(x0 - x1) > 1) {
                if (fraction >= 0) {
                    y0 += stepy;
                    fraction -= dx;
                }
                x0 += stepx;
                fraction += dy;
                if (x0 >= 0 && x0 <= 1024 && y0 >= 0 && y0 <= 1024)
                    tex.SetPixel(x0, y0, col);
            }
        } else {
            fraction = dx - (dy >> 1);
            while (Mathf.Abs(y0 - y1) > 1) {
                if (fraction >= 0) {
                    x0 += stepx;
                    fraction -= dy;
                }
                y0 += stepy;
                fraction += dx;
                if (x0 >= 0 && x0 <= 1024 && y0 >= 0 && y0 <= 1024)
                    tex.SetPixel(x0, y0, col);
            }
        }
    }

    public void fillMeshTriangles() {
        int[] triangles = new int[mesh.vertices.Length];
        Vector3[] normals = new Vector3[mesh.vertices.Length];
        // iterating through every point
        for (int i = 0; i < mesh.vertices.Length; i++) {
            // filling positions of points in triangle
            triangles[i] = i;
            // all normals will be fixed
            normals[i] = Vector3.forward;
        }
        mesh.triangles = triangles;
        mesh.normals = normals;
    }

    // returns list of texture objects on inputed texture
    // they have relative position, rotation, scale
    private List<TextureObject> createObjects(Texture2D objectMap) {
        try {
            Color color;
            List<TextureObject> objects = new List<TextureObject>();
            for (int i = 0; i < objectMap.width; i++) {
                for (int j = 0; j < objectMap.height; j++) {
                    if ((color = objectMap.GetPixel(i, j)) != Color.black) {
                        TextureObject obj = new TextureObject(i / (float)objectMap.width, j / (float)objectMap.height, color);
                        Debug.Log("Found object (" + obj.x + ", " + obj.y + ")");
                        objects.Add(obj);
                    }
                }
            }
            return objects;
        } catch (Exception ignored) {
            Debug.LogError(ignored.Data);
            // use in case of error with importer.
            SetTextureImporterFormat(objectMap, true);
            return createObjects(objectMap);
        }
    }

    // Used for possible probme when texture wasn't imported successfully
    public static void SetTextureImporterFormat(Texture2D texture, bool isReadable) {
        if (null == texture) return;

        string assetPath = AssetDatabase.GetAssetPath(texture);
        var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (tImporter != null) {
            tImporter.textureType = TextureImporterType.Default;

            tImporter.isReadable = isReadable;

            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
        }
    }

    public void renderDistortedMap(Texture2D stamp, Texture2D output, Color background, int pass, int row) { renderDistortedMap(stamp, output, background, null, pass, row); }

    public void renderDistortedMap(Texture2D stamp, Texture2D output, Texture2D background, int pass, int row) { renderDistortedMap(stamp, output, Color.black, background, pass, row); }

    public void renderDistortedMap(Texture2D stamp, Texture2D output, Color backgroundC, Texture2D backgroundT, int pass, int row) {
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

    private void setMaterialByTexture(Texture2D stamp) {
        // create material for distortedMap rendering
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = stamp;
        material.SetPass(0);
    }

    private void setMaterialByColor(Color color) {
        // create material for distortedMap rendering
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.color = color;
        material.SetPass(0);
    }

    private void renderPlanarMeshOnTexture(Texture2D stamp, Color backgroundC, Texture2D backgroundT, int pass, int row) {
        GL.PushMatrix();
        GL.LoadPixelMatrix(0 - pass, 1, 1, 0 - row);
        if (backgroundT == null) { setMaterialByColor(backgroundC); } else { setMaterialByTexture(backgroundT); }
        Graphics.DrawMeshNow(cleaningMesh, Vector3.zero, Quaternion.identity);
        setMaterialByColor(new Color(1, 0, 0, 1));
        Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
        setMaterialByTexture(stamp);
        Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);
        GL.PopMatrix();
    }

    public void renderMapOver(Texture2D stamp, Texture2D output, Texture2D backgroundT, int pass, int row) {
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
}
