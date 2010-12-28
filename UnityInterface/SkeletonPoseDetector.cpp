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

//-----------------------------------------------------------------------------
// Headers
//-----------------------------------------------------------------------------

#include "SkeletonPoseDetector.h"


//-----------------------------------------------------------------------------
// SkeletonPoseDetectorBase Implementation
//-----------------------------------------------------------------------------

void SkeletonPoseDetectorBase::Update()
{
	// are we in the pose now?
	if (IsInPose())
	{
		// take note of start time, if we haven't yet
		if (m_startTime == 0.0)
		{
			m_startTime = Now();
		}
	
		if (0.0 == m_timeInPose)
		{
			m_detectionProgress = 1.0;
		}
		else
		{
			m_detectionProgress = (Now() - m_startTime) / m_timeInPose;
			if (m_detectionProgress > 1.0) m_detectionProgress = 1.0;
		}
	}
	// not in pose
	else
	{
		// subtract some of our detection progress - this will smooth out
		// cases when we ocassionally miss a few frames but still in the pose
		if (m_detectionProgress > 0.04)
		{
			m_detectionProgress -= 0.04;
		}
		else
		{
			m_detectionProgress = 0.0;
			m_startTime = 0.0;
		}
	}
}

double SkeletonPoseDetectorBase::GetDetectionProgress()
{
	return m_detectionProgress;
}

void SkeletonPoseDetectorBase::Reset()
{
	m_startTime = 0.0;
	m_detectionProgress = 0.0;
}

double SkeletonPoseDetectorBase::Now()
{
	return GetTickCount() / 1000.0;
}