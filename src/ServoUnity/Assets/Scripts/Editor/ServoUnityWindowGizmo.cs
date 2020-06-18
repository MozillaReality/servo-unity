// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla.
//
// Author(s): Philip Lamb

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

class ServoUnityWindowGizmo
{
    private static Color BorderSelected = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    private static Color BorderUnselected = new Color(0.75f, 0.75f, 0.75f, 0.5f);


    // Draw the gizmo always, and the gizmo can be picked in the editor.
    [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
    // First argument of method is the type for which the Gizmo will be drawn.
    static void RenderServoUnityWindowGizmo(ServoUnityWindow suw, GizmoType gizmoType)
    {
        Matrix4x4 pose = suw.gameObject.transform.localToWorldMatrix;
        bool selected = (gizmoType & GizmoType.Active) != 0;
        float width = suw.Width;
        float height = width * suw.DefaultHeightToRequest/suw.DefaultWidthToRequest;
        Vector3 origin = pose.GetColumn(3) + new Vector4(0.0f, height/2.0f, 0.0f, 0.0f);
        Vector3 right = pose.GetColumn(0);
        Vector3 up = pose.GetColumn(1);

        DrawRectangle(origin, up, right, width, height, selected ? BorderSelected : BorderUnselected); // Edge.
    }
    
    private static void DrawRectangle(Vector3 centre, Vector3 up, Vector3 right, float width, float height, Color color) 
    {

        Gizmos.color = color;

        //ARController.Log("DrawRectangle centre=" + centre.ToString("F3") + ", up=" + up.ToString("F3") + ", right=" + right.ToString("F3") + ", width=" + width.ToString("F3") + ", height=" + height.ToString("F3") + ".");
        Vector3 u = up * height;
        Vector3 r = right * width;
        Vector3 p = centre - (u * 0.5f) - (r * 0.5f);
        
        Gizmos.DrawLine(p, p + u);
        Gizmos.DrawLine(p + u, p + u + r);
        Gizmos.DrawLine(p + u + r, p + r);
        Gizmos.DrawLine(p + r, p);
    }
}




