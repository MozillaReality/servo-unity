//
// servo_unity_log.h
//
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0.If a copy of the MPL was not distributed with this
// file, You can obtain one at https ://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla, Inc.
//
// Author(s): Philip Lamb
//

#ifndef __servo_unity_log_h__
#define __servo_unity_log_h__

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdarg.h>
#ifndef _WIN32 // errno is defined in stdlib.h on Windows.
#  ifdef EMSCRIPTEN // errno is not in sys/
#    include <errno.h>
#  else
#    include <sys/errno.h>
#  endif
#endif

#include "servo_unity_c.h"

#ifdef __cplusplus
extern "C" {
#endif

enum {
	SERVO_UNITY_LOG_LEVEL_DEBUG = 0,
	SERVO_UNITY_LOG_LEVEL_INFO,
	SERVO_UNITY_LOG_LEVEL_WARN,
	SERVO_UNITY_LOG_LEVEL_ERROR,
	SERVO_UNITY_LOG_LEVEL_REL_INFO
};
#define SERVO_UNITY_LOG_LEVEL_DEFAULT SERVO_UNITY_LOG_LEVEL_INFO

/*!
	@var int arLogLevel
	@brief   Sets the severity level. Log messages below the set severity level are not logged.
	@details
		All calls to artoolkitX's logging facility include a "log level" parameter, which specifies
		the severity of the log message. (The severities are defined in &lt;ARUtil/log.h&gt;.)
		Setting this global allows for filtering of log messages. All log messages lower than
		the set level will not be logged by arLog().
		Note that debug log messages created using the ARLOGd() macro will be logged only in
		debug builds, irrespective of the log level.
	@see arLog
*/
SERVO_UNITY_EXTERN extern int servoUnityLogLevel;

/*!
	@brief   Write a string to the current logging facility.
	@details
		The default logging facility varies by platform, but on Unix-like platforms is typically
		the standard error file descriptor. However, logging may be redirected to some other
		facility by arLogSetLogger.
		Newlines are not automatically appended to log output.
	@param      tag A tag to supply to an OS-specific logging function to specify the source
		of the error message. May be NULL, in which case "libAR" will be used.
	@param      logLevel The severity of the log message. Defined in %lt;ARUtil/log.h&gt;.
		Log output is written to the logging facility provided the logLevel meets or
		exceeds the minimum level specified in global arLogLevel.
	@param      format Log format string, in the form of printf().
	@see servoUnityLogLevel
	@see servoUnityLogSetLogger
*/

SERVO_UNITY_EXTERN void servoUnityLog(const char *tag, const int logLevel, const char *format, ...);
SERVO_UNITY_EXTERN void servoUnityLogv(const char *tag, const int logLevel, const char *format, va_list ap);

typedef void (SERVO_UNITY_CALLBACK *SERVO_UNITY_LOG_LOGGER_CALLBACK)(const char *logMessage);

/*!
	@brief   Divert logging to a callback, or revert to default logging.
	@details
		The default logging facility varies by platform, but on Unix-like platforms is typically
		the standard error file descriptor. However, logging may be redirected to some other
		facility by this function.
	@param      callback The function which will be called with the log output, or NULL to
		cancel redirection.
	@param      callBackOnlyIfOnSameThread If non-zero, then the callback will only be called
		if the call to arLog is made on the same thread as the thread which called this function,
		and if the arLog call is made on a different thread, log output will be buffered until
		the next call to arLog on the original thread.
		The purpose of this is to prevent logging from secondary threads in cases where the
		callback model of the target platform precludes this.
	@see servoUnityLog
*/
SERVO_UNITY_EXTERN void servoUnityLogSetLogger(SERVO_UNITY_LOG_LOGGER_CALLBACK callback, int callBackOnlyIfOnSameThread);

#ifndef NDEBUG
#  define SERVOUNITYLOGd(...) servoUnityLog(NULL, SERVO_UNITY_LOG_LEVEL_DEBUG, __VA_ARGS__)
#else
#  define SERVOUNITYLOGd(...)
#endif
#define SERVOUNITYLOGi(...) servoUnityLog(NULL, SERVO_UNITY_LOG_LEVEL_INFO, __VA_ARGS__)
#define SERVOUNITYLOGw(...) servoUnityLog(NULL, SERVO_UNITY_LOG_LEVEL_WARN, __VA_ARGS__)
#define SERVOUNITYLOGe(...) servoUnityLog(NULL, SERVO_UNITY_LOG_LEVEL_ERROR, __VA_ARGS__)
#define SERVOUNITYLOGperror(s) servoUnityLog(NULL, SERVO_UNITY_LOG_LEVEL_ERROR, ((s != NULL) ? "%s: %s\n" : "%s%s\n"), ((s != NULL) ? s : ""), strerror(errno))

#ifdef __cplusplus
}
#endif
#endif // !__servo_unity_log_h__
