//
// ServoUnityPlugin_pinvoke.cs
// 
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla.
//
// Author(s): Philip Lamb
//
// Low-level declarations of plugin interfaces that are exported from the native DLL.
//

using System;
using System.Text;
using System.Runtime.InteropServices;

public static class ServoUnityPlugin_pinvoke
{
    // The name of the external library containing the native functions
    private const string LIBRARY_NAME = "servo_unity";

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr GetRenderEventFunc();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityRegisterLogCallback(ServoUnityPluginLogCallback callback);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityRegisterFullScreenBeginCallback(ServoUnityPluginFullScreenBeginCallback callback);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityRegisterFullScreenEndCallback(ServoUnityPluginFullEndCallback callback);
    
    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnitySetLogLevel(int logLevel);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityFlushLog();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAsAttribute(UnmanagedType.I1)]
    public static extern bool servoUnityGetVersion([MarshalAs(UnmanagedType.LPStr)]StringBuilder buffer, int length);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityInit(ServoUnityPluginWindowCreatedCallback callback, ServoUnityPluginWindowResizedCallback resizedCallback, ServoUnityPluginBrowserEventCallback browserEventCallback);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityFinalise();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnitySetResourcesPath(string path);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityKeyEvent(int windowIndex, int upDown, int keyCode, int character);
    
    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityWindowPointerEvent(int windowIndex, int eventID, int eventParam0, int eventParam1, int windowX, int windowY);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int servoUnityGetWindowCount();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAsAttribute(UnmanagedType.I1)]
    public static extern bool servoUnityRequestNewWindow(int uid, int widthPixelsRequested, int heightPixelsRequested);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityGetWindowTextureFormat(int windowIndex, out int width, out int height, out int format, [MarshalAsAttribute(UnmanagedType.I1)] out bool mipChain, [MarshalAsAttribute(UnmanagedType.I1)] out bool linear, IntPtr[] nativeTextureIDHandle);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAsAttribute(UnmanagedType.I1)]
    public static extern bool servoUnitySetWindowUnityTextureID(int windowIndex, IntPtr nativeTexturePtr);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAsAttribute(UnmanagedType.I1)]
    public static extern bool servoUnityRequestWindowSizeChange(int windowIndex, int width, int height);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAsAttribute(UnmanagedType.I1)]
    public static extern bool servoUnityCloseWindow(int windowIndex);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAsAttribute(UnmanagedType.I1)]
    public static extern bool servoUnityCloseAllWindows();

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityServiceWindowEvents(int windowIndex);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityGetWindowMetadata(int windowIndex, [MarshalAs(UnmanagedType.LPStr)] StringBuilder titleBuf, int titleBufLen, [MarshalAs(UnmanagedType.LPStr)] StringBuilder urlBuf, int urlBufLen);

    ///
    /// Must be called from rendering thread with active rendering context.
    /// As an alternative to invoking directly, an equivalent invocation can be invoked via call this sequence:
    ///     servoUnitySetRenderEventFunc1Params(windowIndex, timeDelta);
    ///     (*GetRenderEventFunc())(1);
    ///
    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityRequestWindowUpdate(int windowIndex, float timeDelta);

    ///
    /// Must be called from rendering thread with active rendering context.
    /// As an alternative to invoking directly, an equivalent invocation can be invoked via call this sequence:
    ///     servoUnitySetRenderEventFunc2Param(windowIndex);
    ///     (*GetRenderEventFunc())(2);
    ///
    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnityCleanupRenderer(int windowIndex);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnitySetRenderEventFunc1Params(int windowIndex, float timeDelta);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnitySetRenderEventFunc2Param(int windowIndex);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnitySetParamBool(int param, bool flag);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnitySetParamInt(int param, int flag);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void servoUnitySetParamFloat(int param, [MarshalAsAttribute(UnmanagedType.I1)] bool flag);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAsAttribute(UnmanagedType.I1)]
    public static extern bool servoUnityGetParamBool(int param);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int servoUnityGetParamInt(int param);

    [DllImport(LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern float servoUnityGetParamFloat(int param);
}
