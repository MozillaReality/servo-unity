// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019-2020 Mozilla.
//
// Author(s): Philip Lamb

//
// ServoUnityUtilityFunctions.cs
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0.If a copy of the MPL was not distributed with this
// file, You can obtain one at https ://mozilla.org/MPL/2.0/.
//
// Copyright (c) 2019- Mozilla, Inc.
//
// Author(s): Philip Lamb
//
// Declarations of plugin interfaces which are exported from the native DLL.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class ServoUnityUtilityFunctions
{
	/// <summary>
	/// Creates a Unity matrix from an array of floats.
	/// </summary>
	/// <param name="values">Array of 16 floats to populate the matrix.</param>
	/// <returns>A new <see cref="Matrix4x4"/> with the given values.</returns>
	public static Matrix4x4 MatrixFromFloatArray(float[] values)
	{
	    if (values == null || values.Length < 16) throw new ArgumentException("Expected 16 elements in values array", "values");

	    Matrix4x4 mat = new Matrix4x4();
	    for (int i = 0; i < 16; i++) mat[i] = values[i];
	    return mat;
	}

#if false
	// Posted on: http://answers.unity3d.com/questions/11363/converting-matrix4x4-to-quaternion-vector3.html
	public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
	{
	    // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
	    Quaternion q = new Quaternion();
	    q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
	    q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
	    q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
	    q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
	    q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
	    q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
	    q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
	    return q;
	}
#else
	public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
	{
		// Trap the case where the matrix passed in has an invalid rotation submatrix.
		if (m.GetColumn(2) == Vector4.zero) {
			Debug.LogError("QuaternionFromMatrix got zero matrix.");
			return Quaternion.identity;
		}
		return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
	}
#endif

	public static Vector3 PositionFromMatrix(Matrix4x4 m)
	{
	    return m.GetColumn(3);
	}

	// Convert from right-hand coordinate system with <normal vector> in direction of +x,
	// <orthorgonal vector> in direction of +y, and <approach vector> in direction of +z,
	// to Unity's left-hand coordinate system with <normal vector> in direction of +x,
	// <orthorgonal vector> in direction of +y, and <approach vector> in direction of +z.
	// This is equivalent to negating row 2, and then negating column 2.
	public static Matrix4x4 LHMatrixFromRHMatrix(Matrix4x4 rhm)
	{
		Matrix4x4 lhm = new Matrix4x4();;

		// Column 0.
		lhm[0, 0] =  rhm[0, 0];
		lhm[1, 0] =  rhm[1, 0];
		lhm[2, 0] = -rhm[2, 0];
		lhm[3, 0] =  rhm[3, 0];
		
		// Column 1.
		lhm[0, 1] =  rhm[0, 1];
		lhm[1, 1] =  rhm[1, 1];
		lhm[2, 1] = -rhm[2, 1];
		lhm[3, 1] =  rhm[3, 1];
		
		// Column 2.
		lhm[0, 2] = -rhm[0, 2];
		lhm[1, 2] = -rhm[1, 2];
		lhm[2, 2] =  rhm[2, 2];
		lhm[3, 2] = -rhm[3, 2];
		
		// Column 3.
		lhm[0, 3] =  rhm[0, 3];
		lhm[1, 3] =  rhm[1, 3];
		lhm[2, 3] = -rhm[2, 3];
		lhm[3, 3] =  rhm[3, 3];

		return lhm;
	}

}