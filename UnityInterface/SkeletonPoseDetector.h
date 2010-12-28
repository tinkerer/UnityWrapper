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

//-----------------------------------------------------------------------------
// SkeletonPoseDetectorBase
//-----------------------------------------------------------------------------

class SkeletonPoseDetectorBase
{
public:
	SkeletonPoseDetectorBase(xn::UserGenerator &userGenerator, XnUserID userId, double timeInPose)
		: m_userGenerator(userGenerator), m_userId(userId), m_startTime(0.0), m_timeInPose(timeInPose), m_detectionProgress(0.0) {}

	void Update();
	double GetDetectionProgress();

	virtual bool IsInPose() = 0;
	virtual void Reset();

protected:
	xn::UserGenerator &m_userGenerator;
	XnUserID m_userId;
	double m_startTime;
	double m_timeInPose;
	double m_detectionProgress;

private:
	double Now();
};