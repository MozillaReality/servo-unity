//
// servo_unity.cpp
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0.If a copy of the MPL was not distributed with this
// file, You can obtain one at https ://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla, Inc.
//
// Author(s): Philip Lamb
//
// Implementations of plugin interfaces which are invoked from Unity via P/Invoke.
//

#include "servo_unity_c.h"
#include "servo_unity_log.h"
#include "servo_unity_internal.h"

#include "ServoUnityWindowDX11.h"
#include "ServoUnityWindowGL.h"
#include <memory>
#include <assert.h>
#include <map>
#include "simpleservo.h"
#include "utils.h"

//
// Unity low-level plugin interface.
//

#include "IUnityInterface.h"
#include "IUnityGraphics.h"

// --------------------------------------------------------------------------
// UnitySetInterfaces

static IUnityInterfaces* s_UnityInterfaces = NULL;
static IUnityGraphics* s_Graphics = NULL;
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);
static char *s_ResourcesPath = NULL;

// --------------------------------------------------------------------------

static PFN_WINDOWCREATEDCALLBACK m_windowCreatedCallback = nullptr;
static PFN_WINDOWRESIZEDCALLBACK m_windowResizedCallback = nullptr;
static PFN_BROWSEREVENTCALLBACK m_browserEventCallback = nullptr;

static std::map<int, std::unique_ptr<ServoUnityWindow>> s_windows;
static int s_windowIndexNext = 1;

static const char *s_servoVersion = nullptr; // To avoid repeated leaking of servo's version string, we'll stash it here.

// --------------------------------------------------------------------------
//  Configuration parameters

bool s_param_CloseNativeWindowOnClose = true;
std::string s_param_SearchURI = SEARCH_URI_DEFAULT;
std::string s_param_Homepage = HOMEPAGE_DEFAULT;

// --------------------------------------------------------------------------

extern "C" void	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
	s_UnityInterfaces = unityInterfaces;
	s_Graphics = unityInterfaces->Get<IUnityGraphics>();
	s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
	// Run OnGraphicsDeviceEvent(initialize) manually on plugin load
	// to not miss the event in case the graphics device is already initialized.
	OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

extern "C" void	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
	s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
}

static UnityGfxRenderer s_RendererType = kUnityGfxRendererNull;

// Note that Unity uses multiple OpenGL contexts, and the one active when
// this event fires may not be the same one active during UnityRenderingEvent events,
// so don't do any context-specific initialisation here (i.e. no textures, VAOs etc.)
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
	switch (eventType) {
		case kUnityGfxDeviceEventInitialize:
            {
                SERVOUNITYLOGi("OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize) called on thread %" PRIu64 ".\n", getThreadID());
                s_RendererType = s_Graphics->GetRenderer();
                switch (s_RendererType) {
#ifdef SUPPORT_D3D11
                    case kUnityGfxRendererD3D11:
                        SERVOUNITYLOGi("Using DirectX 11 renderer.\n");
                        ServoUnityWindowDX11::initDevice(s_UnityInterfaces);
                        break;
#endif // SUPPORT_D3D11
#ifdef SUPPORT_OPENGL_CORE
                    case kUnityGfxRendererOpenGLCore:
                        SERVOUNITYLOGi("Using OpenGL renderer.\n");
                        ServoUnityWindowGL::initDevice();
                        break;
#endif // SUPPORT_OPENGL_CORE
                    default:
                        SERVOUNITYLOGe("Unsupported renderer.\n");
                        return;
                    }
                    break;
            }
            break;
		case kUnityGfxDeviceEventShutdown:
            {
                switch (s_RendererType) {
#ifdef SUPPORT_D3D11
                    case kUnityGfxRendererD3D11:
                    ServoUnityWindowDX11::finalizeDevice();
                    break;
#endif // SUPPORT_D3D11
#ifdef SUPPORT_OPENGL_CORE
                    case kUnityGfxRendererOpenGLCore:
                    ServoUnityWindowGL::finalizeDevice();
                    break;
#endif // SUPPORT_OPENGL_CORE
                    default:
                    SERVOUNITYLOGe("Unsupported renderer.\n");
                    return;
                }
                s_RendererType = kUnityGfxRendererNull;
                break;
            }
            break;
        case kUnityGfxDeviceEventBeforeReset:
        case kUnityGfxDeviceEventAfterReset:
            break;
            
	};
}

static int s_RenderEventFunc12Param_windowIndex = 0;
static float s_RenderEventFunc1Param_timeDelta = 0.0f;

void servoUnitySetRenderEventFunc1Params(int windowIndex, float timeDelta)
{
	s_RenderEventFunc12Param_windowIndex = windowIndex;
	s_RenderEventFunc1Param_timeDelta = timeDelta;
}

void servoUnitySetRenderEventFunc2Param(int windowIndex)
{
    s_RenderEventFunc12Param_windowIndex = windowIndex;
}

static void UNITY_INTERFACE_API OnRenderEvent(int eventID)
{
	// Unknown / unsupported graphics device type? Do nothing
	switch (s_RendererType) {
	case kUnityGfxRendererD3D11:
		break;
	case kUnityGfxRendererOpenGLCore:
		break; 
	default:
		SERVOUNITYLOGe("Unsupported renderer.\n");
		return;
	}

	switch (eventID) {
	case 1:
		servoUnityRequestWindowUpdate(s_RenderEventFunc12Param_windowIndex, s_RenderEventFunc1Param_timeDelta);
		break;
    case 2:
        servoUnityCleanupRenderer(s_RenderEventFunc12Param_windowIndex);
        break;
	default:
		break;
	}
}


// GetRenderEventFunc, a function we export which is used to get a rendering event callback function.
extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc()
{
	return OnRenderEvent;
}

//
// ServoUnity plugin implementation.
//

void servoUnityRegisterLogCallback(PFN_LOGCALLBACK logCallback)
{
	servoUnityLogSetLogger(logCallback, 1); // 1 -> only callback on same thread, as required e.g. by C# interop.
}

void servoUnitySetLogLevel(const int logLevel)
{
	if (logLevel >= 0) {
		servoUnityLogLevel = logLevel;
	}
}

void servoUnityFlushLog(void)
{
    servoUnityLogFlush();
}

bool servoUnityGetVersion(char *buffer, int length)
{
	if (!buffer || length <= 0) return false;

    if (!s_servoVersion) {
        s_servoVersion = servo_version();
        if (!s_servoVersion) {
            SERVOUNITYLOGw("Could not read servo version.\n");
            return false;
        }
    }
	strncpy(buffer, s_servoVersion, length - 1); buffer[length - 1] = '\0'; // Guarantee nul-termination, even if truncated.
	return true;
}

void servoUnitySetResourcesPath(const char *path)
{
	free(s_ResourcesPath);
	s_ResourcesPath = NULL;
	if (path && path[0]) {
		s_ResourcesPath = strdup(path);
		SERVOUNITYLOGi("Resources path is '%s'.\n", s_ResourcesPath);
	}
}

void servoUnityInit(PFN_WINDOWCREATEDCALLBACK windowCreatedCallback, PFN_WINDOWRESIZEDCALLBACK windowResizedCallback, PFN_BROWSEREVENTCALLBACK browserEventCallback)
{
    SERVOUNITYLOGi("servoUnityInit called on thread %" PRIu64 ".\n", getThreadID());
	m_windowCreatedCallback = windowCreatedCallback;
	m_windowResizedCallback = windowResizedCallback;
	m_browserEventCallback = browserEventCallback;
}

void servoUnityFinalise(void)
{
	m_windowCreatedCallback = nullptr;
	m_windowResizedCallback = nullptr;
	m_browserEventCallback = nullptr;
}

void servoUnityKeyEvent(int windowIndex, int upDown, int keyCode, int character)
{
	auto window_iter = s_windows.find(windowIndex);
	if (window_iter != s_windows.end()) {
        window_iter->second->keyEvent(upDown, keyCode, character);
	}
}

int servoUnityGetWindowCount(void)
{
	return (int)s_windows.size();
}

bool servoUnityRequestNewWindow(int uidExt, int widthPixelsRequested, int heightPixelsRequested)
{
	std::unique_ptr<ServoUnityWindow> window;
#ifdef SUPPORT_D3D11
    if (s_RendererType == kUnityGfxRendererD3D11) {
        SERVOUNITYLOGi("Servo window requested with DirectX 11 renderer.\n");
		window = std::make_unique<ServoUnityWindowDX11>(s_windowIndexNext++, uidExt, ServoUnityWindow::Size({ widthPixelsRequested, heightPixelsRequested }));
	} else
#endif // SUPPORT_D3D11
#ifdef SUPPORT_OPENGL_CORE
    if (s_RendererType == kUnityGfxRendererOpenGLCore) {
        SERVOUNITYLOGi("Servo window requested with OpenGL renderer.\n");
		window = std::make_unique<ServoUnityWindowGL>(s_windowIndexNext++, uidExt, ServoUnityWindow::Size({ widthPixelsRequested, heightPixelsRequested }));
	} else
#endif // SUPPORT_OPENGL_CORE
    {
		SERVOUNITYLOGe("Cannot create window. Unknown/unsupported render type detected.\n");
	}
	auto inserted = s_windows.emplace(window->uid(), move(window));
	if (!inserted.second || !inserted.first->second->init(m_windowCreatedCallback, m_windowResizedCallback, m_browserEventCallback)) {
		SERVOUNITYLOGe("Error initing window.\n");
		return false;
	}
	return true;
}

bool servoUnitySetWindowUnityTextureID(int windowIndex, void *nativeTexturePtr)
{
    if (s_RendererType != kUnityGfxRendererD3D11 && s_RendererType != kUnityGfxRendererOpenGLCore) {
        SERVOUNITYLOGe("Unsupported renderer.\n");
        return false;
    }
   
	auto window_iter = s_windows.find(windowIndex);
	if (window_iter == s_windows.end()) {
		SERVOUNITYLOGe("Requested to set unity texture ID for non-existent window with index %d.\n", windowIndex);
		return false;
	}

	window_iter->second->setNativePtr(nativeTexturePtr);
	SERVOUNITYLOGi("servoUnitySetWindowUnityTextureID set texturePtr %p.\n", nativeTexturePtr);
	return true;
}

void servoUnitySetParamBool(int param, bool flag)
{
	switch (param) {
		case ServoUnityParam_b_CloseNativeWindowOnClose:
			s_param_CloseNativeWindowOnClose = flag;
			break;
		default:
			break;
	}
}

void servoUnitySetParamInt(int param, int val)
{
    // No parameters to set yet.
}

void servoUnitySetParamString(int param, const char *s)
{
    switch (param) {
        case ServoUnityParam_s_SearchURI:
            s_param_SearchURI = std::string(s);
            break;
        case ServoUnityParam_s_Homepage:
            s_param_Homepage = std::string(s);
            break;
        default:
            break;
    }
}

void servoUnitySetParamFloat(int param, float val)
{
    // No parameters to set yet.
}

bool servoUnityGetParamBool(int param)
{
	switch (param) {
		case ServoUnityParam_b_CloseNativeWindowOnClose:
			return s_param_CloseNativeWindowOnClose;
			break;
		default:
			break;
	}
	return false;
}

int servoUnityGetParamInt(int param)
{
    // No parameters to query yet.
	return 0;
}

float servoUnityGetParamFloat(int param)
{
    // No parameters to query yet.
	return 0.0f;
}

void servoUnityGetParamString(int param, char *sbuf, int sbufLen)
{
    if (!sbuf || sbufLen <= 0) return;
    switch (param) {
        case ServoUnityParam_s_SearchURI:
            strncpy(sbuf, s_param_SearchURI.c_str(), sbufLen - 1);
            break;
        case ServoUnityParam_s_Homepage:
            strncpy(sbuf, s_param_Homepage.c_str(), sbufLen - 1);
            break;
        default:
            break;
    }
    sbuf[sbufLen - 1] = '\0'; // Guarantee nul-termination, even if truncated.
}

bool servoUnityCloseWindow(int windowIndex)
{
	auto window_iter = s_windows.find(windowIndex);
	if (window_iter == s_windows.end()) return false;
	
	window_iter->second->CloseServoWindow();	
	s_windows.erase(window_iter);
	return true;
}

bool servoUnityCloseAllWindows(void)
{
	s_windows.clear();
	return true;
}

bool servoUnityGetWindowTextureFormat(int windowIndex, int *width, int *height, int *format, bool *mipChain, bool *linear, void **nativeTextureID_p)
{
	auto window_iter = s_windows.find(windowIndex);
	if (window_iter == s_windows.end()) return false;

	ServoUnityWindow::Size size = window_iter->second->size();
	if (width) *width = size.w;
	if (height) *height = size.h;
	if (format) *format = window_iter->second->format();
	if (mipChain) *mipChain = false;
	if (linear) *linear = true;
	if (nativeTextureID_p) *nativeTextureID_p = window_iter->second->nativePtr();
	return true;
}

uint64_t servoUnityGetBufferSizeForTextureFormat(int width, int height, int format)
{
	if (!width || !height) return 0;
	int Bpp;
	switch (format) {
		case ServoUnityTextureFormat_BGRA32:
		case ServoUnityTextureFormat_RGBA32:
		case ServoUnityTextureFormat_ABGR32:
		case ServoUnityTextureFormat_ARGB32:
			Bpp = 4;
			break;
		case ServoUnityTextureFormat_BGR24:
		case ServoUnityTextureFormat_RGB24:
			Bpp = 3;
			break;
		case ServoUnityTextureFormat_RGB565:
		case ServoUnityTextureFormat_RGBA5551:
		case ServoUnityTextureFormat_RGBA4444:
			Bpp = 2;
			break;
		default:
			Bpp = 0;
	}
	return width * height * Bpp;
}

bool servoUnityRequestWindowSizeChange(int windowIndex, int width, int height)
{
	auto window_iter = s_windows.find(windowIndex);
	if (window_iter == s_windows.end()) return false;
	
	window_iter->second->setSize({ width, height });

    return true;
}

void servoUnityServiceWindowEvents(int windowIndex)
{
    auto window_iter = s_windows.find(windowIndex);
    if (window_iter == s_windows.end()) {
        SERVOUNITYLOGe("Requested event service for non-existent window with index %d.\n", windowIndex);
        return;
    }
    window_iter->second->serviceWindowEvents();
}

void servoUnityGetWindowMetadata(int windowIndex, char *titleBuf, int titleBufLen, char *urlBuf, int urlBufLen)
{
    auto window_iter = s_windows.find(windowIndex);
    if (window_iter == s_windows.end()) {
        SERVOUNITYLOGe("Requested window metadata for non-existent window with index %d.\n", windowIndex);
        return;
    }
    if (titleBuf && titleBufLen > 0) {
        std::string title = window_iter->second->windowTitle();
        strncpy(titleBuf, title.c_str(), titleBufLen - 1);
        titleBuf[titleBufLen - 1] = '\0';  // Guarantee nul-termination, even if truncated.
    }
    if (urlBuf && urlBufLen > 0) {
        std::string URL = window_iter->second->windowURL();
        strncpy(urlBuf, URL.c_str(), urlBufLen - 1);
        urlBuf[urlBufLen - 1] = '\0';  // Guarantee nul-termination, even if truncated.
    }
}

void servoUnityRequestWindowUpdate(int windowIndex, float timeDelta)
{
	auto window_iter = s_windows.find(windowIndex);
	if (window_iter == s_windows.end()) {
		SERVOUNITYLOGe("Requested update for non-existent window with index %d.\n", windowIndex);
		return;
	}
	window_iter->second->requestUpdate(timeDelta);
}

void servoUnityCleanupRenderer(int windowIndex)
{
    auto window_iter = s_windows.find(windowIndex);
    if (window_iter == s_windows.end()) {
        SERVOUNITYLOGe("Requested cleanup for non-existent window with index %d.\n", windowIndex);
        return;
    }
    window_iter->second->cleanupRenderer();
}

void servoUnityWindowPointerEvent(int windowIndex, int eventID, int eventParam0, int eventParam1, int windowX, int windowY)
{
	auto window_iter = s_windows.find(windowIndex);
	if (window_iter == s_windows.end()) return;

	switch (eventID) {
	case ServoUnityPointerEventID_Enter:
		window_iter->second->pointerEnter();
		break;
	case ServoUnityPointerEventID_Exit:
		window_iter->second->pointerExit();
		break;
	case ServoUnityPointerEventID_Over:
		window_iter->second->pointerOver(windowX, windowY);
		break;
	case ServoUnityPointerEventID_Press:
		window_iter->second->pointerPress(eventParam0, windowX, windowY);
		break;
	case ServoUnityPointerEventID_Release:
		window_iter->second->pointerRelease(eventParam0, windowX, windowY);
		break;
    case ServoUnityPointerEventID_Click:
        window_iter->second->pointerClick(eventParam0, windowX, windowY);
        break;
	case ServoUnityPointerEventID_ScrollDiscrete:
		window_iter->second->pointerScrollDiscrete(eventParam0, eventParam1, windowX, windowY);
		break;
	default:
		break;
	}
}

void servoUnityWindowBrowserControlEvent(int windowIndex, int eventID, int eventParam0, int eventParam1, const char *eventParamS)
{
    auto window_iter = s_windows.find(windowIndex);
    if (window_iter == s_windows.end()) return;

    switch (eventID) {
    case ServoUnityWindowBrowserControlEventID_Refresh:
        window_iter->second->refresh();
        break;
    case ServoUnityWindowBrowserControlEventID_Reload:
        window_iter->second->reload();
        break;
    case ServoUnityWindowBrowserControlEventID_Stop:
        window_iter->second->stop();
        break;
    case ServoUnityWindowBrowserControlEventID_GoBack:
        window_iter->second->goBack();
        break;
    case ServoUnityWindowBrowserControlEventID_GoForward:
        window_iter->second->goForward();
        break;
    case ServoUnityWindowBrowserControlEventID_GoHome:
        window_iter->second->goHome();
        break;
    case ServoUnityWindowBrowserControlEventID_Navigate:
        window_iter->second->navigate(std::string(eventParamS));
        break;
    default:
        break;
    }
}
