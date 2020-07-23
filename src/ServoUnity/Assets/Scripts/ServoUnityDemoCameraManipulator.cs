// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla.
//
// Author(s): Philip Lamb
//
// ServoUnityDemoCameraManipulator is a non-essential convenience class that implements
// keyboard control of the position and orientation of the a camera, for demo purposes.
//

using System;
using System.Collections;
using UnityEngine;

public class ServoUnityDemoCameraManipulator : MonoBehaviour
{
    private ServoUnityController suc = null;
    private Camera c;

    const float millisecsToFullmovementFactor = 300.0f; // How long (in milliseconds) to reach full movement speed.
    const float moveAroundMetresPerSecond = 1.5f; // Full movement speed in Unity units (metres assumed) per second.
    const float lookAroundDegreesPerSecond = 90.0f; // Full look around speed in degrees per second.
    private Vector3 moveAroundFactor = new Vector3(0.0f, 0.0f, 0.0f); // Normalised move around input.
    private Vector2 lookAroundFactor = new Vector2(0.0f, 0.0f); // Normalised look around input.

    void Awake()
    {
        suc = FindObjectOfType<ServoUnityController>();
        c = gameObject.GetComponent<Camera>();
        if (c == null) c = Camera.main;
    }

    void Update()
    {
        //
        // Process keyboard-driven motion of the camera.
        //

        bool keyboardOKForMovement = !suc.KeyboardInUse;
        float accelFactor = Time.deltaTime*1000.0f / millisecsToFullmovementFactor;

        Vector3 excursion = new Vector3(Mathf.Abs(moveAroundFactor.x), Mathf.Abs(moveAroundFactor.y), Mathf.Abs(moveAroundFactor.z));
        Vector3 excursionDirection = new Vector3(Mathf.Sign(moveAroundFactor.x), Mathf.Sign(moveAroundFactor.y), Mathf.Sign(moveAroundFactor.z));

        if (keyboardOKForMovement && Input.GetKey(KeyCode.D))
        {
            moveAroundFactor.x += accelFactor;
            if (moveAroundFactor.x > 1.0f) moveAroundFactor.x = 1.0f;
        }
        else if (keyboardOKForMovement && Input.GetKey(KeyCode.A))
        {
            moveAroundFactor.x -= accelFactor;
            if (moveAroundFactor.x < 1.0f) moveAroundFactor.x = -1.0f;
        }
        else if (excursion.x > 0.0f)
        {
            moveAroundFactor.x -= Mathf.Min(accelFactor, excursion.x) * excursionDirection.x;
        }

        if (keyboardOKForMovement && Input.GetKey(KeyCode.Space))
        {
            moveAroundFactor.y += accelFactor;
            if (moveAroundFactor.y > 1.0f) moveAroundFactor.y = 1.0f;
        }
        else if (keyboardOKForMovement && Input.GetKey(KeyCode.C))
        {
            moveAroundFactor.y -= accelFactor;
            if (moveAroundFactor.y < 1.0f) moveAroundFactor.y = -1.0f;
        }
        else if (excursion.y > 0.0f)
        {
            moveAroundFactor.y -= Mathf.Min(accelFactor, excursion.y) * excursionDirection.y;
        }

        if (keyboardOKForMovement && Input.GetKey(KeyCode.W))
        {
            moveAroundFactor.z += accelFactor;
            if (moveAroundFactor.z > 1.0f) moveAroundFactor.z = 1.0f;
        }
        else if (keyboardOKForMovement && Input.GetKey(KeyCode.S))
        {
            moveAroundFactor.z -= accelFactor;
            if (moveAroundFactor.z < 1.0f) moveAroundFactor.z = -1.0f;
        }
        else if (excursion.z > 0.0f)
        {
            moveAroundFactor.z -= Mathf.Min(accelFactor, excursion.z) * excursionDirection.z;
        }

        Vector3 lookExcursionDirection = new Vector2(Mathf.Sign(lookAroundFactor.x), Mathf.Sign(lookAroundFactor.y));
        Vector3 lookExcursion = new Vector3(Mathf.Abs(lookAroundFactor.x), Mathf.Abs(lookAroundFactor.y));
            
        if (keyboardOKForMovement && Input.GetKey(KeyCode.DownArrow))
        {
            lookAroundFactor.x += accelFactor;
            if (lookAroundFactor.x > 1.0f) lookAroundFactor.x = 1.0f;
        }
        else if (keyboardOKForMovement && Input.GetKey(KeyCode.UpArrow))
        {
            lookAroundFactor.x -= accelFactor;
            if (lookAroundFactor.x < 1.0f) lookAroundFactor.x = -1.0f;
        }
        else if (lookExcursion.x > 0.0f)
        {
            lookAroundFactor.x -= Mathf.Min(accelFactor, lookExcursion.x) * lookExcursionDirection.x;
        }

        c.transform.Translate(moveAroundFactor * moveAroundMetresPerSecond * Time.deltaTime);

        if (keyboardOKForMovement && Input.GetKey(KeyCode.RightArrow))
        {
            lookAroundFactor.y += accelFactor;
            if (lookAroundFactor.y > 1.0f) lookAroundFactor.y = 1.0f;
        }
        else if (keyboardOKForMovement && Input.GetKey(KeyCode.LeftArrow))
        {
            lookAroundFactor.y -= accelFactor;
            if (lookAroundFactor.y < 1.0f) lookAroundFactor.y = -1.0f;
        }
        else if (lookExcursion.y > 0.0f)
        {
            lookAroundFactor.y -= Mathf.Min(accelFactor, lookExcursion.y) * lookExcursionDirection.y;
        }

        Vector3 orientation = c.transform.eulerAngles;
        orientation.z = 0.0f; // Keep the camera level.
        orientation.x += lookAroundFactor.x * lookAroundDegreesPerSecond * Time.deltaTime; // Elevation.
        orientation.y += lookAroundFactor.y * lookAroundDegreesPerSecond * Time.deltaTime; // Azimuth.
        c.transform.eulerAngles = orientation;
    }

}