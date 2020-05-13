//
// ServoUnityPointer.cs
//
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020, Mozilla.
//
// Author(s): Philip Lamb, Patrick O'Shaughnessey
//

// To use, when an external device has a valid pointer, it should set the
// 'transform' property of the game object to which this script is attached,
// and then call SetActive(true) on the game object. If the pointer becomes
// invalid, it should call SetActive(false) on the game object.
// It should also set the value of SelectButtonDown.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ServoUnityPointer : MonoBehaviour
{
    public Texture2D reticleTexture;
    public float reticleRadius = 0.01f;
    public float maximumPointerDistance = 100.0f;

    private GameObject reticle;

    // Subscribe to this event to know when the user pulled the trigger when not pointing at anything
    public delegate void PointerAirClick();

    public static PointerAirClick OnPointerAirClick;

    // Legacy delegate event support.
    public event PointerEventHandler PointerIn;
    public event PointerEventHandler PointerOut;
    public event PointerEventHandler PointerClick;
    
    // State that must be set by derived classes before calling Update() in this class.
    protected Ray ray;
    protected bool buttonDown = false;
    protected bool buttonUp = false;
    protected bool button = false;
    protected Vector2 scrollDelta;

    // State that will be set during an Update() in this class.
    protected float rayLength;
    protected Transform previousContact = null;
    protected Transform previousContactButtonDown = null;

    protected virtual void Awake()
    {
        // Create the reticle.
        reticle = new GameObject("Reticle");
        Shader shaderSource = Shader.Find("TextureOverlayNoLight");
        Material mat = new Material(shaderSource);
        mat.hideFlags = HideFlags.HideAndDontSave;
        mat.mainTexture = reticleTexture;
        Mesh m = new Mesh();
        m.vertices = new Vector3[]
        {
            new Vector3(-reticleRadius, -reticleRadius, 0.0f),
            new Vector3(reticleRadius, -reticleRadius, 0.0f),
            new Vector3(reticleRadius, reticleRadius, 0.0f),
            new Vector3(-reticleRadius, reticleRadius, 0.0f),
        };
        m.normals = new Vector3[]
        {
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
        };
        float u1 = 0.0f;
        float u2 = 1.0f;
        float v1 = 0.0f;
        float v2 = 1.0f;
        m.uv = new Vector2[]
        {
            new Vector2(u1, v1),
            new Vector2(u2, v1),
            new Vector2(u2, v2),
            new Vector2(u1, v2)
        };
        m.triangles = new int[]
        {
            2, 1, 0,
            3, 2, 0
        };
        MeshFilter filter = reticle.AddComponent<MeshFilter>();
        filter.mesh = m;
        MeshRenderer meshRenderer = reticle.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        reticle.GetComponent<Renderer>().material = mat;
        reticle.SetActive(false);
    }

    // A call SetActive(false) calls this, and stops Update() being called.
    protected virtual void OnDisable()
    {
        // Send a pointer exit event if required.
        if (previousContact) {
            PointerEventArgs args = new PointerEventArgs();
            args.flags = 0;
            args.target = previousContact;
            OnPointerOut(args);
            
            previousContact = null;
            reticle.SetActive(false);
        }
    }

    // A call SetActive(true) calls this, and starts Update() being called.
    // We'll let Update() do the processing.
    protected virtual void OnEnable()
    {
    }

    protected virtual void Start()
    {
    }

    protected virtual void OnPointerIn(PointerEventArgs e)
    {
        IPointerEnterHandler[] pointerEnterHandlers = e.target.GetComponents<IPointerEnterHandler>();
        foreach (var pointerEnterHandler in pointerEnterHandlers)
        {
            pointerEnterHandler?.OnPointerEnter(new PointerEventData(EventSystem.current));
        }

        // Legacy delegate event support.
        if (PointerIn != null)
            PointerIn(this, e);
    }

    protected virtual void OnPointerClick(PointerEventArgs e)
    {
        IPointerClickHandler[] clickHandlers = e.target.GetComponents<IPointerClickHandler>();
        foreach (var clickHandler in clickHandlers)
        {
            clickHandler?.OnPointerClick(new PointerEventData(EventSystem.current));
        }

        // Legacy delegate event support.
        if (PointerClick != null)
            PointerClick(this, e);
    }

    protected virtual void OnPointerDown(PointerEventArgs e)
    {
        IPointerDownHandler[] downHandlers = e.target.GetComponents<IPointerDownHandler>();
        foreach (var downHandler in downHandlers)
        {
            downHandler?.OnPointerDown(new PointerEventData(EventSystem.current));
        }
    }

    protected virtual void OnPointerUp(PointerEventArgs e)
    {
        IPointerUpHandler[] upHandlers = e.target.GetComponents<IPointerUpHandler>();
        foreach (var upHandler in upHandlers)
        {
            upHandler?.OnPointerUp(new PointerEventData(EventSystem.current));
        }
    }

    protected virtual void OnPointerOut(PointerEventArgs e)
    {
        // Special handling for ServoUnityPointableSurface.
        ServoUnityPointableSurface s = e.target.gameObject.GetComponentInParent(typeof(ServoUnityPointableSurface)) as ServoUnityWindow;
        s?.PointerExit();
    
        IPointerExitHandler[] pointerExitHandlers = e.target.GetComponents<IPointerExitHandler>();
        foreach (var pointerExitHandler in pointerExitHandlers)
        {
            pointerExitHandler?.OnPointerExit(new PointerEventData(EventSystem.current));
        }

        // Legacy delegate event support.
        if (PointerOut != null)
            PointerOut(this, e);
    }


    protected virtual void Update()
    {
        // ray must be set first in derived class.
        rayLength = maximumPointerDistance;
        RaycastHit hit;
        bool bHit = Physics.Raycast(ray, out hit);
        if (bHit && hit.distance > maximumPointerDistance) bHit = false;

        if (previousContact && (!bHit || previousContact != hit.transform))
        {
            ServoUnityPointableSurface sups = previousContact.gameObject.GetComponentInParent(typeof(ServoUnityPointableSurface)) as ServoUnityPointableSurface;
            sups?.PointerExit();

            PointerEventArgs args = new PointerEventArgs();
            args.flags = 0;
            args.target = previousContact;
            OnPointerOut(args);
        }

        // Target not found for hit testing, so clear state
        if (!bHit)
        {
            reticle.SetActive(false);
            previousContact = null;

            if (buttonUp) OnPointerAirClick?.Invoke();
        }
        else
        {
            rayLength = hit.distance;
            reticle.SetActive(true);
            reticle.transform.position = hit.point;
            reticle.transform.rotation = Quaternion.LookRotation(-hit.normal);

            ServoUnityPointableSurface sups = hit.transform.gameObject.GetComponentInParent(typeof(ServoUnityPointableSurface)) as ServoUnityPointableSurface;

            if (previousContact != hit.transform)
            {
                sups?.PointerEnter();

                PointerEventArgs argsIn = new PointerEventArgs();
                argsIn.flags = 0;
                argsIn.target = hit.transform;
                OnPointerIn(argsIn);
                
                previousContact = hit.transform;
            }

            if (buttonDown)
            {
                sups?.PointerPress(hit.textureCoord);

                previousContactButtonDown = previousContact;
                PointerEventArgs argsClick = new PointerEventArgs();
                argsClick.flags = 0;
                argsClick.target = previousContact;
                OnPointerDown(argsClick);
            }

            if (buttonUp)
            {
                sups?.PointerRelease(hit.textureCoord);

                PointerEventArgs argsClick = new PointerEventArgs();
                argsClick.flags = 0;
                argsClick.target = previousContact;
                if (previousContactButtonDown == previousContact) OnPointerClick(argsClick);
                OnPointerUp(argsClick);
            }

            sups?.PointerOver(hit.textureCoord);
            if (scrollDelta != Vector2.zero)
            {
                 sups?.PointerScrollDiscrete(scrollDelta);
            }
        } // bHit
    }
}

public struct PointerEventArgs
{
    public uint flags;
    public Transform target;
}

public delegate void PointerEventHandler(object sender, PointerEventArgs e);