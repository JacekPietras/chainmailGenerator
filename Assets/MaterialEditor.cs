using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialEditor : MonoBehaviour
{
	public GameObject item;
	public int itemResolution = 512;
	public int textureResolution = 1024;
	private Texture2D Texture2;
	private RingGenerator generator;
	private static Material flatMaterial;


	void Start ()
	{
		generator = new RingGenerator (item, itemResolution);
		generator.getHeightMap ();
		Texture2D stamp = generator.getNormalMap (30);
		//Texture2D stamp = generator.getHeightMap();

		//colorizeTriangles();
		//colorizeTrianglgenerateRing();

		// create material for GL rendering
		//Material material = new Material (Shader.Find ("GUI/Text Shader"));
		flatMaterial = getFlatMaterial ();
		//material.mainTexture = stamp;

		Texture2D texture = RenderGLToTexture (textureResolution, textureResolution, stamp);
		texture.Apply ();
		System.IO.File.WriteAllBytes ("Assets/DupaMap.png", texture.EncodeToPNG ());

		GetComponent<Renderer> ().material.mainTexture = texture;
	}

	static Material getFlatMaterial ()
	{
		Material material = new Material (Shader.Find ("Sprites/Default"));
		//material.hideFlags = HideFlags.HideAndDontSave;
		//material.shader.hideFlags = HideFlags.HideAndDontSave;
		material.SetPass (0);

		return material;
	}

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

	// Class helps to generate heightMap and normalMap
	// It is raytracing input 3D model to planar textures
	public class RingGenerator
	{
		// resolutian of both output textures
		public int resolution = 512;

		// item is 3D object that will be raytraced to textures
		private GameObject item;

		// generated textures
		private Texture2D textureHeight;
		private Texture2D normalTexture;
		// generated 2D array of raytraced height
		// we don't using generated heightMap because
		// in float we can have more accurate data
		private float[,] heights;

		public RingGenerator (GameObject item, int resolution)
		{
			this.item = item;
			if (resolution > 0)
				this.resolution = resolution;

		}
			
		// returns generated heightMap of input 3D object
		public Texture2D getHeightMap ()
		{
			// if texture was already generated there is no need
			// to generate it again
			if (textureHeight != null)
				return textureHeight;
			
			MeshCollider collider = item.GetComponent<MeshCollider> ();
			GameObject go = null;
			if (!collider) {
				//Add a collider to our source object if it does not exist.
				go = Instantiate (item, new Vector3 (), Quaternion.Euler (-90, 0, 0)) as GameObject;
				collider = go.AddComponent<MeshCollider> ();
			}
			Bounds bounds = collider.bounds;
			textureHeight = new Texture2D (resolution, resolution, TextureFormat.ARGB32, false);

			// Do raycasting samples over the object to see what terrain heights should be
			heights = new float[resolution, resolution];
			Ray ray = new Ray (new Vector3 (bounds.min.x, bounds.max.y + bounds.size.y, bounds.min.z), -Vector3.up);
			RaycastHit hit = new RaycastHit ();
			float meshHeightInverse = 1 / bounds.size.y;
			Vector3 rayOrigin = ray.origin;

			int maxHeight = heights.GetLength (0);
			int maxLength = heights.GetLength (1);

			float top = 0, bottom = 1;
			float height = 0.0f;

			Vector2 stepXZ = new Vector2 (bounds.size.x / maxLength, bounds.size.z / maxHeight);

			for (int zCount = 0; zCount < maxHeight; zCount++) {
				for (int xCount = 0; xCount < maxLength; xCount++) {

					height = 0.0f;

					if (collider.Raycast (ray, out hit, bounds.size.y * 3)) {
						height = (hit.point.y - bounds.min.y) * meshHeightInverse;
					}
					//clamp
					if (height <= 0)
						height = 0;
					else {
						if (height < bottom)
							bottom = height;
						if (height > top)
							top = height;
					}

					heights [zCount, xCount] = height;
					rayOrigin.x += stepXZ [0];
					ray.origin = rayOrigin;
				}

				rayOrigin.z += stepXZ [1];
				rayOrigin.x = bounds.min.x;
				ray.origin = rayOrigin;
			}

			float mult = 1f / (top - bottom);

			for (int zCount = 0; zCount < maxHeight; zCount++) {
				for (int xCount = 0; xCount < maxLength; xCount++) {
					height = heights [zCount, xCount];
					//clamp negative value as black color
					if (height <= 0) {
						textureHeight.SetPixel (zCount, xCount, new Color (0, 0, 0, 0));
					} else {
						height = (height - bottom) * mult;
						textureHeight.SetPixel (zCount, xCount, new Color (height, height, height, 1));
					}
				}
			}

			// Actually apply all 'setPixel' changes
			textureHeight.Apply ();

			// objct was created only for raycasting, we don't need it now
			if (go != null)
				Destroy (go);

			// map is also saved in asset files so we can use it in other places
			System.IO.File.WriteAllBytes ("Assets/HeightMap.png", textureHeight.EncodeToPNG ());

			return textureHeight;
		}

		// returns generated normalMap of input 3D object
		// strength in argument is for how raised output texture should be
		public Texture2D getNormalMap (float strength)
		{
			// if texture was already generated there is no need
			// to generate it again
			if (normalTexture != null)
				return normalTexture;

			normalTexture = new Texture2D (textureHeight.width, textureHeight.height, TextureFormat.RGB24, textureHeight.mipmapCount > 1);
			Color[] nPixels = new Color[heights.Length];

			for (int y = 0; y < textureHeight.height; y++) {
				for (int x = 0; x < textureHeight.width; x++) {
					int x_1 = x - 1;
					if (x_1 < 0)
						x_1 = textureHeight.width - 1; // repeat the texture so use the opposit side
					int x1 = x + 1;
					if (x1 >= textureHeight.width)
						x1 = 0; // repeat the texture so use the opposit side
					int y_1 = y - 1;
					if (y_1 < 0)
						y_1 = textureHeight.height - 1; // repeat the texture so use the opposit side
					int y1 = y + 1;
					if (y1 >= textureHeight.height)
						y1 = 0; // repeat the texture so use the opposit side
					float grayX_1 = heights [x_1, y];
					float grayX1 = heights [x1, y];
					float grayY_1 = heights [x, y_1];
					float grayY1 = heights [x, y1];
					Vector3 vx = new Vector3 (0, 1, (grayX_1 - grayX1) * strength);
					Vector3 vy = new Vector3 (1, 0, (grayY_1 - grayY1) * strength);
					Vector3 n = Vector3.Cross (vy, vx).normalized;
					nPixels [(y * textureHeight.width) + x] = (Vector4)((n + Vector3.one) * 0.5f);
				}
			}

			normalTexture.SetPixels (nPixels, 0);
			// Actually apply all 'setPixel' changes
			normalTexture.Apply ();

			// map is also saved in asset files so we can use it in other places
			System.IO.File.WriteAllBytes ("Assets/NormalMap.png", normalTexture.EncodeToPNG ());
			return normalTexture;
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
		//mesh.RecalculateNormals ();

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
