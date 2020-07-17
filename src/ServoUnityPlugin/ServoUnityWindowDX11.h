//
// ServoUnityWindowDX11.h
//
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0.If a copy of the MPL was not distributed with this
// file, You can obtain one at https ://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla, Inc.
//
// Author(s): Philip Lamb
//
// An implementation for a Servo window that renders to a DirectX 11 texture handle.
// Note that this is presently UNIMPLEMENTED, but the class is retained here for
// possible future development.
//

#pragma once
#include "ServoUnityWindow.h"
#ifdef SUPPORT_D3D11
#include <cstdint>
#include <string>
#include <Windows.h>
#include "IUnityInterface.h"

struct ID3D11Texture2D;

class ServoUnityWindowDX11 : public ServoUnityWindow
{
private:

	ID3D11Texture2D* m_servoTexPtr;
	void* m_servoTexHandle;
	Size m_size;
	int m_format;
	void *m_unityTexPtr;
    PFN_WINDOWCREATEDCALLBACK m_windowCreatedCallback;
    PFN_WINDOWRESIZEDCALLBACK m_windowResizedCallback;
    PFN_BROWSEREVENTCALLBACK m_browserEventCallback;

public:
	static void initDevice(IUnityInterfaces* unityInterfaces);
	static void finalizeDevice();

	ServoUnityWindowDX11(int uid, int uidExt, Size size);
	~ServoUnityWindowDX11() ;
    //ServoUnityWindowDX11(const ServoUnityWindowDX11&) = delete;
	//void operator=(const ServoUnityWindowDX11&) = delete;

	bool init(PFN_WINDOWCREATEDCALLBACK windowCreatedCallback, PFN_WINDOWRESIZEDCALLBACK windowResizedCallback, PFN_BROWSEREVENTCALLBACK browserEventCallback) override;
    RendererAPI rendererAPI() override {return RendererAPI::DirectX11;}
	Size size() override;
	void setSize(Size size) override;
	void setNativePtr(void* texPtr) override;
	void* nativePtr() override;

    void serviceWindowEvents(void) override {}
    std::string windowTitle(void) override {return std::string();}
    std::string windowURL(void) override {return std::string();}

	// Must be called from render thread.
	void requestUpdate(float timeDelta) override;

	int format() override { return m_format; }

    void CloseServoWindow() override {}
	void pointerEnter() override;
	void pointerExit() override;
	void pointerOver(int x, int y) override;
	void pointerPress(int button, int x, int y) override;
	void pointerRelease(int button, int x, int y) override;
	void pointerClick(int button, int x, int y) override;
    void pointerScrollDiscrete(int x_scroll, int y_scroll, int x, int y) override;
	void keyDown(int charCode) override;
    void keyUp(int charCode) override;
};

#endif // SUPPORT_D3D11
