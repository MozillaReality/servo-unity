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
        StringBuilder sb = new StringBuilder(128);
        bool ok = ServoUnityPlugin_pinvoke.servoUnityGetVersion(sb, sb.Capacity);
        if (ok) return sb.ToString();
        else return "";
    }

    public void ServoUnityInitServo(ServoUnityPluginWindowCreatedCallback wccb, ServoUnityPluginWindowResizedCallback wrcb, ServoUnityPluginBrowserEventCallback becb)
    {
        windowCreatedCallback = wccb;
        windowResizedCallback = wrcb;
        browserEventCallback = becb;
        
        // Create the callback stub prior to registering the callback on the native side.
        windowCreatedCallbackGCH = GCHandle.Alloc(windowCreatedCallback); // Does not need to be pinned, see http://stackoverflow.com/a/19866119/316487 
        windowResizedCallbackGCH = GCHandle.Alloc(windowResizedCallback);
        browserEventCallbackGCH = GCHandle.Alloc(browserEventCallback);
        
        ServoUnityPlugin_pinvoke.servoUnityInitServo(windowCreatedCallback, windowResizedCallback, browserEventCallback);
    }

    public void ServoUnityFinaliseServo()
    {
        ServoUnityPlugin_pinvoke.servoUnityFinaliseServo();
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

    public void ServoUnityKeyEvent(int windowIndex, int keyCode)
    {
        ServoUnityPlugin_pinvoke.servoUnityKeyEvent(windowIndex, keyCode);
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
        //ServoUnityPlugin_pinvoke.servoUnityRequestWindowUpdate(windowIndex, timeDelta);
        ServoUnityPlugin_pinvoke.servoUnitySetRenderEventFunc1Params(windowIndex, timeDelta);
        GL.IssuePluginEvent(ServoUnityPlugin_pinvoke.GetRenderEventFunc(), 1);
    }

    public enum ServoUnityPointerEventID
    {
        Enter = 0,
        Exit = 1,
        Over = 2,
        Press = 3,
        Release = 4,
        ScrollDiscrete = 5
    };

    public void ServoUnityWindowPointerEvent(int windowIndex, ServoUnityPointerEventID eventID, int windowX, int windowY)
    {
        ServoUnityPlugin_pinvoke.servoUnityWindowPointerEvent(windowIndex, (int) eventID, windowX, windowY);
    }

    public enum ServoUnityBrowserEventType
    {
        NOP = 0,
        Shutdown = 1,
        LoadStateChanged = 2,
        FullscreenStateChanged = 3,
        IMEStateChanged = 4,
        Total = 5
    };

    public bool ServoUnityCloseWindow(int windowIndex)
    {
        return ServoUnityPlugin_pinvoke.servoUnityCloseWindow(windowIndex);
    }

    public bool ServoUnityCloseAllWindows()
    {
        return ServoUnityPlugin_pinvoke.servoUnityCloseAllWindows();
    }

    public enum ServoUnityParam {
        b_CloseNativeWindowOnClose = 0,
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

}
