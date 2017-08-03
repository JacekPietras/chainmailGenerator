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
		Texture2D distortedMap = RenderToTexture (output, input, normalMap);

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
}
