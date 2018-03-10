using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialEditor : MonoBehaviour
{
	// 3D object that will be raytraced to textures
	public GameObject item;
	// resolution of raytraced maps
	public int itemResolution = 512;
	// resolution of final texture for object
	public int textureResolution = 1024;

	private RingGenerator generator;



	void Start ()
	{
		// generates raytraced maps from 3D object
		generator = new RingGenerator (item, itemResolution);

		// raytracing textures from 3D object
		Texture2D heightMap = generator.getHeightMap ();
		Texture2D normalMap = generator.getNormalMap (30);	

		Triangle2D input = new Triangle2D (
			                   new Vector2 (1, 0),
			                   new Vector2 (1, 1),
			                   new Vector2 (0, 1)
		                   );

		Triangle2D output = new Triangle2D (
			                    new Vector2 (.5f, .5f),
			                    new Vector2 (1, 0),
			                    new Vector2 (0, 0)
		                    );

		// generating distorted texture
		//Texture2D distortedMap = RenderToTexture (output, input, normalMap);
        Texture2D distortedMap = paintAllTriangles(normalMap);

        // map is also saved in asset files so we can use it in other places
        System.IO.File.WriteAllBytes ("Assets/Maps/DistortedMap.png", distortedMap.EncodeToPNG ());
		System.IO.File.WriteAllBytes ("Assets/Maps/NormalMap.png", normalMap.EncodeToPNG ());
		System.IO.File.WriteAllBytes ("Assets/Maps/HeightMap.png", heightMap.EncodeToPNG ());

		// Setting generated texture to object
		GetComponent<Renderer> ().material.mainTexture = distortedMap;
	}

	// points are on outputMap 0..1 - positionInOutput
	// uv are on normalMap 0..1 - positionInInput
	private Texture2D RenderToTexture (Triangle2D positionInOutput, Triangle2D positionInInput, Texture2D source)
	{
		// create material for distortedMap rendering
		Material material = new Material (Shader.Find ("Sprites/Default"));
		material.mainTexture = source;
		material.SetPass (0);

		// get a temporary RenderTexture 
		RenderTexture renderTexture = RenderTexture.GetTemporary (textureResolution, textureResolution);

		// set the RenderTexture as global target (that means GL too)
		RenderTexture.active = renderTexture;

		// render GL immediately to the active render texture
		RenderTriangle (positionInOutput, positionInInput);

		// read the active RenderTexture into a new Texture2D
		Texture2D newTexture = new Texture2D (textureResolution, textureResolution);
		newTexture.ReadPixels (new Rect (0, 0, textureResolution, textureResolution), 0, 0);

		// clean up after the party
		RenderTexture.active = null;
		RenderTexture.ReleaseTemporary (renderTexture);

		// return the goods
		newTexture.Apply ();
		return newTexture;
	}

	private void RenderTriangle (Triangle2D positionInOutput, Triangle2D positionInInput)
	{
		GL.PushMatrix ();

		GL.LoadPixelMatrix (0, 1, 1, 0);
		Graphics.DrawMeshNow (CreateMesh (positionInOutput, positionInInput), Vector3.zero, Quaternion.identity);

		GL.PopMatrix ();
	}

	private Mesh CreateMesh (Triangle2D points, Triangle2D uv)
	{
		Mesh mesh = new Mesh ();

		int[] triangles = new int[] {
			0, 1, 2
		};

		Vector3[] normals = new[] {
			Vector3.forward,
			Vector3.forward,
			Vector3.forward
		};

		mesh.vertices = points.toArray3 ();
		mesh.uv = uv.toArray ();
		mesh.triangles = triangles;
		mesh.normals = normals;

		return mesh;
    }

    Texture2D paintAllTriangles(Texture2D source)
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        
        Triangle2D input = new Triangle2D(
                   new Vector2(1, 0),
                   new Vector2(1, 1),
                   new Vector2(0, 1)
               );
        
        return RenderToTexture2(mesh, input, source);
    }

    private Texture2D RenderToTexture2(Mesh output, Triangle2D positionInInput, Texture2D source)
    {
        // create material for distortedMap rendering
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = source;
        material.SetPass(0);

        // get a temporary RenderTexture 
        RenderTexture renderTexture = RenderTexture.GetTemporary(textureResolution, textureResolution);

        // set the RenderTexture as global target (that means GL too)
        RenderTexture.active = renderTexture;

        // render GL immediately to the active render texture
        RenderTriangle2(output, positionInInput);

        // read the active RenderTexture into a new Texture2D
        Texture2D newTexture = new Texture2D(textureResolution, textureResolution);
        newTexture.ReadPixels(new Rect(0, 0, textureResolution, textureResolution), 0, 0);

        // clean up after the party
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        // return the goods
        newTexture.Apply();
        return newTexture;
    }

    private void RenderTriangle2(Mesh output, Triangle2D positionInInput)
    {
        GL.PushMatrix();

        GL.LoadPixelMatrix(0, 1, 1, 0);
        Graphics.DrawMeshNow(CreateMesh2(output, positionInInput), Vector3.zero, Quaternion.identity);

        GL.PopMatrix();
    }

    private Mesh CreateMesh2(Mesh output, Triangle2D input)
    {
        Mesh mesh = new Mesh();
        Vector2[] inputArray = input.toArray();
        Debug.Log(output.triangles.Length);
        Debug.Log(output.uv.LongLength);
        Debug.Log(output.vertices.Length);

        int[] triangles = new int[output.triangles.Length];
        Vector3[] vertices = new Vector3[output.triangles.Length];
        Vector2[] uv = new Vector2[output.triangles.Length];
        Vector3[] normals = new Vector3[output.triangles.Length];

        for (int i = 0; i < output.uv.Length; i++)
        {
            uv[i] = inputArray[i % 3];
            normals[i] = Vector3.forward;
            vertices[i] = new Vector3(output.uv[i].x, 1-output.uv[i].y);

            Debug.Log(i + " " + vertices[i].x + " " + vertices[i].y);
        }

        for (int i = 0; i < output.triangles.Length; i +=3)
        { 
            Vector2 u1 = output.uv[output.triangles[i + 0]];
            Vector2 u2 = output.uv[output.triangles[i + 1]];
            Vector2 u3 = output.uv[output.triangles[i + 2]];

            vertices[i] = new Vector3(u1.x, 1 - u1.y);
            vertices[i + 1] = new Vector3(u2.x, 1 - u2.y);
            vertices[i + 2] = new Vector3(u3.x, 1 - u3.y);

            uv[i] = inputArray[0];
            uv[i+1] = inputArray[1];
            uv[i+2] = inputArray[2];
            triangles[i] = i;
            triangles[i+1] = i+1;
            triangles[i+2] = i+2;
            normals[i] = Vector3.forward;
            normals[i+1] = Vector3.forward;
            normals[i+2] = Vector3.forward;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.normals = normals;

        return mesh;
    }
}
