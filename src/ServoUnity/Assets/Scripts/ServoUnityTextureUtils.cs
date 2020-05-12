// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla.
//
// Author(s): Philip Lamb, Patrick O'Shaughnessey


using UnityEngine;

public class ServoUnityTextureUtils : MonoBehaviour
{
    public static Texture2D CreateTexture(int width, int height, TextureFormat format)
    {
        // Check parameters.
        if (width <= 0 || height <= 0)
        {
            Debug.LogError("Error: Cannot configure video texture with invalid size: " + width + "x" + height);
            return null;
        }
        
        Texture2D vt = new Texture2D(width, height, format, false);
        vt.hideFlags = HideFlags.HideAndDontSave;
        vt.filterMode = FilterMode.Bilinear;
        vt.wrapMode = TextureWrapMode.Clamp;
        vt.anisoLevel = 0;

        // Initialise the video texture to black.
        Color32[] arr = new Color32[width * height];
        Color32 blackOpaque = new Color32(0, 0, 0, 255);
        for (int i = 0; i < arr.Length; i++) arr[i] = blackOpaque;
        vt.SetPixels32(arr);
        vt.Apply(); // Pushes all SetPixels*() ops to texture.
        arr = null;

        return vt;
    }

    // Creates a GameObject in layer 'layer' which renders a mesh displaying the video stream.
    public static GameObject Create2DVideoSurface(Texture2D vt, float textureScaleU, float textureScaleV, float width,
        float height, int layer, bool flipX, bool flipY)
    {
        // Check parameters.
        if (!vt)
        {
            Debug.LogError("Error: CreateWindowMesh null Texture2D");
            return null;
        }

        // Create new GameObject to hold mesh.
        GameObject vmgo = new GameObject("Video source");
        if (vmgo == null)
        {
            Debug.LogError("Error: CreateWindowMesh cannot create GameObject.");
            return null;
        }

        vmgo.layer = layer;

        // Create a material which uses our "TextureNoLight" shader, and paints itself with the texture.
        Shader shaderSource = Shader.Find("TextureAlphaNoLight");
        Material vm = new Material(shaderSource); //fxrUnity.Properties.Resources.VideoPlaneShader;
        vm.hideFlags = HideFlags.HideAndDontSave;
        //Debug.Log("Created video material");

        MeshFilter filter = vmgo.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = vmgo.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
 
        vmgo.GetComponent<Renderer>().material = vm;
        vmgo.AddComponent<MeshCollider>();

        Configure2DVideoSurface(vmgo, vt, textureScaleU, textureScaleV, width, height, flipX, flipY);
        return vmgo;
    }

    public static Mesh CreateVideoMesh(float textureScaleU, float textureScaleV, float width, float height, bool flipX, bool flipY)
    {
        // Now create a mesh appropriate for displaying the video, a mesh filter to instantiate that mesh,
        // and a mesh renderer to render the material on the instantiated mesh.
        Mesh m = new Mesh();
        m.Clear();
        m.vertices = new Vector3[]
        {
            new Vector3(-width * 0.5f, 0.0f, 0.0f),
            new Vector3(width * 0.5f, 0.0f, 0.0f),
            new Vector3(width * 0.5f, height, 0.0f),
            new Vector3(-width * 0.5f, height, 0.0f),
        };
        m.normals = new Vector3[]
        {
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
        };
        float u1 = flipX ? textureScaleU : 0.0f;
        float u2 = flipX ? 0.0f : textureScaleU;
        float v1 = flipY ? textureScaleV : 0.0f;
        float v2 = flipY ? 0.0f : textureScaleV;
        m.uv = new Vector2[] {
            new Vector2(u1, v1),
            new Vector2(u2, v1),
            new Vector2(u2, v2),
            new Vector2(u1, v2)
        };
        m.triangles = new int[] {
            2, 1, 0,
            3, 2, 0
        };


        return m;
    }

    public static void Configure2DVideoSurface(GameObject vmgo, Texture2D vt, float textureScaleU, float textureScaleV,
        float width, float height, bool flipX, bool flipY)
    {
        // Check parameters.
        if (!vt) {
            Debug.LogError("Error: CreateWindowMesh null Texture2D");
            return;
        }
        // Create the mesh
        var m = ServoUnityTextureUtils.CreateVideoMesh(textureScaleU, textureScaleV, width, height, flipX, flipY);
        
        // Assign the texture to the window's material
        Material vm = vmgo.GetComponent<Renderer>().material;
        vm.mainTexture = vt;

        // Assign the mesh to the mesh filter
        MeshFilter filter = vmgo.GetComponent<MeshFilter>();
        filter.mesh = m;

        // Update the mesh collider mesh
        MeshCollider vmc = vmgo.GetComponent<MeshCollider>();;
        vmc.sharedMesh = filter.sharedMesh;
    }
}