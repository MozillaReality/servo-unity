//
// ServoUnityMousePointer.cs
//
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020, Mozilla.
//
// Author(s): Philip Lamb, Patrick O'Shaughnessey
//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ServoUnityMousePointer : ServoUnityPointer
{
    protected override void Update()
    {
        for (int i = 0; i < 3; i++)
        {
            buttonDown[i] = Input.GetMouseButtonDown(i);
            buttonUp[i] = Input.GetMouseButtonUp(i);
            button[i] = Input.GetMouseButton(i);
        }
        scrollDelta = Input.mouseScrollDelta;
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        base.Update();
    }
}
