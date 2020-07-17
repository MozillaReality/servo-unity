// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla.
//
// Author(s): Philip Lamb

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ServoUnityNavbarController : MonoBehaviour
{
    [HideInInspector] public ServoUnityController suc = null;
    public Button backButton;
    public Button forwardButton;
    public Button stopButton;
    public Button reloadButton;
    public Button homeButton;

    void Awake()
    {
        suc = GetComponent<ServoUnityController>();
    }

    void Start()
    {
        stopButton.gameObject.SetActive(false);
        reloadButton.gameObject.SetActive(true);
        backButton.interactable = false;
        forwardButton.interactable = false;
    }

    public void OnLoadStateChanged(bool started)
    {
        stopButton.gameObject.SetActive(started);
        reloadButton.gameObject.SetActive(!started);
    }

    public void OnHistoryChanged(bool canGoBack, bool canGoForward)
    {
        backButton.interactable = canGoBack;
        forwardButton.interactable = canGoForward;
    }
}
