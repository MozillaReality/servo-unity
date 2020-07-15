// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla.
//
// Author(s): Philip Lamb, Patrick O'Shaughnessey

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServoUnityPointableSurface : MonoBehaviour
{
    public ServoUnityPlugin servo_unity_plugin = null; // Reference to the plugin. Will be set/cleared by ServoUnityController.
    protected Vector2Int videoSize;
    protected int _windowIndex = 0;

    public void PointerEnter()
    {
        //Debug.Log("PointerEnter()");
        servo_unity_plugin?.ServoUnityWindowPointerEvent(_windowIndex, ServoUnityPlugin.ServoUnityPointerEventID.Enter, -1, -1, - 1, -1);
    }

    public void PointerExit()
    {
        //Debug.Log("PointerExit()");
        servo_unity_plugin?.ServoUnityWindowPointerEvent(_windowIndex, ServoUnityPlugin.ServoUnityPointerEventID.Exit, -1, -1, -1, -1);
    }

    public void PointerOver(Vector2 texCoord)
    {
        int x = (int) (texCoord.x * videoSize.x);
        int y = (int) (texCoord.y * videoSize.y);
        //Debug.Log("PointerOver(" + x + ", " + y + ")");
        servo_unity_plugin?.ServoUnityWindowPointerEvent(_windowIndex, ServoUnityPlugin.ServoUnityPointerEventID.Over, -1, -1, x, y);
    }

    public void PointerPress(ServoUnityPlugin.ServoUnityPointerEventMouseButtonID button, Vector2 texCoord)
    {
        int x = (int) (texCoord.x * videoSize.x);
        int y = (int) (texCoord.y * videoSize.y);
        //Debug.Log("PointerPress(" + x + ", " + y + ")");
        servo_unity_plugin?.ServoUnityWindowPointerEvent(_windowIndex, ServoUnityPlugin.ServoUnityPointerEventID.Press, (int)button, -1, x, y);
    }

    public void PointerRelease(ServoUnityPlugin.ServoUnityPointerEventMouseButtonID button, Vector2 texCoord)
    {
        int x = (int)(texCoord.x * videoSize.x);
        int y = (int)(texCoord.y * videoSize.y);
        //Debug.Log("PointerRelease(" + x + ", " + y + ")");
        servo_unity_plugin?.ServoUnityWindowPointerEvent(_windowIndex, ServoUnityPlugin.ServoUnityPointerEventID.Release, (int)button, -1, x, y);
    }

    public void PointerClick(ServoUnityPlugin.ServoUnityPointerEventMouseButtonID button, Vector2 texCoord)
    {
        int x = (int)(texCoord.x * videoSize.x);
        int y = (int)(texCoord.y * videoSize.y);
        //Debug.Log("PointerClick(" + x + ", " + y + ")");
        servo_unity_plugin?.ServoUnityWindowPointerEvent(_windowIndex, ServoUnityPlugin.ServoUnityPointerEventID.Click, (int)button, -1, x, y);
    }

    public void PointerScrollDiscrete(Vector2 delta, Vector2 texCoord)
    {
        int scroll_x = (int)delta.x;
        int scroll_y = (int)delta.y;
        int x = (int)texCoord.x;
        int y = (int)texCoord.y;
        //Debug.Log("PointerScrollDiscrete(" + scroll_x + ", " + scroll_y + ", " + x + ", " + y + ")");
        servo_unity_plugin?.ServoUnityWindowPointerEvent(_windowIndex, ServoUnityPlugin.ServoUnityPointerEventID.ScrollDiscrete, scroll_x, scroll_y, x, y);
    }
}
