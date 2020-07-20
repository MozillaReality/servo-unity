//
// ServoUnityPlugin.cs
// 
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019, Mozilla.
//
// Author(s): Philip Lamb
//
// Main declarations of plugin interfaces, including any managed-to-native wrappers.
//

using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// Delegate type declarations.
public delegate void ServoUnityPluginLogCallback([MarshalAs(UnmanagedType.LPStr)] string msg);
public delegate void ServoUnityPluginWindowCreatedCallback(int uid, int windowIndex, int pixelWidth, int pixelHeight, int format);
public delegate void ServoUnityPluginWindowResizedCallback(int uid, int pixelWidth, int pixelHeight);
public delegate void ServoUnityPluginBrowserEventCallback(int uid, int eventType, int eventData1, int eventData2);
public delegate void ServoUnityPluginFullScreenBeginCallback(int pixelWidth, int pixelHeight, int format, int projection);
public delegate void ServoUnityPluginFullEndCallback();

public class ServoUnityPlugin
{
    //
    // Delegate instances.
    //
    
    private ServoUnityPluginLogCallback logCallback = null;
    private GCHandle logCallbackGCH;
    
    private ServoUnityPluginWindowCreatedCallback windowCreatedCallback = null;
    private GCHandle windowCreatedCallbackGCH;

    private ServoUnityPluginWindowResizedCallback windowResizedCallback = null;
    private GCHandle windowResizedCallbackGCH;

    private ServoUnityPluginBrowserEventCallback browserEventCallback = null;
    private GCHandle browserEventCallbackGCH;

    public void ServoUnityRegisterLogCallback(ServoUnityPluginLogCallback lcb)
    {
        logCallback = lcb; // Set or unset.
        if (lcb != null)
        {
            // If setting, create the callback stub prior to registering the callback on the native side.
            logCallbackGCH =  GCHandle.Alloc(logCallback); // Does not need to be pinned, see http://stackoverflow.com/a/19866119/316487 
        }

        ServoUnityPlugin_pinvoke.servoUnityRegisterLogCallback(logCallback);
        if (lcb == null)
        {
            // If unsetting, free the callback stub after deregistering the callback on the native side.
            logCallbackGCH.Free();
        }
    }

    public void ServoUnitySetLogLevel(int logLevel)
    {
        ServoUnityPlugin_pinvoke.servoUnitySetLogLevel(logLevel);
    }

    public void ServoUnityFlushLog()
    {
        ServoUnityPlugin_pinvoke.servoUnityFlushLog();
    }

    public string ServoUnityGetVersion()
    {
        var sb = new StringBuilder(128);
        bool ok = ServoUnityPlugin_pinvoke.servoUnityGetVersion(sb, sb.Capacity);
        if (ok) return sb.ToString();
        else return "";
    }

    public void ServoUnityInit(ServoUnityPluginWindowCreatedCallback wccb, ServoUnityPluginWindowResizedCallback wrcb, ServoUnityPluginBrowserEventCallback becb)
    {
        windowCreatedCallback = wccb;
        windowResizedCallback = wrcb;
        browserEventCallback = becb;
        
        // Create the callback stub prior to registering the callback on the native side.
        windowCreatedCallbackGCH = GCHandle.Alloc(windowCreatedCallback); // Does not need to be pinned, see http://stackoverflow.com/a/19866119/316487 
        windowResizedCallbackGCH = GCHandle.Alloc(windowResizedCallback);
        browserEventCallbackGCH = GCHandle.Alloc(browserEventCallback);
        
        ServoUnityPlugin_pinvoke.servoUnityInit(windowCreatedCallback, windowResizedCallback, browserEventCallback);
    }

    public void ServoUnityFinalise()
    {
        ServoUnityPlugin_pinvoke.servoUnityFinalise();
        windowCreatedCallback = null;
        windowResizedCallback = null;
        browserEventCallback = null;
        
        // Free the callback stubs after deregistering the callbacks on the native side.
        windowCreatedCallbackGCH.Free();
        windowResizedCallbackGCH.Free();
        browserEventCallbackGCH.Free();
    }

    public void ServoUnitySetResourcesPath(string path)
    {
        ServoUnityPlugin_pinvoke.servoUnitySetResourcesPath(path);
    }

    public enum ServoUnityKeyCode
    {
        Null = 0,
        Character = 1,
        Backspace = 2,
        Delete = 3,
        Tab = 4,
        Clear = 5,
        Return = 6,
        Pause = 7,
        Escape = 8,
        Space = 9,
        UpArrow = 10,
        DownArrow = 11,
        RightArrow = 12,
        LeftArrow = 13,
        Insert = 14,
        Home = 15,
        End = 16,
        PageUp = 17,
        PageDown = 18,
        F1 = 19,
        F2 = 20,
        F3 = 21,
        F4 = 22,
        F5 = 23,
        F6 = 24,
        F7 = 25,
        F8 = 26,
        F9 = 27,
        F10 = 28,
        F11 = 29,
        F12 = 30,
        F13 = 31,
        F14 = 32,
        F15 = 33,
        F16 = 34,
        F17 = 35,
        F18 = 36,
        F19 = 37,
        Numlock = 38,
        CapsLock = 39,
        ScrollLock = 40,
        RightShift = 41,
        LeftShift = 42,
        RightControl = 43,
        LeftControl = 44,
        RightAlt = 45,
        LeftAlt = 46,
        LeftCommand = 47,
        LeftWindows = 48,
        RightCommand = 49,
        RightWindows = 50,
        AltGr = 51,
        Help = 52,
        Print = 53,
        SysReq = 54,
        Break = 55,
        Menu = 56,
        Keypad0 = 57,
        Keypad1 = 58,
        Keypad2 = 59,
        Keypad3 = 60,
        Keypad4 = 61,
        Keypad5 = 62,
        Keypad6 = 63,
        Keypad7 = 64,
        Keypad8 = 65,
        Keypad9 = 66,
        KeypadPeriod = 67,
        KeypadDivide = 68,
        KeypadMultiply = 69,
        KeypadMinus = 70,
        KeypadPlus = 71,
        KeypadEnter = 72,
        KeypadEquals = 73,
        Max
    };

    public void ServoUnityKeyEvent(int windowIndex, bool upDown, ServoUnityKeyCode keyCode, int character)
    {
        ServoUnityPlugin_pinvoke.servoUnityKeyEvent(windowIndex, upDown ? 1 : 0, (int)keyCode, character);
    }

    public int ServoUnityGetWindowCount()
    {
        return ServoUnityPlugin_pinvoke.servoUnityGetWindowCount();
    }

    public bool ServoUnityRequestNewWindow(int uid, int widthPixelsRequested, int heightPixelsRequested)
    {
        return ServoUnityPlugin_pinvoke.servoUnityRequestNewWindow(uid, widthPixelsRequested, heightPixelsRequested);
    }
    
    public bool ServoUnityRequestWindowSizeChange(int windowIndex, int widthPixelsRequested, int heightPixelsRequested)
    {
        return ServoUnityPlugin_pinvoke.servoUnityRequestWindowSizeChange(windowIndex, widthPixelsRequested, heightPixelsRequested);
    }

    public TextureFormat NativeFormatToTextureFormat(int formatNative)
    {
        switch (formatNative)
        {
            case 1:
                return TextureFormat.RGBA32;
            case 2:
                return TextureFormat.BGRA32;
            case 3:
                return TextureFormat.ARGB32;
            //case 4:
            //    format = TextureFormat.ABGR32;
            case 5:
                return TextureFormat.RGB24;
            //case 6:
            //    format = TextureFormat.BGR24;
            case 7:
                return TextureFormat.RGBA4444;
            //case 8:
            //    format = TextureFormat.RGBA5551;
            case 9:
                return TextureFormat.RGB565;
            default:
                return (TextureFormat) 0;
        }
    }

    public bool ServoUnityGetTextureFormat(int windowIndex, out int width, out int height, out TextureFormat format,
        out bool mipChain, out bool linear, out IntPtr nativeTexureID)
    {
        int formatNative;
        IntPtr[] nativeTextureIDHandle = new IntPtr[1];
        ServoUnityPlugin_pinvoke.servoUnityGetWindowTextureFormat(windowIndex, out width, out height, out formatNative, out mipChain,
            out linear, nativeTextureIDHandle);
        nativeTexureID = nativeTextureIDHandle[0];

        format = NativeFormatToTextureFormat(formatNative);
        if (format == (TextureFormat) 0)
        {
            return false;
        }

        return true;
    }


    public bool ServoUnitySetWindowUnityTextureID(int windowIndex, IntPtr nativeTexturePtr)
    {
        return ServoUnityPlugin_pinvoke.servoUnitySetWindowUnityTextureID(windowIndex, nativeTexturePtr);
    }

    public void ServoUnityRequestWindowUpdate(int windowIndex, float timeDelta)
    {
        // Rather than calling ServoUnityPlugin_pinvoke.servoUnityRequestWindowUpdate(windowIndex, timeDelta)
        // directly, make sure the call runs on the rendering thread.
        ServoUnityPlugin_pinvoke.servoUnitySetRenderEventFunc1Params(windowIndex, timeDelta);
        GL.IssuePluginEvent(ServoUnityPlugin_pinvoke.GetRenderEventFunc(), 1);
        GL.InvalidateState();
    }


    public string ServoUnityGetWindowTitle(int windowIndex)
    {
        var sb = new StringBuilder(1024); // 1kb
        ServoUnityPlugin_pinvoke.servoUnityGetWindowMetadata(windowIndex, sb, sb.Capacity, null, 0);
        return sb.ToString();
    }

    public string ServoUnityGetWindowURL(int windowIndex)
    {
        var sb = new StringBuilder(16384); // 16kb
        ServoUnityPlugin_pinvoke.servoUnityGetWindowMetadata(windowIndex, null, 0, sb, sb.Capacity);
        return sb.ToString();
    }

    public void ServoUnityCleanupRenderer(int windowIndex)
    {
        // Rather than calling ServoUnityPlugin_pinvoke.servoUnityCleanupRenderer(windowIndex)
        // directly, make sure the call runs on the rendering thread.
        ServoUnityPlugin_pinvoke.servoUnitySetRenderEventFunc2Param(windowIndex);
        GL.IssuePluginEvent(ServoUnityPlugin_pinvoke.GetRenderEventFunc(), 2);
        GL.InvalidateState();
    }

    public enum ServoUnityPointerEventID
    {
        Enter = 0,
        Exit = 1,
        Over = 2,
        Press = 3,
        Release = 4,
        Click = 5,
        ScrollDiscrete = 6,
        Max
    };

    public enum ServoUnityPointerEventMouseButtonID
    {
        Left = 0,
        Right = 1,
        Middle = 2,
        Max
    };

    public void ServoUnityWindowPointerEvent(int windowIndex, ServoUnityPointerEventID eventID, int eventParam0, int eventParam1, int windowX, int windowY)
    {
        ServoUnityPlugin_pinvoke.servoUnityWindowPointerEvent(windowIndex, (int) eventID, eventParam0, eventParam1, windowX, windowY);
    }

    public enum ServoUnityWindowBrowserControlEventID
    {
        Refresh = 0,
        Reload = 1,
        Stop = 2,
        GoBack = 3,
        GoForward = 4,
        GoHome = 5,
        Navigate = 6,
        Max
    };

    public void ServoUnityWindowBrowserControlEvent(int windowIndex, ServoUnityWindowBrowserControlEventID eventID, int eventParam0, int eventParam1, string eventParamS)
    {
        ServoUnityPlugin_pinvoke.servoUnityWindowBrowserControlEvent(windowIndex, (int)eventID, eventParam0, eventParam1, eventParamS);
    }

    public enum ServoUnityBrowserEventType
    {
        NOP = 0,
        Shutdown = 1,
        LoadStateChanged = 2, // eventData1: 0=LoadEnded, 1=LoadStarted,
        FullscreenStateChanged = 3, // eventData1: 0=WillEnterFullscreen, 1=DidEnterFullscreen, 2=WillExitFullscreen, 3=DidExitFullscreen,
        IMEStateChanged = 4, // eventData1: 0=HideIME, 1=ShowIME
        HistoryChanged = 5, // eventData1: 0=CantGoBack, 1=CanGoBack, eventData1: 0=CantGoForward, 1=CanGoForward
        TitleChanged = 6,
        URLChanged = 7,
        Max
    };

    public bool ServoUnityCloseWindow(int windowIndex)
    {
        return ServoUnityPlugin_pinvoke.servoUnityCloseWindow(windowIndex);
    }

    public void ServoUnityServiceWindowEvents(int windowIndex)
    {
        ServoUnityPlugin_pinvoke.servoUnityServiceWindowEvents(windowIndex);
    }

    public bool ServoUnityCloseAllWindows()
    {
        return ServoUnityPlugin_pinvoke.servoUnityCloseAllWindows();
    }

    public enum ServoUnityParam {
        b_CloseNativeWindowOnClose = 0,
        s_SearchURI = 1,
        s_Homepage = 2,
        Max
    };

    public void ServoUnitySetParamBool(ServoUnityParam param, bool flag)
    {
        ServoUnityPlugin_pinvoke.servoUnitySetParamBool((int)param, flag);
    }

    public void ServoUnitySetParamInt(ServoUnityParam param, int val)
    {
        ServoUnityPlugin_pinvoke.servoUnitySetParamInt((int)param, val);
    }

    public void ServoUnitySetParamFloat(ServoUnityParam param, bool val)
    {
        ServoUnityPlugin_pinvoke.servoUnitySetParamFloat((int)param, val);
    }

    public void ServoUnitySetParamString(ServoUnityParam param, string s)
    {
        ServoUnityPlugin_pinvoke.servoUnitySetParamString((int)param, s);
    }

    public bool ServoUnityGetParamBool(ServoUnityParam param)
    {
        return ServoUnityPlugin_pinvoke.servoUnityGetParamBool((int)param);
    }

    public int ServoUnityGetParamInt(ServoUnityParam param)
    {
        return ServoUnityPlugin_pinvoke.servoUnityGetParamInt((int)param);
    }

    public float ServoUnityGetParamFloat(ServoUnityParam param)
    {
        return ServoUnityPlugin_pinvoke.servoUnityGetParamFloat((int)param);
    }

    public string ServoUnityGetParamString(ServoUnityParam param)
    {
        var sb = new StringBuilder(16384); // 16kb
        ServoUnityPlugin_pinvoke.servoUnityGetParamString((int)param, sb, sb.Capacity);
        return sb.ToString();
    }
}
