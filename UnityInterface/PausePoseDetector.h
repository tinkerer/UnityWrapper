/*****************************************************************************
*                                                                            *
*  Unity Wrapper                                                             *
*  Copyright (C) 2010 PrimeSense Ltd.                                        *
*                                                                            *
*  This file is part of OpenNI.                                              *
*                                                                            *
*  OpenNI is free software: you can redistribute it and/or modify            *
*  it under the terms of the GNU Lesser General Public License as published  *
*  by the Free Software Foundation, either version 3 of the License, or      *
*  (at your option) any later version.                                       *
*                                                                            *
*  OpenNI is distributed in the hope that it will be useful,                 *
*  but WITHOUT ANY WARRANTY; without even the implied warranty of            *
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the              *
*  GNU Lesser General Public License for more details.                       *
*                                                                            *
*  You should have received a copy of the GNU Lesser General Public License  *
*  along with OpenNI. If not, see <http://www.gnu.org/licenses/>.            *
*                                                                            *
*****************************************************************************/
//Author: Shlomo Zippel

#pragma once

//-----------------------------------------------------------------------------
// Headers
//-----------------------------------------------------------------------------

#include <XnOpenNI.h>
#include <XnCppWrapper.h>
#include "SkeletonPoseDetector.h"

//-----------------------------------------------------------------------------
// PauseGesturePoseDetector
//-----------------------------------------------------------------------------

const double PAUSE_DETECTOR_SECONDS	= 2.0;

class PauseGesturePoseDetector : public SkeletonPoseDetectorBase
{
public:
	PauseGesturePoseDetector(xn::UserGenerator &userGenerator, XnUserID userId, double timeInPose)
		: SkeletonPoseDetectorBase(userGenerator, userId, timeInPose) {}
	PauseGesturePoseDetector(xn::UserGenerator &userGenerator, XnUserID userId)
		: SkeletonPoseDetectorBase(userGenerator, userId, PAUSE_DETECTOR_SECONDS) {}

	virtual bool IsInPose();
	virtual void Reset();
};
