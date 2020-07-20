//
//  utils.h
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

#ifndef utils_h
#define utils_h

#include <stdint.h>
#include <inttypes.h>

#ifdef __cplusplus
extern "C" {
#endif

// Can be printed e.g. with printf("thread ID is %" PRIu64 "\n");
extern uint64_t getThreadID(void);

typedef struct {
    long secs;
    int  millisecs;
} utilTime;

extern utilTime getTimeNow(void);
extern unsigned long millisecondsElapsedSince(utilTime time);

#ifdef __cplusplus
}
#endif
#endif // !utils_h
