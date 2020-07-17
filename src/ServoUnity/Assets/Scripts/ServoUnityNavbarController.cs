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
    public Button BackButton;
    public Button ForwardButton;
    public Button StopButton;
    public Button ReloadButton;
    public Button HomeButton;
    public Text TitleText;
    public Text URLTextField;

    void Awake()
    {
        suc = GetComponent<ServoUnityController>();
    }

    void Start()
    {
        StopButton.gameObject.SetActive(false);
        ReloadButton.gameObject.SetActive(true);
        BackButton.interactable = false;
        ForwardButton.interactable = false;
    }

    public void OnLoadStateChanged(bool started)
    {
        StopButton.gameObject.SetActive(started);
        ReloadButton.gameObject.SetActive(!started);
    }

    public void OnHistoryChanged(bool canGoBack, bool canGoForward)
    {
        BackButton.interactable = canGoBack;
        ForwardButton.interactable = canGoForward;
    }

    public void OnTitleChanged(string title)
    {
        TitleText.text = title;
    }

    public void OnURLChanged(string URL)
    {
        URLTextField.text = URL;
    }

}
