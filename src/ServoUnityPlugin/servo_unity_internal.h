//
// servo_unity_internal.h
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0.If a copy of the MPL was not distributed with this
// file, You can obtain one at https ://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla, Inc.
//
// Author(s): Philip Lamb
//
// Plugin-wide C++ internals
//


#pragma once
#include <string>
#include "servo_unity_c.h"

// --------------------------------------------------------------------------
//  Configuration parameters

extern bool s_param_CloseNativeWindowOnClose;
extern std::string s_param_SearchURI;
extern std::string s_param_Homepage;

// --------------------------------------------------------------------------
//  Other internal globals

#ifdef _WIN32
extern "C" SERVO_UNITY_EXTERN int NvOptimusEnablement;
extern "C" SERVO_UNITY_EXTERN int AmdPowerXpressRequestHighPerformance;
#endif
