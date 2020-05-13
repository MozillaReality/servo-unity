//
// ServoUnityLaserPointer.cs
//
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020, Mozilla.
//
// Author(s): Philip Lamb, Patrick O'Shaughnessey
//

// To use, when an external device has a valid laserBeam, it should set the
// 'transform' property of the game object to which this script is attached,
// and then call SetActive(true) on the game object. If the laserBeam becomes
// invalid, it should call SetActive(false) on the game object.
// It should also set the value of SelectButtonDown.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR;

public class ServoUnityLaserPointer : ServoUnityPointer
{
    public SteamVR_Behaviour_Pose pose;
    //public SteamVR_Action_Boolean interactWithUI = SteamVR_Input.__actions_default_in_InteractUI;
    public SteamVR_Action_Boolean interactWithUI = SteamVR_Input.GetBooleanAction("InteractUI");
    public SteamVR_Action_Vector2 scroll2D = SteamVR_Input.GetVector2Action("Scroll2D");

    public Color color = new Color(0.0f, 0.8f, 1.0f, 0.8f);
    public float thickness = 0.002f;
    public Color clickColor = new Color(0.0f, 0.8f, 1.0f, 0.8f);
    public float clickThicknesss = 0.0024f;
 
    private static List<ServoUnityLaserPointer> AllLaserPointers = new List<ServoUnityLaserPointer>();
    private static ServoUnityLaserPointer ActiveLaserPointer = null;

    private GameObject holder;
    private GameObject laserBeam;

    public bool addRigidBody = false;

    protected override void Awake()
    {
        base.Awake();

        holder = new GameObject("Holder");
        holder.transform.parent = this.transform;
        holder.transform.localPosition = Vector3.zero;
        holder.transform.localRotation = Quaternion.identity;

        laserBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        laserBeam.name = "Laser beam";
        laserBeam.transform.parent = holder.transform;
        laserBeam.transform.localScale = new Vector3(thickness, thickness, 100f);
        laserBeam.transform.localPosition = new Vector3(0f, 0f, 50f);
        laserBeam.transform.localRotation = Quaternion.identity;
        BoxCollider collider = laserBeam.GetComponent<BoxCollider>();
        if (addRigidBody)
        {
            if (collider)
            {
                collider.isTrigger = true;
            }

            Rigidbody rigidBody = laserBeam.AddComponent<Rigidbody>();
            rigidBody.isKinematic = true;
        }
        else
        {
            if (collider)
            {
                Destroy(collider);
            }
        }
        Material newMaterial = new Material(Shader.Find("Unlit/Color"));
        newMaterial.SetColor("_Color", color);
        laserBeam.GetComponent<MeshRenderer>().material = newMaterial;
        laserBeam.SetActive(false);

        AllLaserPointers.Add(this);    
    }
    
    protected override void Start()
    {
        if (pose == null) {
            pose = this.GetComponent<SteamVR_Behaviour_Pose>();
            if (pose == null)
                Debug.LogError("No SteamVR_Behaviour_Pose component found on this object");
        }
        if (interactWithUI == null)
            Debug.LogError("No UI Interaction action has been set on this component.");
        if (scroll2D == null)
            Debug.LogError("No 2D scrolling action has been set on this component.");

        base.Start();
    }

    protected override void Update()
    {
        if (!pose.isValid) {
            holder.SetActive(false);
            laserBeam.SetActive(false);
        } else {
            if (ActiveLaserPointer != this) {
                holder.SetActive(true);
                laserBeam.SetActive(false);
                if (interactWithUI.GetStateUp(pose.inputSource)) {
                    ActiveLaserPointer = this;
                }
            } else {
                holder.SetActive(true);
                laserBeam.SetActive(true);

                buttonUp = interactWithUI.GetStateUp(pose.inputSource);
                buttonDown = interactWithUI.GetStateDown(pose.inputSource);
                button = interactWithUI.GetState(pose.inputSource);
                scrollDelta = scroll2D.GetAxis(pose.inputSource);
                Ray ray = new Ray(transform.position, transform.forward);

                base.Update();
        
                // Set the laser beam length, and position the centroid half-way out.
                if (button)
                {
                    laserBeam.transform.localScale = new Vector3(clickThicknesss, clickThicknesss, rayLength);
                    laserBeam.GetComponent<MeshRenderer>().material.color = clickColor;
                }
                else
                {
                    laserBeam.transform.localScale = new Vector3(thickness, thickness, rayLength);
                    laserBeam.GetComponent<MeshRenderer>().material.color = color;
                }
                laserBeam.transform.localPosition = new Vector3(0f, 0f, rayLength / 2f);
            }
        }
    }
}
