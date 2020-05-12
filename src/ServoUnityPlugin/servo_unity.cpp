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

#include "ServoUnityWindowDX11.h"
#include "ServoUnityWindowGL.h"
#include <memory>
#include <string>
#include <assert.h>
#include <map>

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
static PFN_FULLSCREENBEGINCALLBACK m_fullScreenBeginCallback = nullptr;
static PFN_FULLSCREENENDCALLBACK m_fullScreenEndCallback = nullptr;
static PFN_BROWSEREVENTCALLBACK m_browserEventCallback = nullptr;

static std::map<int, std::unique_ptr<ServoUnityWindow>> s_windows;
static int s_windowIndexNext = 1;

static bool s_param_CloseNativeWindowOnClose = true;

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

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
	switch (eventType) {
		case kUnityGfxDeviceEventInitialize:
            {
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

static int s_RenderEventFunc1Param_windowIndex = 0;
static float s_RenderEventFunc1Param_timeDelta = 0.0f;

void servoUnitySetRenderEventFunc1Params(int windowIndex, float timeDelta)
{
	s_RenderEventFunc1Param_windowIndex = windowIndex;
	s_RenderEventFunc1Param_timeDelta = timeDelta;
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
		servoUnityRequestWindowUpdate(s_RenderEventFunc1Param_windowIndex, s_RenderEventFunc1Param_timeDelta);
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

void servoUnityRegisterFullScreenBeginCallback(PFN_FULLSCREENBEGINCALLBACK fullScreenBeginCallback)
{
	m_fullScreenBeginCallback = fullScreenBeginCallback;
}

void servoUnityRegisterFullScreenEndCallback(PFN_FULLSCREENENDCALLBACK fullScreenEndCallback)
{
	m_fullScreenEndCallback = fullScreenEndCallback;
}

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

bool servoUnityGetVersion(char *buffer, int length)
{
	if (!buffer) return false;

	const char *version = SERVO_UNITY_PLUGIN_VERSION; // TODO: replace with method that fetches the actual Servo version.
	strncpy(buffer, version, length - 1); buffer[length - 1] = '\0';
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

void servoUnityInitServo(PFN_WINDOWCREATEDCALLBACK windowCreatedCallback, PFN_WINDOWRESIZEDCALLBACK windowResizedCallback, PFN_BROWSEREVENTCALLBACK browserEventCallback)
{
	m_windowCreatedCallback = windowCreatedCallback;
	m_windowResizedCallback = windowResizedCallback;
	m_browserEventCallback = browserEventCallback;
}

void servoUnityFinaliseServo(void)
{
	m_windowCreatedCallback = nullptr;
	m_windowResizedCallback = nullptr;
	m_browserEventCallback = nullptr;
}

void servoUnityKeyEvent(int windowIndex, int keyCode)
{
	SERVOUNITYLOGd("Got keyCode %d.\n", keyCode);

	auto window_iter = s_windows.find(windowIndex);
	if (window_iter != s_windows.end()) {
		window_iter->second->keyPress(keyCode);
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
	if (!inserted.second || !inserted.first->second->init(m_windowCreatedCallback)) {
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
}

void servoUnitySetParamFloat(int param, float val)
{
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
	return 0;
}

float servoUnityGetParamFloat(int param)
{
	return 0.0f;
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

	if (m_windowResizedCallback) {
		// Once window resize request has completed, get the size that actually was set, and call back to Unity
		ServoUnityWindow::Size size = window_iter->second->size();
		(*m_windowResizedCallback)(window_iter->second->uidExt(), size.w, size.h);
	}
	// TODO: Return true once above method implemented...
	return false;
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

void servoUnityWindowPointerEvent(int windowIndex, int eventID, int windowX, int windowY)
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
		window_iter->second->pointerPress(windowX, windowY);
		break;
	case ServoUnityPointerEventID_Release:
		window_iter->second->pointerRelease(windowX, windowY);
		break;
	case ServoUnityPointerEventID_ScrollDiscrete:
		window_iter->second->pointerScrollDiscrete(windowX, windowY);
		break;
	default:
		break;
	}
}