// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla.
//
// Author(s): Philip Lamb, Patrick O'Shaughnessey

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UI;

public class ServoUnityWindow : ServoUnityPointableSurface
{
    public static Vector2Int DefaultSizeToRequest = new Vector2Int(1920, 1080);

    public bool flipX = false;
    public bool flipY = false;
    private static float DefaultWidth = 3.0f;
    private float Width = DefaultWidth;
    private float Height;
    private float textureScaleU;
    private float textureScaleV;

    private GameObject _videoMeshGO = null; // The GameObject which holds the MeshFilter and MeshRenderer for the video. 

    private Texture2D _videoTexture = null; // Texture object with the video image.

    private TextureFormat _textureFormat;

    public Vector2Int PixelSize
    {
        get => videoSize;
        private set { videoSize = value; }
    }

    public TextureFormat TextureFormat
    {
        get => _textureFormat;
        private set { _textureFormat = value; }
    }

    public static ServoUnityWindow FindWindowWithUID(int uid)
    {
        Debug.Log("ServoUnityWindow.FindWindowWithUID(uid:" + uid + ")");
        if (uid != 0)
        {
            ServoUnityWindow[] windows = GameObject.FindObjectsOfType<ServoUnityWindow>();
            foreach (ServoUnityWindow window in windows)
            {
                if (window.GetInstanceID() == uid) return window;
            }
        }

        return null;
    }

    // TODO: This is only necessary in the current state of affairs where we are sharing a video texture id between video and windows...
    public void RecreateVideoTexture()
    {
        _videoTexture = CreateWindowTexture(videoSize.x, videoSize.y, _textureFormat, out textureScaleU,
            out textureScaleV);
        _videoMeshGO.GetComponent<Renderer>().material.mainTexture = _videoTexture;
    }

    public static ServoUnityWindow CreateNewInParent(GameObject parent)
    {
        Debug.Log("ServoUnityWindow.CreateNewInParent(parent:" + parent + ")");
        ServoUnityWindow window = parent.AddComponent<ServoUnityWindow>();
        return window;
    }

    private void OnEnable()
    {
    }

    public bool Visible
    {
        get => visible;
        set
        {
            visible = value;
            if (_videoMeshGO != null)
            {
                _videoMeshGO.SetActive(visible);
            }
        }
    }

    private bool visible = true;

    public int WindowIndex
    {
        get => _windowIndex;
    }

    private void OnDisable()
    {
    }

    private void HandleCloseKeyPressed()
    {
    }

    private void HandleKeyPressed(int keycode)
    {
        // TODO: All windows will respond to all keyboard presses. Since we only ever have one at the moment...
        servo_unity_plugin.ServoUnityKeyEvent(_windowIndex, keycode);
    }

    void Start()
    {
        Debug.Log("ServoUnityWindow.Start()");

        if (_windowIndex == 0) {
            servo_unity_plugin?.ServoUnityRequestNewWindow(GetInstanceID(), DefaultSizeToRequest.x, DefaultSizeToRequest.y);
        }
    }

    public void Close()
    {
        Debug.Log("ServoUnityWindow.Close()");
        if (_windowIndex == 0) return;

        servo_unity_plugin?.ServoUnityCloseWindow(_windowIndex);
        _windowIndex = 0;
    }

    public void RequestSizeMultiple(float sizeMultiple)
    {
        Width = DefaultWidth * sizeMultiple;
        Resize(Mathf.FloorToInt(DefaultSizeToRequest.x * sizeMultiple),
            Mathf.FloorToInt(DefaultSizeToRequest.y * sizeMultiple));
    }

    public bool Resize(int widthPixels, int heightPixels)
    {
        return servo_unity_plugin.ServoUnityRequestWindowSizeChange(_windowIndex, widthPixels, heightPixels);
    }

    public void WasCreated(int windowIndex, int widthPixels, int heightPixels, TextureFormat format)
    {
        Debug.Log("ServoUnityWindow.WasCreated(windowIndex:" + windowIndex + ", widthPixels:" + widthPixels +
                  ", heightPixels:" + heightPixels + ", format:" + format + ")");
        _windowIndex = windowIndex;
        Height = (Width / widthPixels) * heightPixels;
        videoSize = new Vector2Int(widthPixels, heightPixels);
        _textureFormat = format;
        _videoTexture = CreateWindowTexture(videoSize.x, videoSize.y, _textureFormat, out textureScaleU, out textureScaleV);
        _videoMeshGO = ServoUnityTextureUtils.Create2DVideoSurface(_videoTexture, textureScaleU, textureScaleV, Width, Height,
            0, flipX, flipY);
        _videoMeshGO.transform.parent = this.gameObject.transform;
        _videoMeshGO.transform.localPosition = Vector3.zero;
        _videoMeshGO.transform.localRotation = Quaternion.identity;
        _videoMeshGO.SetActive(Visible);
    }

    public void WasResized(int widthPixels, int heightPixels)
    {
        Height = (Width / widthPixels) * heightPixels;
        videoSize = new Vector2Int(widthPixels, heightPixels);
        var oldTexture = _videoTexture;
        _videoTexture =
            CreateWindowTexture(videoSize.x, videoSize.y, _textureFormat, out textureScaleU, out textureScaleV);
        Destroy(oldTexture);

        ServoUnityTextureUtils.Configure2DVideoSurface(_videoMeshGO, _videoTexture, textureScaleU, textureScaleV, Width,
            Height, flipX, flipY);
    }

    // Update is called once per frame
    void Update()
    {
        if (_windowIndex == 0) return;

        //Debug.Log("ServoUnityWindow.Update() with _windowIndex == " + _windowIndex);
        servo_unity_plugin?.ServoUnityRequestWindowUpdate(_windowIndex, Time.deltaTime);
    }

    // Pointer events from ServoUnityLaserPointer.

//
    private Texture2D CreateWindowTexture(int videoWidth, int videoHeight, TextureFormat format,
        out float textureScaleU, out float textureScaleV)
    {
// Check parameters.
        var vt = ServoUnityTextureUtils.CreateTexture(videoWidth, videoHeight, format);
        if (vt == null)
        {
            textureScaleU = 0;
            textureScaleV = 0;
        }
        else
        {
            textureScaleU = 1;
            textureScaleV = 1;
        }

        // Now pass the ID to the native side.
        IntPtr nativeTexPtr = vt.GetNativeTexturePtr();
        //Debug.Log("Calling ServoUnitySetWindowUnityTextureID(windowIndex:" + _windowIndex + ", nativeTexPtr:" + nativeTexPtr.ToString("X") + ")");
        servo_unity_plugin?.ServoUnitySetWindowUnityTextureID(_windowIndex, nativeTexPtr);

        return vt;
    }

    private void DestroyWindow()
    {
        bool ed = Application.isEditor;
        if (_videoTexture != null)
        {
            if (ed) DestroyImmediate(_videoTexture);
            else Destroy(_videoTexture);
            _videoTexture = null;
        }

        if (_videoMeshGO != null)
        {
            if (ed) DestroyImmediate(_videoMeshGO);
            else Destroy(_videoMeshGO);
            _videoMeshGO = null;
        }

        Resources.UnloadUnusedAssets();
    }
}