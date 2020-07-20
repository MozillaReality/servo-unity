//
//  utils.c
//  servo_unity
//
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0.If a copy of the MPL was not distributed with this
// file, You can obtain one at https ://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla, Inc.
//
// Author(s): Philip Lamb
//
// Plugin utility functions
//

#include "utils.h"

#if defined(__APPLE__)
#  include <pthread.h>
#  define FN_THREADID pthread_threadid_np
#elif defined(__WIN32)
#  include <Processthreadsapi.h>
#elif defined(__linux__)
#  include <sys/types.h>
#endif

#ifdef _WIN32
#  include <windows.h>
#  include <sys/timeb.h>
#else
#  include <time.h>
#  include <sys/time.h>
#endif

uint64_t getThreadID()
{
    uint64_t tid = 0;
#if defined(__APPLE__)
    pthread_threadid_np(NULL, &tid);
#elif defined(__WIN32)
    tid = (uint64_t)GetCurrentThreadId(); // Cast from DWORD.
#elif defined(__linux__)
    tid = (uint64_t)gettid(); // Cast from pid_t.
#elif defined(__unix__) // Other BSD not elsewhere defined.
    tid = (uint64_t) = pthread_getthreadid_np(); // Cast from int.
#endif
    return tid;
}

utilTime getTimeNow(void)
{
    utilTime timeNow;
#ifdef _WIN32
    struct _timeb sys_time;

    _ftime_s(&sys_time);
    timeNow.secs = (long)sys_time.time;
    timeNow.millisecs = sys_time.millitm;
#else
    struct timeval time;

#  if defined(__linux) || defined(__APPLE__) || defined(EMSCRIPTEN)
    gettimeofday(&time, NULL);
#  else
    gettimeofday(&time);
#  endif
    timeNow.secs = time.tv_sec;
    timeNow.millisecs = time.tv_usec/1000;
#endif

    return timeNow;
}

unsigned long millisecondsElapsedSince(utilTime time)
{
    utilTime timeNow = getTimeNow();
    return ((timeNow.secs - time.secs)*1000 + (timeNow.millisecs - time.millisecs)); // The second addend can be negative.
}
