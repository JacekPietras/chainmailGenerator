using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	private float strengthOfGeneratedNormalMap = -1f;

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
			go = GameObject.Instantiate (item, new Vector3 (), Quaternion.Euler (-90, 0, 0)) as GameObject;
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
			GameObject.Destroy (go);

		return textureHeight;
	}

	// returns generated normalMap of input 3D object
	// strength in argument is for how raised output texture should be
	public Texture2D getNormalMap (float strength = 30)
	{
		// if texture was already generated there is no need
		// to generate it again
		// also strength must be the same
		if (normalTexture != null && strengthOfGeneratedNormalMap == strength)
			return normalTexture;
		else
			strengthOfGeneratedNormalMap = strength;

        // If heightMap wasn't generated, we should generate it now
        if (textureHeight == null)
            getHeightMap();

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

		return normalTexture;
	}
}