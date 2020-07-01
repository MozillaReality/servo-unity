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

void ServoUnityWindowGL::initDevice() {
#ifdef _WIN32
	gl3wInit();
#endif
}

void ServoUnityWindowGL::finalizeDevice() {
}

// Unfortunately the simpleservo interface doesn't allow arbitrary userdata
// to be passed along with callbacks, so we have to keep a global static
// instance pointer so that we can correctly call back to the correct window
// instance.
ServoUnityWindowGL *ServoUnityWindowGL::s_servo = nullptr;

ServoUnityWindowGL::ServoUnityWindowGL(int uid, int uidExt, Size size) :
	ServoUnityWindow(uid, uidExt),
	m_size(size),
	m_texID(0),
	m_buf(NULL),
	m_format(ServoUnityTextureFormat_BGRA32),
	m_pixelIntFormatGL(0),
	m_pixelFormatGL(0),
	m_pixelTypeGL(0),
	m_pixelSize(0),
    m_windowCreatedCallback(nullptr),
    m_windowResizedCallback(nullptr),
    m_browserEventCallback(nullptr),
    m_servoGLInited(false)
{
}

ServoUnityWindowGL::~ServoUnityWindowGL() {
	if (m_buf) {
		free(m_buf);
		m_buf = NULL;
	}
}

bool ServoUnityWindowGL::init(PFN_WINDOWCREATEDCALLBACK windowCreatedCallback, PFN_WINDOWRESIZEDCALLBACK windowResizedCallback, PFN_BROWSEREVENTCALLBACK browserEventCallback)
{
    m_windowCreatedCallback = windowCreatedCallback;
    m_windowResizedCallback = windowResizedCallback;
    m_browserEventCallback = browserEventCallback;
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

    m_buf = (uint8_t *)calloc(1, m_size.w * m_size.h * m_pixelSize);

	if (m_windowCreatedCallback) (*m_windowCreatedCallback)(m_uidExt, m_uid, m_size.w, m_size.h, m_format);

	return true;
}

ServoUnityWindow::Size ServoUnityWindowGL::size() {
	return m_size;
}

void ServoUnityWindowGL::setSize(ServoUnityWindow::Size size) {
	m_size = size;
	if (m_buf) free(m_buf);
	m_buf = (uint8_t *)calloc(1, m_size.w * m_size.h * m_pixelSize);

    if (m_windowResizedCallback) (*m_windowResizedCallback)(m_uidExt, m_size.w, m_size.h);
}

void ServoUnityWindowGL::setNativePtr(void* texPtr) {
	m_texID = (uint32_t)((uintptr_t)texPtr); // Truncation to 32-bits is the desired behaviour.
}

void* ServoUnityWindowGL::nativePtr() {
	return (void *)((uintptr_t)m_texID); // Extension to pointer-length (usually 64 bits) is the desired behaviour.
}

void ServoUnityWindowGL::requestUpdate(float timeDelta) {
    SERVOUNITYLOGd("ServoUnityWindowGL::requestUpdate(%f)\n", timeDelta);

    if (!m_servoGLInited) {
        if (s_servo) {
            SERVOUNITYLOGe("servo already inited.\n");
            return;
        }
        SERVOUNITYLOGi("initing servo.\n");
        s_servo = this;
        // Note about logs:
        // By default: all modules are enabled. Only warn level-logs are displayed.
        // To change the log level, add e.g. "--vslogger-level debug" to .args.
        // To only print logs from specific modules, add their names to pfilters.
        // For example:
        // static char *pfilters[] = {
        //   "servo",
        //   "simpleservo",
        //   "script::dom::bindings::error", // Show JS errors by default.
        //   "canvas::webgl_thread", // Show GL errors by default.
        //   "compositing",
        //   "constellation",
        // };
        // .vslogger_mod_list = pfilters;
        // .vslogger_mod_size = sizeof(pfilters) / sizeof(pfilters[0]);
        char *args = nullptr;
        const char *arg_ll = nullptr;
        const char *arg_ll_debug = "debug";
        const char *arg_ll_info = "info";
        const char *arg_ll_warn = "warn";
        const char *arg_ll_error = "error";
        switch (servoUnityLogLevel) {
            case SERVO_UNITY_LOG_LEVEL_DEBUG: arg_ll = arg_ll_debug; break;
            case SERVO_UNITY_LOG_LEVEL_INFO: arg_ll = arg_ll_info; break;
            case SERVO_UNITY_LOG_LEVEL_WARN: arg_ll = arg_ll_warn; break;
            case SERVO_UNITY_LOG_LEVEL_ERROR: arg_ll = arg_ll_error; break;
            default: break;
        }
        if (arg_ll) asprintf(&args, "--vslogger-level %s", arg_ll);

        CInitOptions cio {
            .args = args,
            .width = m_size.w,
            .height = m_size.h,
            .density = 1.0f,
            .enable_subpixel_text_antialiasing = true,
            .vslogger_mod_list = nullptr,
            .vslogger_mod_size = 0,
            .native_widget = nullptr
        };
        CHostCallbacks chc {
            .on_load_started = on_load_started,
            .on_load_ended = on_load_ended,
            .on_title_changed = on_title_changed,
            .on_allow_navigation = on_allow_navigation,
            .on_url_changed = on_url_changed,
            .on_history_changed = on_history_changed,
            .on_animating_changed = on_animating_changed,
            .on_shutdown_complete = on_shutdown_complete,
            .on_ime_state_changed = on_ime_state_changed,
            .get_clipboard_contents = get_clipboard_contents,
            .set_clipboard_contents = set_clipboard_contents,
            .on_media_session_metadata = on_media_session_metadata,
            .on_media_session_playback_state_change = on_media_session_playback_state_change,
            .on_media_session_set_position_state = on_media_session_set_position_state,
            .prompt_alert = prompt_alert,
            .prompt_ok_cancel = prompt_ok_cancel,
            .prompt_yes_no = prompt_yes_no,
            .prompt_input = prompt_input,
            .on_devtools_started = on_devtools_started,
            .show_context_menu = show_context_menu,
            .on_log_output = on_log_output
        };
        init_with_gl(cio, wakeup, chc);
        free(args);

        m_servoGLInited = true;
    }

    // Service task queue.
    while (true) {
        std::function<void()> task;
        {
            std::lock_guard<std::mutex> lock(m_tasksLock);
            if (m_tasks.empty()) {
                break;
            } else {
                task = m_tasks.front();
                m_tasks.pop_front();
            }
        }
        task();
    };

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

void ServoUnityWindowGL::runOnServoThread(std::function<void()> task) {
    std::lock_guard<std::mutex> lock(m_tasksLock);
    m_tasks.push_back(task);
}

void ServoUnityWindowGL::pointerEnter() {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerEnter()\n");
}

void ServoUnityWindowGL::pointerExit() {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerExit()\n");
}

void ServoUnityWindowGL::pointerOver(int x, int y) {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerOver(%d, %d)\n", x, y);
    if (!m_servoGLInited) return;

    runOnServoThread([=] {mouse_move(x, y);});
}

void ServoUnityWindowGL::pointerPress(int x, int y) {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerPress(%d, %d)\n", x, y);
    if (!m_servoGLInited) return;
    runOnServoThread([=] {mouse_down(x, y, CMouseButton::Left);});
}

void ServoUnityWindowGL::pointerRelease(int x, int y) {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerRelease(%d, %d)\n", x, y);
    if (!m_servoGLInited) return;
    runOnServoThread([=] {mouse_up(x, y, CMouseButton::Left);});
}

void ServoUnityWindowGL::pointerScrollDiscrete(int x, int y) {
	SERVOUNITYLOGi("ServoUnityWindowGL::pointerScrollDiscrete(%d, %d)\n", x, y);
    if (!m_servoGLInited) return;
    runOnServoThread([=] {scroll(x, y, 0, 0);});
}

void ServoUnityWindowGL::keyPress(int charCode) {
	SERVOUNITYLOGi("ServoUnityWindowGL::keyPress(%d)\n", charCode);
}

//
// Callback implementations. These are static, so have to fetch the active instance
// via the static instance pointer s_servo.
//

void ServoUnityWindowGL::on_load_started(void)
{
    SERVOUNITYLOGi("servo callback on_load_started\n");
    if (!s_servo || !s_servo->m_browserEventCallback) return;
    (*s_servo->m_browserEventCallback)(s_servo->uidExt(), ServoUnityBrowserEvent_LoadStateChanged, 1, 0);
}

void ServoUnityWindowGL::on_load_ended(void)
{
    SERVOUNITYLOGi("servo callback on_load_ended\n");
    if (!s_servo || !s_servo->m_browserEventCallback) return;
    (*s_servo->m_browserEventCallback)(s_servo->uidExt(), ServoUnityBrowserEvent_LoadStateChanged, 0, 0);
}

void ServoUnityWindowGL::on_title_changed(const char *title)
{
    SERVOUNITYLOGi("servo callback on_title_changed: %s\n", title);
}

bool ServoUnityWindowGL::on_allow_navigation(const char *url)
{
    SERVOUNITYLOGi("servo callback on_allow_navigation: %s\n", url);
    return true;
}

void ServoUnityWindowGL::on_url_changed(const char *url)
{
    SERVOUNITYLOGi("servo callback on_url_changed: %s\n", url);
}

void ServoUnityWindowGL::on_history_changed(bool can_go_back, bool can_go_forward)
{
    SERVOUNITYLOGi("servo callback on_history_changed: can_go_back:%s, can_go_forward:%s\n", can_go_back ? "true" : "false", can_go_forward ? "true" : "false");
}

void ServoUnityWindowGL::on_animating_changed(bool animating)
{
    SERVOUNITYLOGi("servo callback on_animating_changed(%s)\n", animating ? "true" : "false");
    // TODO: change the rate of callbacks
}

void ServoUnityWindowGL::on_shutdown_complete(void)
{
    SERVOUNITYLOGi("servo callback on_shutdown_complete\n");
    if (!s_servo || !s_servo->m_browserEventCallback) return;
    (*s_servo->m_browserEventCallback)(s_servo->uidExt(), ServoUnityBrowserEvent_Shutdown, 0, 0);
}

void ServoUnityWindowGL::on_ime_state_changed(bool show)
{
    SERVOUNITYLOGi("servo callback get_clipboard_contents\n");
    if (!s_servo || !s_servo->m_browserEventCallback) return;
    (*s_servo->m_browserEventCallback)(s_servo->uidExt(), ServoUnityBrowserEvent_IMEStateChanged, show ? 1 : 0, 0);
}

const char *ServoUnityWindowGL::get_clipboard_contents(void)
{
    SERVOUNITYLOGi("servo callback get_clipboard_contents\n");
    return nullptr;
}

void ServoUnityWindowGL::set_clipboard_contents(const char *contents)
{
    SERVOUNITYLOGi("servo callback set_clipboard_contents: %s\n", contents);
}

void ServoUnityWindowGL::on_media_session_metadata(const char *title, const char *album, const char *artist)
{
    SERVOUNITYLOGi("servo callback on_media_session_metadata: title:%s, album:%s, artist:%s\n", title, album, artist);
}

void ServoUnityWindowGL::on_media_session_playback_state_change(CMediaSessionPlaybackState state)
{
    const char *stateA;
    switch (state) {
        case CMediaSessionPlaybackState::None:
            stateA = "None";
            break;
        case CMediaSessionPlaybackState::Paused:
            stateA = "Paused";
            break;
        case CMediaSessionPlaybackState::Playing:
            stateA = "Playing";
            break;
        default:
            stateA = "";
            break;
    }
    SERVOUNITYLOGi("servo callback on_media_session_playback_state_change: %s\n", stateA);
}

void ServoUnityWindowGL::on_media_session_set_position_state(double duration, double position, double playback_rate)
{
    SERVOUNITYLOGi("servo callback on_media_session_set_position_state: duration:%f, position:%f, playback_rate:%f\n", duration, position, playback_rate);
}

void ServoUnityWindowGL::prompt_alert(const char *message, bool trusted)
{
    SERVOUNITYLOGi("servo callback prompt_alert%s: %s\n", trusted ? " (trusted)" : "", message);
}

CPromptResult ServoUnityWindowGL::prompt_ok_cancel(const char *message, bool trusted)
{
    SERVOUNITYLOGi("servo callback prompt_ok_cancel%s: %s\n", trusted ? " (trusted)" : "", message);
    return CPromptResult::Dismissed;
}

CPromptResult ServoUnityWindowGL::prompt_yes_no(const char *message, bool trusted)
{
    SERVOUNITYLOGi("servo callback prompt_yes_no%s: %s\n", trusted ? " (trusted)" : "", message);
    return CPromptResult::Dismissed;
}

const char *ServoUnityWindowGL::prompt_input(const char *message, const char *def, bool trusted)
{
    SERVOUNITYLOGi("servo callback prompt_input%s: %s\n", trusted ? " (trusted)" : "", message);
    return def;
}

void ServoUnityWindowGL::on_devtools_started(CDevtoolsServerState result, unsigned int port)
{
    const char *resultA;
    switch (result) {
        case CDevtoolsServerState::Error:
            resultA = "Error";
            break;
        case CDevtoolsServerState::Started:
            resultA = "Started";
            break;
        default:
            resultA = "";
            break;
    }
    SERVOUNITYLOGi("servo callback on_devtools_started: result:%s, port:%d\n", resultA, port);
}

void ServoUnityWindowGL::show_context_menu(const char *title, const char *const *items_list, uint32_t items_size)
{
    SERVOUNITYLOGi("servo callback show_context_menu: title:%s\n", title);
    for (int i = 0; i < items_size; i++) {
        SERVOUNITYLOGi("    item %n:%s\n", i, items_list[i]);
    }
    on_context_menu_closed(CContextMenuResult::Dismissed_, 0);
}

void ServoUnityWindowGL::on_log_output(const char *buffer, uint32_t buffer_length)
{
    SERVOUNITYLOGi("servo callback on_log_output: %s\n", buffer);
}

void ServoUnityWindowGL::wakeup(void)
{
    SERVOUNITYLOGi("servo callback wakeup\n");
    if (!s_servo) return;
    s_servo->runOnServoThread([]{perform_updates();});
}

#endif // SUPPORT_OPENGL_CORE
