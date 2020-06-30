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
//  ServoUnityController  ServoUnityWindow         Unity Plugin                libServo 
//       +                  +                     servo_unity.dll)                |                   
//       +                  +                         +                           |   
//   [Start]                +                         +                           |
//       |                  +  servoUnityInitServo    +                           |   
//       | -----------------------------------------> |                           |
//       +                  +                   Initialize plugin,                |
//       +                  +                   recording passed-in               |
//       +                  +                   callbacks                         |      
//       +               [Start]                      +                           | 
//       +                  |                         +                           | 
//       +                  | servoUnityRequestNewWindow +                        | 
//       +                  | ----------------------> |                           | 
//       +                  +                         |     CreateServoWindow     |                 
//       +                  +                         | ------------------------> |                 
//       +                  +                         |                           |    
//       +                  +                         |                    Return with window id
//       +                  +                         |                    and texture handle  
//       +                  +                         |                           |    
//       +                  +                         | <------------------------ |                 
//       +                  +                         |                           +    
//       +                  +                   Record texture handle.            + 
//       |                  +                   Retrieve texture                  + 
//       |                  +                   format and size from              + 
//       |                  +                   texture handle.                   + 
//       |                  +                   Invoke windowCreatedCallback      + 
//       |         OnServoWindowCreated               |                           + 
//       | <----------------------------------------- |                           + 
//       |   WasCreated     +                         +                           |
//       | -------------->  |                         +                           | 
//       |                  |                         +                           | 
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

    //
    // MonoBehavior methods.
    //

    void Awake()
    {
        Debug.Log("ServoUnityController.Awake())");
    }

    [AOT.MonoPInvokeCallback(typeof(ServoUnityPluginLogCallback))]
    public static void Log(System.String msg)
    {
        if (msg.StartsWith("[error]", StringComparison.Ordinal)) Debug.LogError(msg);
        else if (msg.StartsWith("[warning]", StringComparison.Ordinal)) Debug.LogWarning(msg);
        else Debug.Log(msg); // includes [info] and [debug].
    }

    public void SendKeyEvent(int keycode)
    {
        // TODO: Introduce concept of "focused" window, once we allow more than one, so these events can be sent to the window that has focus 
        ServoUnityWindow[] servoUnityWindows = FindObjectsOfType<ServoUnityWindow>();

        if (servoUnityWindows.Length > 0)
        {
            servo_unity_plugin.ServoUnityKeyEvent(servoUnityWindows[0].WindowIndex, keycode);
        }
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

    public void UserExitFullScreenVideo()
    {
        // Notify plugin we are closing video by sending escape key
        SendKeyEvent(0x1b);
        HandleFullScreenEnd();
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

        servo_unity_plugin.ServoUnityInitServo(OnServoWindowCreated, OnServoWindowResized, OnServoBrowserEvent);
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
                    switch (eventData1)
                    {
                        case 0:
                            // Load ended.
                            Debug.Log("Servo browser event: load ended.");
                            break;
                        case 1:
                            // Load began.
                            Debug.Log("Servo browser event: load began.");
                            break;
                        default:
                            break;
                    }
                }
                break;
            case ServoUnityPlugin.ServoUnityBrowserEventType.IMEStateChanged:
                {
                    switch (eventData1)
                    {
                        case 0:
                            // Hide IME.
                            Debug.Log("Servo browser event: hide IME.");
                            break;
                        case 1:
                            // Show IME.
                            Debug.Log("Servo browser event: show IME.");
                            break;
                        default:
                            break;
                    }
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

        servo_unity_plugin.ServoUnityFinaliseServo();
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