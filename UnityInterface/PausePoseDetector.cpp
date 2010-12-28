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

#include "PausePoseDetector.h"
#include <math.h>

//-----------------------------------------------------------------------------
// PauseGesturePoseDetector implementation
//-----------------------------------------------------------------------------

bool PauseGesturePoseDetector::IsInPose()
{
	XnSkeletonJointTransformation leftHand;
	XnSkeletonJointTransformation rightHand;
	xn::SkeletonCapability skeletonCap = m_userGenerator.GetSkeletonCap();

	// is this user even being tracked?
	if (!skeletonCap.IsTracking(m_userId))
	{
		return false;
	}

	// get hands & head
	skeletonCap.GetSkeletonJoint(m_userId, XnSkeletonJoint::XN_SKEL_LEFT_HAND, leftHand);
	skeletonCap.GetSkeletonJoint(m_userId, XnSkeletonJoint::XN_SKEL_RIGHT_HAND, rightHand);

	// make sure the points have enough confidence
	if ((leftHand.position.fConfidence  < 0.5) || 
		(rightHand.position.fConfidence < 0.5))
	{
		return false;
	}

	// make sure hands are reversed & same height
	float xDist = leftHand.position.position.X - rightHand.position.position.X;
	float yDist = fabs(leftHand.position.position.Y - rightHand.position.position.Y);
	if ((xDist < 60 ) ||
		(yDist > 300))
	{
		return false;
	}

	return true;
}

void PauseGesturePoseDetector::Reset()
{
	SkeletonPoseDetectorBase::Reset();
}