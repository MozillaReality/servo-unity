// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla.
//
// Author(s): Philip Lamb, Thomas Moore, Patrick O'Shaughnessey
//
// ServoUnityController is the core controller in the operation of the servo
// browser. Details of the classes and processes involved in bootstrap are
// described below:
//
//                              
//  ServoUnityController  ServoUnityWindow           Unity Plugin             libsimpleservo2 
//       +                  +                        servo_unity.dll)                |                   
//       +                  +                            +                           |   
//   [Start]                +                            +                           |
//       |                  +     servoUnityInit         +                           |   
//       | --------------------------------------------> |                           |
//       +                  +                      Initialize plugin,                |
//       +                  +                      recording passed-in               |
//       +                  +                      callbacks                         |      
//       +               [Start]                         +                           | 
//       +                  |                            +                           | 
//       +                  | servoUnityRequestNewWindow +                           | 
//       +                  | -------------------------> |                           | 
//       +                  +                            |     CreateServoWindow     |                 
//       +                  +                            | ------------------------> |                 
//       +                  +                            |                           |    
//       +                  +                            |                    Return with window id
//       +                  +                            |                    and texture handle  
//       +                  +                            |                           |    
//       +                  +                            | <------------------------ |                 
//       +                  +                            |                           +    
//       +                  +                      Record texture handle.            + 
//       |                  +                      Retrieve texture                  + 
//       |                  +                      format and size from              + 
//       |                  +                      texture handle.                   + 
//       |                  +                      Invoke windowCreatedCallback      + 
//       |         OnServoWindowCreated                  |                           + 
//       | <-------------------------------------------- |                           + 
//       |   WasCreated     +                            +                           |
//       | ---------------> |                            +                           | 
//       |                  |                            +                           | 
//      ...                ...                       ...                         ...  


using System;
using System.Collections.Generic;
using UnityEngine;

public class ServoUnityController : MonoBehaviour
{
    public enum SERVO_UNITY_LOG_LEVEL
    {
        SERVO_UNITY_LOG_LEVEL_DEBUG = 0,
        SERVO_UNITY_LOG_LEVEL_INFO,
        SERVO_UNITY_LOG_LEVEL_WARN,
        SERVO_UNITY_LOG_LEVEL_ERROR,
        SERVO_UNITY_LOG_LEVEL_REL_INFO
    }

    [SerializeField] private SERVO_UNITY_LOG_LEVEL currentLogLevel = SERVO_UNITY_LOG_LEVEL.SERVO_UNITY_LOG_LEVEL_INFO;

    public ServoUnityPlugin Plugin => servo_unity_plugin;

    // Main reference to the plugin functions. Created in OnEnable(), destroyed in OnDisable().
    private ServoUnityPlugin servo_unity_plugin = null;

    public bool DontCloseNativeWindowOnClose = false;

    private ServoUnityNavbarController navbarController = null;

    public string Homepage = "https://mozilla.org/";

    private bool IMEActive = false;
    private int IMEWindowIndex = 0;

    [NonSerialized] public ServoUnityWindow NavbarWindow = null;

    //
    // MonoBehavior methods.
    //

    void Awake()
    {
        Debug.Log("ServoUnityController.Awake())");
        navbarController = FindObjectOfType<ServoUnityNavbarController>();
    }

    [AOT.MonoPInvokeCallback(typeof(ServoUnityPluginLogCallback))]
    public static void Log(System.String msg)
    {
        if (msg.EndsWith(Environment.NewLine)) msg = msg.Substring(0, msg.Length - Environment.NewLine.Length); // Trim any final newline.
        if (msg.StartsWith("[error]", StringComparison.Ordinal)) Debug.LogError(msg);
        else if (msg.StartsWith("[warning]", StringComparison.Ordinal)) Debug.LogWarning(msg);
        else Debug.Log(msg); // includes [info] and [debug].
    }

    void OnEnable()
    {
        Debug.Log("ServoUnityController.OnEnable()");

        servo_unity_plugin = new ServoUnityPlugin();

        Application.runInBackground = true;

        // Register the log callback.
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor: // Unity Editor on OS X.
            case RuntimePlatform.OSXPlayer: // Unity Player on OS X.
            case RuntimePlatform.WindowsEditor: // Unity Editor on Windows.
            case RuntimePlatform.WindowsPlayer: // Unity Player on Windows.
            case RuntimePlatform.LinuxEditor:
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.WSAPlayerX86: // Unity Player on Windows Store X86.
            case RuntimePlatform.WSAPlayerX64: // Unity Player on Windows Store X64.
            case RuntimePlatform.WSAPlayerARM: // Unity Player on Windows Store ARM.
            case RuntimePlatform.Android: // Unity Player on Android.
            case RuntimePlatform.IPhonePlayer: // Unity Player on iOS.
                servo_unity_plugin.ServoUnityRegisterLogCallback(Log);
                servo_unity_plugin.ServoUnitySetLogLevel((int)currentLogLevel);
                break;
            default:
                break;
        }

        // Give the plugin a place to look for resources.

        string resourcesPath = Application.streamingAssetsPath;
        servo_unity_plugin.ServoUnitySetResourcesPath(resourcesPath);

        // Set any launch-time parameters.
        if (DontCloseNativeWindowOnClose)
            servo_unity_plugin.ServoUnitySetParamBool(ServoUnityPlugin.ServoUnityParam.b_CloseNativeWindowOnClose, false);

        // Set the reference to the plugin in any other objects in the scene that need it.
        ServoUnityWindow[] servoUnityWindows = FindObjectsOfType<ServoUnityWindow>();
        foreach (ServoUnityWindow w in servoUnityWindows)
        {
            w.servo_unity_plugin = servo_unity_plugin;
        }
    }

    //
    // Handlers for callbacks from plugin.
    //
    
    private void HandleFullScreenBegin(int pixelwidth, int pixelheight, int format, int projection)
    {
        Debug.Log("ServoUnityController.HandleFullScreenBegin(" + pixelwidth + ", " + pixelheight + ", " + format + ", " + projection + ")");
    }

    private void HandleFullScreenEnd()
    {
        Debug.Log("ServoUnityController.HandleFullScreenEnd()");
    }

    void OnDisable()
    {
        Debug.Log("ServoUnityController.OnDisable()");

        // Clear the references to the plugin in any other objects in the scene that have it.
        ServoUnityWindow[] servoUnityWindows = FindObjectsOfType<ServoUnityWindow>();
        foreach (ServoUnityWindow w in servoUnityWindows)
        {
            w.servo_unity_plugin = null;
        }

        servo_unity_plugin.ServoUnitySetResourcesPath(null);

        // Since we might be going away, tell users of our Log function
        // to stop calling it.
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.LinuxEditor:
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.WSAPlayerX86:
            case RuntimePlatform.WSAPlayerX64:
            case RuntimePlatform.WSAPlayerARM:
            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
                servo_unity_plugin.ServoUnityRegisterLogCallback(null);
                break;
            default:
                break;
        }

        servo_unity_plugin = null;
    }

    void Start()
    {
        Debug.Log("ServoUnityController.Start()");
        Debug.Log("Plugin version " + servo_unity_plugin.ServoUnityGetVersion());

        servo_unity_plugin.ServoUnityInit(OnServoWindowCreated, OnServoWindowResized, OnServoBrowserEvent);
    }

    void OnGUI()
    {
        if (IMEActive)
        {
            Event e = Event.current;
            if (e.isKey)
            {
                ServoUnityPlugin.ServoUnityKeyCode keyCode;
                int character = 0;
                switch (e.keyCode) {
                    case KeyCode.Backspace: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Backspace; break;
                    case KeyCode.Delete: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Delete; break;
                    case KeyCode.Tab: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Tab; break;
                    case KeyCode.Clear: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Clear; break;
                    case KeyCode.Return: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Return; break;
                    case KeyCode.Pause: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Pause; break;
                    case KeyCode.Escape: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Escape; break;
                    case KeyCode.Space: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Space; break;
                    case KeyCode.UpArrow: keyCode = ServoUnityPlugin.ServoUnityKeyCode.UpArrow; break;
                    case KeyCode.DownArrow: keyCode = ServoUnityPlugin.ServoUnityKeyCode.DownArrow; break;
                    case KeyCode.RightArrow: keyCode = ServoUnityPlugin.ServoUnityKeyCode.RightArrow; break;
                    case KeyCode.LeftArrow: keyCode = ServoUnityPlugin.ServoUnityKeyCode.LeftArrow; break;
                    case KeyCode.Insert: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Insert; break;
                    case KeyCode.Home: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Home; break;
                    case KeyCode.End: keyCode = ServoUnityPlugin.ServoUnityKeyCode.End; break;
                    case KeyCode.PageUp: keyCode = ServoUnityPlugin.ServoUnityKeyCode.PageUp; break;
                    case KeyCode.PageDown: keyCode = ServoUnityPlugin.ServoUnityKeyCode.PageDown; break;
                    case KeyCode.F1: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F1; break;
                    case KeyCode.F2: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F2; break;
                    case KeyCode.F3: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F3; break;
                    case KeyCode.F4: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F4; break;
                    case KeyCode.F5: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F5; break;
                    case KeyCode.F6: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F6; break;
                    case KeyCode.F7: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F7; break;
                    case KeyCode.F8: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F8; break;
                    case KeyCode.F9: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F9; break;
                    case KeyCode.F10: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F10; break;
                    case KeyCode.F11: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F11; break;
                    case KeyCode.F12: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F12; break;
                    case KeyCode.F13: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F13; break;
                    case KeyCode.F14: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F14; break;
                    case KeyCode.F15: keyCode = ServoUnityPlugin.ServoUnityKeyCode.F15; break;
                    case KeyCode.Numlock: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Numlock; break;
                    case KeyCode.CapsLock: keyCode = ServoUnityPlugin.ServoUnityKeyCode.CapsLock; break;
                    case KeyCode.ScrollLock: keyCode = ServoUnityPlugin.ServoUnityKeyCode.ScrollLock; break;
                    case KeyCode.RightShift: keyCode = ServoUnityPlugin.ServoUnityKeyCode.RightShift; break;
                    case KeyCode.LeftShift: keyCode = ServoUnityPlugin.ServoUnityKeyCode.LeftShift; break;
                    case KeyCode.RightControl: keyCode = ServoUnityPlugin.ServoUnityKeyCode.RightControl; break;
                    case KeyCode.LeftControl: keyCode = ServoUnityPlugin.ServoUnityKeyCode.LeftControl; break;
                    case KeyCode.RightAlt: keyCode = ServoUnityPlugin.ServoUnityKeyCode.RightAlt; break;
                    case KeyCode.LeftAlt: keyCode = ServoUnityPlugin.ServoUnityKeyCode.LeftAlt; break;
                    case KeyCode.LeftCommand: keyCode = ServoUnityPlugin.ServoUnityKeyCode.LeftCommand; break;
                    case KeyCode.LeftWindows: keyCode = ServoUnityPlugin.ServoUnityKeyCode.LeftWindows; break;
                    case KeyCode.RightCommand: keyCode = ServoUnityPlugin.ServoUnityKeyCode.RightCommand; break;
                    case KeyCode.RightWindows: keyCode = ServoUnityPlugin.ServoUnityKeyCode.RightWindows; break;
                    case KeyCode.AltGr: keyCode = ServoUnityPlugin.ServoUnityKeyCode.AltGr; break;
                    case KeyCode.Help: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Help; break;
                    case KeyCode.Print: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Print; break;
                    case KeyCode.SysReq: keyCode = ServoUnityPlugin.ServoUnityKeyCode.SysReq; break;
                    case KeyCode.Break: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Break; break;
                    case KeyCode.Menu: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Menu; break;
                    case KeyCode.Keypad0: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Keypad0; break;
                    case KeyCode.Keypad1: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Keypad1; break;
                    case KeyCode.Keypad2: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Keypad2; break;
                    case KeyCode.Keypad3: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Keypad3; break;
                    case KeyCode.Keypad4: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Keypad4; break;
                    case KeyCode.Keypad5: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Keypad5; break;
                    case KeyCode.Keypad6: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Keypad6; break;
                    case KeyCode.Keypad7: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Keypad7; break;
                    case KeyCode.Keypad8: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Keypad8; break;
                    case KeyCode.Keypad9: keyCode = ServoUnityPlugin.ServoUnityKeyCode.Keypad9; break;
                    case KeyCode.KeypadPeriod: keyCode = ServoUnityPlugin.ServoUnityKeyCode.KeypadPeriod; break;
                    case KeyCode.KeypadDivide: keyCode = ServoUnityPlugin.ServoUnityKeyCode.KeypadDivide; break;
                    case KeyCode.KeypadMultiply: keyCode = ServoUnityPlugin.ServoUnityKeyCode.KeypadMultiply; break;
                    case KeyCode.KeypadMinus: keyCode = ServoUnityPlugin.ServoUnityKeyCode.KeypadMinus; break;
                    case KeyCode.KeypadPlus: keyCode = ServoUnityPlugin.ServoUnityKeyCode.KeypadPlus; break;
                    case KeyCode.KeypadEnter: keyCode = ServoUnityPlugin.ServoUnityKeyCode.KeypadEnter; break;
                    case KeyCode.KeypadEquals: keyCode = ServoUnityPlugin.ServoUnityKeyCode.KeypadEquals; break;
                    default:
                        if (e.character != 0)
                        {
                            keyCode = ServoUnityPlugin.ServoUnityKeyCode.Character;
                            character = e.character;
                        }
                        else
                        {
                            return;
                        }
                        break;
                }
                if (e.type == EventType.KeyDown)
                {
                    servo_unity_plugin.ServoUnityKeyEvent(IMEWindowIndex, true, keyCode, character);
                }
                else if (e.type == EventType.KeyUp)
                {
                    servo_unity_plugin.ServoUnityKeyEvent(IMEWindowIndex, false, keyCode, character);
                }
            } // e.isKey
        } // IMEActive
    }

    void Update()
    {
        servo_unity_plugin.ServoUnityFlushLog();
    }

    [AOT.MonoPInvokeCallback(typeof(ServoUnityPluginWindowResizedCallback))]
    void OnServoWindowResized(int uid, int widthPixels, int heightPixels)
    {
        ServoUnityWindow window = ServoUnityWindow.FindWindowWithUID(uid);
        if (window == null)
        {
            Debug.LogError("ServoUnityController.OnFxWindowResized: Received update request for a window that doesn't exist.");
            return;
        }

        window.WasResized(widthPixels, heightPixels);
    }

    [AOT.MonoPInvokeCallback(typeof(ServoUnityPluginBrowserEventCallback))]
    void OnServoBrowserEvent(int uid, int eventType, int eventData1, int eventData2)
    {
        ServoUnityWindow window = ServoUnityWindow.FindWindowWithUID(uid);
        if (window == null)
        {
            Debug.LogError("ServoUnityController.OnServoBrowserEvent: Received event for a window that doesn't exist.");
            return;
        }

        switch ((ServoUnityPlugin.ServoUnityBrowserEventType)eventType)
        {
            case ServoUnityPlugin.ServoUnityBrowserEventType.NOP:
                break;
            case ServoUnityPlugin.ServoUnityBrowserEventType.Shutdown:
                // Browser is shutting down. So should we.
                OnApplicationQuit();
                break;
            case ServoUnityPlugin.ServoUnityBrowserEventType.LoadStateChanged:
                {
                    Debug.Log($"Servo browser event: load {(eventData1 == 1 ? "began" : "ended")}.");
                    if (navbarController) navbarController.OnLoadStateChanged(eventData1 == 1);
                }
                break;
            case ServoUnityPlugin.ServoUnityBrowserEventType.IMEStateChanged:
                {
                    Debug.Log($"Servo browser event: {(eventData1 == 1 ? "show" : "hide")} IME.");
                    IMEActive = (eventData1 == 1);
                    IMEWindowIndex = window.WindowIndex;
                }
                break;
            case ServoUnityPlugin.ServoUnityBrowserEventType.FullscreenStateChanged:
                {
                    switch (eventData1)
                    {
                        case 0:
                            // Will enter fullscreen. Should e.g. hide windows and other UI.
                            Debug.Log("Servo browser event: will enter fullscreen.");
                            break;
                        case 1:
                            // Did enter fullscreen. Should e.g. show an "exit fullscreen" control.
                            Debug.Log("Servo browser event: did enter fullscreen.");
                            break;
                        case 2:
                            // Will exit fullscreen. Should e.g. hide "exit fullscreen" control.
                            Debug.Log("Servo browser event: will exit fullscreen.");
                            break;
                        case 3:
                            // Did exit fullscreen. Should e.g. show windows and other UI.
                            Debug.Log("Servo browser event: did exit fullscreen.");
                            break;
                        default:
                            break;
                    }
                }
                break;
            case ServoUnityPlugin.ServoUnityBrowserEventType.HistoryChanged:
                {
                    Debug.Log($"Servo browser event: history changed, {(eventData1 == 1 ? "can" : "can't")} go back, {(eventData2 == 1 ? "can" : "can't")} go forward.");
                    if (navbarController) navbarController.OnHistoryChanged(eventData1 == 1, eventData2 == 1);
                }
                break;
            case ServoUnityPlugin.ServoUnityBrowserEventType.TitleChanged:
                {
                    Debug.Log("Servo browser event: title changed.");
                    if (navbarController) navbarController.OnTitleChanged(servo_unity_plugin.ServoUnityGetWindowTitle(window.WindowIndex));
                }
                break;
            case ServoUnityPlugin.ServoUnityBrowserEventType.URLChanged:
                {
                    Debug.Log("Servo browser event: URL changed.");
                    if (navbarController) navbarController.OnURLChanged(servo_unity_plugin.ServoUnityGetWindowURL(window.WindowIndex));
                }
                break;
            default:
                Debug.Log("Servo browser event: unknown event.");
                break;

        }
    }


    private void OnApplicationQuit()
    {
        Debug.Log("ServoUnityController.OnApplicationQuit()");
        ServoUnityWindow[] servoUnityWindows = FindObjectsOfType<ServoUnityWindow>();
        foreach (ServoUnityWindow w in servoUnityWindows)
        {
            w.Close();
        }

        servo_unity_plugin.ServoUnityFinalise();
    }

    public SERVO_UNITY_LOG_LEVEL LogLevel
    {
        get { return currentLogLevel; }

        set
        {
            currentLogLevel = value;
            servo_unity_plugin.ServoUnitySetLogLevel((int) currentLogLevel);
        }
    }

    void OnServoWindowCreated(int uid, int windowIndex, int widthPixels, int heightPixels, int formatNative)
    {
        ServoUnityWindow window = ServoUnityWindow.FindWindowWithUID(uid);
        if (window == null)
        {
            window = ServoUnityWindow.CreateNewInParent(transform.parent.gameObject);
        }
        NavbarWindow = window; // Switch to newly created window.

        /*
         Enum values from Source/ServoUnityPlugin/servo_unity_c.h
         enum  {
	            ServoUnityTextureFormat_Invalid = 0,
	            ServoUnityTextureFormat_RGBA32 = 1,
	            ServoUnityTextureFormat_BGRA32 = 2,
	            ServoUnityTextureFormat_ARGB32 = 3,
	            ServoUnityTextureFormat_ABGR32 = 4,
	            ServoUnityTextureFormat_RGB24 = 5,
	            ServoUnityTextureFormat_BGR24 = 6,
	            ServoUnityTextureFormat_RGBA4444 = 7,
	            ServoUnityTextureFormat_RGBA5551 = 8,
	            ServoUnityTextureFormat_RGB565 = 9
            };
        */
        TextureFormat format;
        switch (formatNative)
        {
            case 1:
                format = TextureFormat.RGBA32;
                break;
            case 2:
                format = TextureFormat.BGRA32;
                break;
            case 3:
                format = TextureFormat.ARGB32;
                break;
            case 5:
                format = TextureFormat.RGB24;
                break;
            case 7:
                format = TextureFormat.RGBA4444;
                break;
            case 9:
                format = TextureFormat.RGB565;
                break;
            default:
                format = (TextureFormat) 0;
                break;
        }

        window.WasCreated(windowIndex, widthPixels, heightPixels, format);
    }
}