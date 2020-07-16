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
