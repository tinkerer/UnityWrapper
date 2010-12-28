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


//-----------------------------------------------------------------------------
// Interface types
//-----------------------------------------------------------------------------

#ifdef UNITYINTERFACE_EXPORTS
#define UNITYINTERFACE_API extern "C" __declspec(dllexport)
#else
#define UNITYINTERFACE_API extern "C" __declspec(dllimport)
#endif

typedef void (__stdcall * pfn_user_callback)(int nUserId);
typedef void (__stdcall * pfn_focus_callback)(bool focused);
typedef void (__stdcall * pfn_handpoint_callback)(float x, float y, float z);
typedef void (__stdcall * pfn_item_callback)(int item_index, int direction);
typedef void (__stdcall * pfn_value_callback)(float value);

//-----------------------------------------------------------------------------
// UnityInterface API
//-----------------------------------------------------------------------------

// Init update & shutdown
UNITYINTERFACE_API XnStatus Init(const char * strXmlPath);
UNITYINTERFACE_API void	Update(bool async);
UNITYINTERFACE_API void Shutdown();

// Getters - only call after Init
UNITYINTERFACE_API const char *	GetStatusString(const XnStatus rc);
UNITYINTERFACE_API unsigned int	GetDepthWidth();
UNITYINTERFACE_API unsigned int	GetDepthHeight();
UNITYINTERFACE_API const XnLabel * GetUsersLabelMap();
UNITYINTERFACE_API const XnDepthPixel * GetUsersDepthMap();

// User tracking
UNITYINTERFACE_API void	StartLookingForUsers(pfn_user_callback pNewUser, pfn_user_callback pCalibrationStarted, pfn_user_callback pCalibrationFailed, pfn_user_callback pCalibrationSuccess, pfn_user_callback pUserLost);
UNITYINTERFACE_API void	StopLookingForUsers();
UNITYINTERFACE_API void	LoseUsers();
UNITYINTERFACE_API bool	GetUserCenterOfMass(XnUserID userID, OUT XnVector3D * pCom);
UNITYINTERFACE_API float GetUserPausePoseProgress(XnUserID userID);

// Skeleton stuff
UNITYINTERFACE_API void SetSkeletonSmoothing(double f);
UNITYINTERFACE_API bool GetJointTransformation(XnUserID userID, XnSkeletonJoint joint, OUT XnSkeletonJointTransformation* pTransformation);
UNITYINTERFACE_API bool GetJointPosition(XnUserID userID, XnSkeletonJoint joint, OUT XnSkeletonJointPosition* pPosition);
UNITYINTERFACE_API bool GetJointOrientation(XnUserID userID, XnSkeletonJoint joint, OUT XnSkeletonJointOrientation* pOrientation);

// UI stuff
UNITYINTERFACE_API void StartUIMode(pfn_focus_callback pFocus, pfn_handpoint_callback pHandPoint, bool bUseFocusGesture);
UNITYINTERFACE_API void ForceUISession(float x, float y, float z);
UNITYINTERFACE_API void StopUIMode();
UNITYINTERFACE_API void StartScrollingMenu(bool horiz, int items, float scrollsize, pfn_item_callback selected_cb, pfn_item_callback highlighted_cb, pfn_value_callback value_cb, pfn_value_callback scroll_cb);
UNITYINTERFACE_API void StopMenu();