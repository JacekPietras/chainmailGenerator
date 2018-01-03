using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bash : MonoBehaviour
{
	public GameObject item;
	public int itemResolution = 512;
	public int textureResolution = 1024;
	private Texture2D Texture2;
	private RingGenerator generator;
	private static Material flatMaterial;

	void colorizeTriangles ()
	{
		Texture2 = new Texture2D (itemResolution, itemResolution, TextureFormat.ARGB32, false);

		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		TriUv[] tris = new TriUv[mesh.triangles.Length / 3];
		for (int i = 0; i < tris.Length; i++) {
			tris [i] = new TriUv (mesh, i * 3);
		}
		tris [0].u = tris [0].p.planar ();
		tris [0].draw (mesh, Texture2);

		Texture2.Apply ();
		GetComponent<Renderer> ().material.mainTexture = Texture2;
	}

	void colorizeTrianglgenerateRing ()
	{
		generator = new RingGenerator (item, itemResolution);
		GetComponent<Renderer> ().material.mainTexture = generator.getHeightMap ();
		GetComponent<Renderer> ().material.SetTexture ("_ParallaxMap", generator.getHeightMap ());
		GetComponent<Renderer> ().material.SetTexture ("_BumpMap", generator.getNormalMap (30));
	}

	public class TriUv
	{
		public Triangle2D u;
		public Triangle3D p;

		public TriUv (Mesh mesh, int index)
		{
			Vector3 p1 = mesh.vertices [index];
			Vector3 p2 = mesh.vertices [index + 1];
			Vector3 p3 = mesh.vertices [index + 2];

			p = new Triangle3D (p1, p2, p3);

			Vector3 u1 = mesh.uv [index];
			Vector3 u2 = mesh.uv [index + 1];
			Vector3 u3 = mesh.uv [index + 2];

			u = new Triangle2D (u1, u2, u3);
		}

		public void draw (Mesh mesh, Texture2D texture)
		{
			for (int i = 0; i < texture.width; i++) {
				for (int j = 0; j < texture.height; j++) {
					float px = i, py = j;
					px /= texture.width;
					py /= texture.height;
					if (u.pointInside (new Vector2 (px, py)))
						texture.SetPixel (i, j, Color.red);
					else
						texture.SetPixel (i, j, Color.blue);
				}
			}
		}
	}

	public static Mesh CreateMesh (int width, int height)
	{

		Mesh mesh = new Mesh ();

		Vector3[] vertices = new Vector3[] {
			new Vector3 (.5f * width, height, 0),
			new Vector3 (width, 0, 0),
			new Vector3 (0, 0, 0)
		};

		Vector2[] uv = new Vector2[] {
			new Vector2 (1, 0),
			new Vector2 (1, 1),
			new Vector2 (0, 1)
		};

		int[] triangles = new int[] {
			0, 1, 2
		};

		Vector3[] normals = new[] {
			Vector3.forward,
			Vector3.forward,
			Vector3.forward
		};

		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh.normals = normals;

		return mesh;
	}

	static Texture2D RenderGLToTexture (int width, int height, Texture2D source)
	{
		// get a temporary RenderTexture 
		RenderTexture renderTexture = RenderTexture.GetTemporary (width, height);

		// set the RenderTexture as global target (that means GL too)
		RenderTexture.active = renderTexture;

		// clear GL
		GL.Clear (false, true, Color.black);

		// render GL immediately to the active render texture
		RenderGLStuff (width, height, source);

		// read the active RenderTexture into a new Texture2D
		Texture2D newTexture = new Texture2D (width, height);
		newTexture.ReadPixels (new Rect (0, 0, width, height), 0, 0);

		// clean up after the party
		RenderTexture.active = null;
		RenderTexture.ReleaseTemporary (renderTexture);

		// return the goods
		return newTexture;
	}

	static void RenderGLStuff (int width, int height, Texture2D source)
	{

		//MaterialPropertyBlock mpb = new MaterialPropertyBlock ();
		//mpb.AddTexture ("_MainTex", source);

		GL.PushMatrix ();
		GL.LoadPixelMatrix (0, width, height, 0);
		//flatMaterial.SetPass (0);


		//Graphics.DrawMeshNow (CreateMesh (width, height), Vector3.zero, Quaternion.identity);
		Graphics.DrawTexture (new Rect (width - 200, height - 200, 200, 200), source);
		//Graphics.DrawTexture (new Rect (width - 200, height - 200, 200, 200), source, flatMaterial, 0);
		flatMaterial.mainTexture = source;
		flatMaterial.SetPass (0);
		GL.LoadPixelMatrix (0, width, height, 0);
		//GL.LoadOrtho ();
		Graphics.DrawMeshNow (CreateMesh (width, height), Vector3.zero, Quaternion.identity);
		//Graphics.DrawMesh (CreateMesh (width, height), Vector3.zero, Quaternion.identity, flatMaterial, 0, Camera.main, 0, mpb);
		//Graphics.DrawMesh (CreateMesh (width, height), Vector3.zero, Quaternion.identity, flatMaterial, 0);


		GL.PopMatrix ();
		/*

		GL.PushMatrix ();

		//Material material = new Material (Shader.Find ("FlatShader"));
		//material.SetPass (0);

		GL.LoadOrtho ();
		//GL.LoadPixelMatrix (0, 1, 0, 1);
		GL.Begin (GL.TRIANGLES);
		GL.TexCoord2 (1, 0);
		GL.Vertex3 (0.5f, 0, 0);
		GL.TexCoord2 (1, 1);
		GL.Vertex3 (1, 1, 0);
		GL.TexCoord2 (0, 1);
		GL.Vertex3 (0, 1, 0);
		GL.End ();
		GL.PopMatrix ();*/
	}
}
