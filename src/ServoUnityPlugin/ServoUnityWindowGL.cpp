//
// ServoUnityWindowGL.cpp
//
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0.If a copy of the MPL was not distributed with this
// file, You can obtain one at https ://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla, Inc.
//
// Author(s): Philip Lamb
//

#include "ServoUnityWindowGL.h"
#ifdef SUPPORT_OPENGL_CORE

#ifdef __APPLE__
#  include <OpenGL/gl3.h>
#elif defined(_WIN32)
#  include <gl3w/gl3w.h>
#else 
#  define GL_GLEXT_PROTOTYPES
#  include <GL/glcorearb.h>
#endif
#include <stdlib.h>
#include "servo_unity_log.h"
#include "simpleservo.h"

void ServoUnityWindowGL::initDevice() {
#ifdef _WIN32
	gl3wInit();
#endif
}

void ServoUnityWindowGL::finalizeDevice() {
}

ServoUnityWindowGL::ServoUnityWindowGL(int uid, int uidExt, Size size) :
	ServoUnityWindow(uid, uidExt),
	m_size(size),
	m_texID(0),
	m_buf(NULL),
	m_format(ServoUnityTextureFormat_BGRA32),
	m_pixelIntFormatGL(0),
	m_pixelFormatGL(0),
	m_pixelTypeGL(0),
	m_pixelSize(0)
{
}

ServoUnityWindowGL::~ServoUnityWindowGL() {
	if (m_buf) {
		free(m_buf);
		m_buf = NULL;
	}
}

bool ServoUnityWindowGL::init(PFN_WINDOWCREATEDCALLBACK windowCreatedCallback)
{
	switch (m_format) {
		case ServoUnityTextureFormat_RGBA32:
			m_pixelIntFormatGL = GL_RGBA;
			m_pixelFormatGL = GL_RGBA;
			m_pixelTypeGL = GL_UNSIGNED_BYTE;
			m_pixelSize = 4;
			break;
		case ServoUnityTextureFormat_BGRA32:
			m_pixelIntFormatGL = GL_RGBA;
			m_pixelFormatGL = GL_BGRA;
			m_pixelTypeGL = GL_UNSIGNED_BYTE;
			m_pixelSize = 4;
			break;
		case ServoUnityTextureFormat_ARGB32:
			m_pixelIntFormatGL = GL_RGBA;
			m_pixelFormatGL = GL_BGRA;
			m_pixelTypeGL = GL_UNSIGNED_INT_8_8_8_8; // GL_UNSIGNED_INT_8_8_8_8_REV on big-endian.
			m_pixelSize = 4;
			break;
			//case ServoUnityTextureFormat_ABGR32: // Needs GL_EXT_abgr
			//	m_pixelIntFormatGL = GL_RGBA;
			//	m_pixelFormatGL = GL_ABGR_EXT;
			//	m_pixelTypeGL = GL_UNSIGNED_BYTE;
			//	m_pixelSize = 4;
			//	break;
		case ServoUnityTextureFormat_RGB24:
			m_pixelIntFormatGL = GL_RGB;
			m_pixelFormatGL = GL_RGB;
			m_pixelTypeGL = GL_UNSIGNED_BYTE;
			m_pixelSize = 3;
			break;
		case ServoUnityTextureFormat_BGR24:
			m_pixelIntFormatGL = GL_RGBA;
			m_pixelFormatGL = GL_BGR;
			m_pixelTypeGL = GL_UNSIGNED_BYTE;
			m_pixelSize = 3;
			break;
		case ServoUnityTextureFormat_RGBA4444:
			m_pixelIntFormatGL = GL_RGBA;
			m_pixelFormatGL = GL_RGBA;
			m_pixelTypeGL = GL_UNSIGNED_SHORT_4_4_4_4;
			m_pixelSize = 2;
			break;
		case ServoUnityTextureFormat_RGBA5551:
			m_pixelIntFormatGL = GL_RGBA;
			m_pixelFormatGL = GL_RGBA;
			m_pixelTypeGL = GL_UNSIGNED_SHORT_5_5_5_1;
			m_pixelSize = 2;
			break;
		case ServoUnityTextureFormat_RGB565:
			m_pixelIntFormatGL = GL_RGB;
			m_pixelFormatGL = GL_RGB;
			m_pixelTypeGL = GL_UNSIGNED_SHORT_5_6_5;
			m_pixelSize = 2;
			break;
		default:
			break;
	}

	setSize(m_size);

	if (windowCreatedCallback) windowCreatedCallback(m_uidExt, m_uid, m_size.w, m_size.h, m_format);

	return true;
}

ServoUnityWindow::Size ServoUnityWindowGL::size() {
	return m_size;
}

void ServoUnityWindowGL::setSize(ServoUnityWindow::Size size) {
	m_size = size;
	if (m_buf) free(m_buf);
	m_buf = (uint8_t *)calloc(1, m_size.w * m_size.h * m_pixelSize);
}

void ServoUnityWindowGL::setNativePtr(void* texPtr) {
	m_texID = (uint32_t)((uintptr_t)texPtr); // Truncation to 32-bits is the desired behaviour.
}

void* ServoUnityWindowGL::nativePtr() {
	return (void *)((uintptr_t)m_texID); // Extension to pointer-length (usually 64 bits) is the desired behaviour.
}

void ServoUnityWindowGL::requestUpdate(float timeDelta) {

	// Auto-generate a dummy texture. A 100 x 100 square, oscillating in x dimension.
	static int k = 0;
	int i, j;
	k++;
	if (k > 100) k = -100;
	memset(m_buf, 255, m_size.w * m_size.h * m_pixelSize); // Clear to white.
	// Assumes BGRA32!
	for (j = m_size.h / 2 - 50; j < m_size.h / 2 - 25; j++) {
		for (i = m_size.w / 2 - 50 + k; i < m_size.w / 2 + 50 + k; i++) {
			m_buf[(j*m_size.w + i) * 4 + 0] = m_buf[(j*m_size.w + i) * 4 + 1] = m_buf[(j*m_size.w + i) * 4 + 2] = 0; m_buf[(j*m_size.w + i) * 4 + 3] = 255;
		}
	}
	for (j = m_size.h / 2 - 25; j < m_size.h / 2 - 8; j++) {
		// Black bar (25 pixels wide).
		for (i = m_size.w / 2 - 50 + k; i < m_size.w / 2 - 25 + k; i++) {
			m_buf[(j*m_size.w + i) * 4 + 0] = m_buf[(j*m_size.w + i) * 4 + 1] = m_buf[(j*m_size.w + i) * 4 + 2] = 0; m_buf[(j*m_size.w + i) * 4 + 3] = 255;
		}
		// Red bar (17 pixels wide).
		for (i = m_size.w / 2 + 8 + k; i < m_size.w / 2 + 25 + k; i++) {
			m_buf[(j*m_size.w + i) * 4 + 0] = m_buf[(j*m_size.w + i) * 4 + 1] = 0; m_buf[(j*m_size.w + i) * 4 + 2] = m_buf[(j*m_size.w + i) * 4 + 3] = 255;
		}
		// Black bar (25 pixels wide).
		for (i = m_size.w / 2 + 25 + k; i < m_size.w / 2 + 50 + k; i++) {
			m_buf[(j*m_size.w + i) * 4 + 0] = m_buf[(j*m_size.w + i) * 4 + 1] = m_buf[(j*m_size.w + i) * 4 + 2] = 0; m_buf[(j*m_size.w + i) * 4 + 3] = 255;
		}
	}
	for (j = m_size.h / 2 - 8; j < m_size.h / 2 + 8; j++) {
		// Black bar (25 pixels wide).
		for (i = m_size.w / 2 - 50 + k; i < m_size.w / 2 - 25 + k; i++) {
			m_buf[(j*m_size.w + i) * 4 + 0] = m_buf[(j*m_size.w + i) * 4 + 1] = m_buf[(j*m_size.w + i) * 4 + 2] = 0; m_buf[(j*m_size.w + i) * 4 + 3] = 255;
		}
		// Green bar (16 pixels wide).
		for (i = m_size.w / 2 - 8 + k; i < m_size.w / 2 + 8 + k; i++) {
			m_buf[(j*m_size.w + i) * 4 + 0] = 0; m_buf[(j*m_size.w + i) * 4 + 1] = 255; m_buf[(j*m_size.w + i) * 4 + 2] = 0; m_buf[(j*m_size.w + i) * 4 + 3] = 255;
		}
		// Black bar (25 pixels wide).
		for (i = m_size.w / 2 + 25 + k; i < m_size.w / 2 + 50 + k; i++) {
			m_buf[(j*m_size.w + i) * 4 + 0] = m_buf[(j*m_size.w + i) * 4 + 1] = m_buf[(j*m_size.w + i) * 4 + 2] = 0; m_buf[(j*m_size.w + i) * 4 + 3] = 255;
		}
	}
	for (j = m_size.h / 2 + 8; j < m_size.h / 2 + 25; j++) {
		// Black bar (25 pixels wide).
		for (i = m_size.w / 2 - 50 + k; i < m_size.w / 2 - 25 + k; i++) {
			m_buf[(j*m_size.w + i) * 4 + 0] = m_buf[(j*m_size.w + i) * 4 + 1] = m_buf[(j*m_size.w + i) * 4 + 2] = 0; m_buf[(j*m_size.w + i) * 4 + 3] = 255;
		}
		// Blue bar (17 pixels wide).
		for (i = m_size.w / 2 - 25 + k; i < m_size.w / 2 - 8 + k; i++) {
			m_buf[(j*m_size.w + i) * 4 + 0] = 255; m_buf[(j*m_size.w + i) * 4 + 1] = m_buf[(j*m_size.w + i) * 4 + 2] = 0; m_buf[(j*m_size.w + i) * 4 + 3] = 255;
		}
		// Black bar (25 pixels wide).
		for (i = m_size.w / 2 + 25 + k; i < m_size.w / 2 + 50 + k; i++) {
			m_buf[(j*m_size.w + i) * 4 + 0] = m_buf[(j*m_size.w + i) * 4 + 1] = m_buf[(j*m_size.w + i) * 4 + 2] = 0; m_buf[(j*m_size.w + i) * 4 + 3] = 255;
		}
	}
	for (j = m_size.h / 2 + 25; j < m_size.h / 2 + 50; j++) {
		for (i = m_size.w / 2 - 50 + k; i < m_size.w / 2 + 50 + k; i++) {
			m_buf[(j*m_size.w + i) * 4 + 0] = m_buf[(j*m_size.w + i) * 4 + 1] = m_buf[(j*m_size.w + i) * 4 + 2] = 0; m_buf[(j*m_size.w + i) * 4 + 3] = 255;
		}
	}

	// Setup a texture ourselves:
	//glGenTextures(1, &m_texID);
	//glBindTexture(GL_TEXTURE_2D, m_texID);
	//glActiveTexture(GL_TEXTURE0);
	//glTexImage2D(GL_TEXTURE_2D, 0, m_pixelIntFormatGL, m_size.w, m_size.h, 0, m_pixelFormatGL, m_pixelTypeGL, m_buf);
	////glTexImage2D(GL_TEXTURE_RECTANGLE, 0, m_pixelIntFormatGL, m_size.w, m_size.h, 0, m_pixelFormatGL, m_pixelTypeGL, m_buf);
	// Would also later require cleanup:
	//glBindTexture(GL_TEXTURE_2D, 0);
	//glDeleteTextures(1, &m_texID);
	//m_texID = 0;

	glBindTexture(GL_TEXTURE_2D, m_texID);
	glActiveTexture(GL_TEXTURE0);
	glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, m_size.w, m_size.h, GL_RGBA, GL_UNSIGNED_BYTE, m_buf);
	//glTexSubImage2D(GL_TEXTURE_RECTANGLE, 0, 0, 0, m_size.w, m_size.h, GL_RGBA, GL_UNSIGNED_BYTE, m_buf);
}

void ServoUnityWindowGL::pointerEnter() {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerEnter()\n");
}

void ServoUnityWindowGL::pointerExit() {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerExit()\n");
}

void ServoUnityWindowGL::pointerOver(int x, int y) {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerOver(%d, %d)\n", x, y);
    mouse_move(x, y);
}

void ServoUnityWindowGL::pointerPress(int x, int y) {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerPress(%d, %d)\n", x, y);
    mouse_down(x, y, CMouseButton::Left);
}

void ServoUnityWindowGL::pointerRelease(int x, int y) {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerRelease(%d, %d)\n", x, y);
    mouse_up(x, y, CMouseButton::Left);
}

void ServoUnityWindowGL::pointerScrollDiscrete(int x, int y) {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerScrollDiscrete(%d, %d)\n", x, y);
    scroll(x, y, 0, 0);
}

void ServoUnityWindowGL::keyPress(int charCode) {
	SERVOUNITYLOGi("ServoUnityWindowGL::keyPress(%d)\n", charCode);
}

#endif // SUPPORT_OPENGL_CORE
