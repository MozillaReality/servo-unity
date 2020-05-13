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
        buttonDown = Input.GetMouseButtonDown(0);
        buttonUp = Input.GetMouseButtonUp(0);
        button = Input.GetMouseButton(0);
        scrollDelta = Input.mouseScrollDelta;
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        base.Update();
    }
}
