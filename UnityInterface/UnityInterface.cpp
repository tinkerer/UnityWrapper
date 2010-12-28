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

#include "UnityInterface.h"
#include <XnOpenNI.h>
#include <XnCppWrapper.h>
#include <XnVSessionManager.h>
#include <XnVPointControl.h>
#include <XnVSelectableSlider1D.h>
#include <XnVPointDenoiser.h>

#include "UsersManager.h"

//-----------------------------------------------------------------------------
// Globals
//-----------------------------------------------------------------------------

// OpenNI stuff
static xn::Context			g_Context;
static xn::DepthGenerator	g_DepthGenerator;
static xn::UserGenerator	g_UserGenerator;

static XnCallbackHandle		g_hUserCallbacks		= NULL;
static XnCallbackHandle		g_hCalibrationCallbacks = NULL;
static XnCallbackHandle		g_hPoseCallbacks		= NULL;

static unsigned int			g_nDepthWidth	= 0;
static unsigned int			g_nDepthHeight	= 0;

static bool					g_bSearchingForPlayers = false;

static UsersManager *		g_pUsersManager = NULL;

// UI mode stuff
static XnVSessionManager *		g_pSessionManager = NULL;
static XnVPointDenoiser *		g_pPointDenoiser = NULL;
static XnVPointControl			g_PointControl;
static XnVSelectableSlider1D*	g_pMainSlider = NULL;

// A dummy focus gesture - doesn't do anything
// We will use this as our focus gesture when the user will force a UI session
// at a specific location
class XnVDummyGesture : public XnVGesture
{
	virtual void Update(const xn::Context* pContext) {}
};
XnVDummyGesture * g_pDummyGesture = NULL;

//-----------------------------------------------------------------------------
// Callbacks
//-----------------------------------------------------------------------------

static pfn_user_callback g_pfnNewUser = NULL;
static pfn_user_callback g_pfnCalibrationStarted = NULL;
static pfn_user_callback g_pfnCalibrationSucceeded = NULL;
static pfn_user_callback g_pfnCalibrationFailed = NULL;
static pfn_user_callback g_pfnUserLost = NULL;

static pfn_focus_callback g_pfnFocused = NULL;
static pfn_handpoint_callback g_pfnHandPoint = NULL;
static pfn_item_callback g_pfnHighlighted = NULL;
static pfn_item_callback g_pfnSelected = NULL;
static pfn_value_callback g_pfnValueChanged = NULL;
static pfn_value_callback g_pfnScroll = NULL;

//-----------------------------------------------------------------------------
// Internal event handlers
//-----------------------------------------------------------------------------

// Point create/destroy callbacks
void XN_CALLBACK_TYPE OnPrimaryPointCreate(const XnVHandPointContext* hand, const XnPoint3D& ptFocus, void* cxt)
{
	if (NULL != g_pfnFocused)
	{
		g_pfnFocused(true);
	}
}
void XN_CALLBACK_TYPE OnPrimaryPointDestroy(XnUInt32 nID, void* cxt)
{
	if (NULL != g_pfnFocused)
	{
		g_pfnFocused(false);
	}
}

void XN_CALLBACK_TYPE OnPrimaryPointUpdate(const XnVHandPointContext* pContext, void* cxt)
{
	if (NULL != g_pfnHandPoint)
	{
		g_pfnHandPoint(pContext->ptPosition.X, pContext->ptPosition.Y, pContext->ptPosition.Z);
	}
}

static void XN_CALLBACK_TYPE OnNewUser(xn::UserGenerator& generator, const XnUserID nUserId, void* pCookie)
{
	if (g_bSearchingForPlayers)
	{
		if (NULL != g_pfnNewUser)
		{
			g_pfnNewUser(nUserId);
		}
		generator.GetPoseDetectionCap().StartPoseDetection("Psi", nUserId);
	}
}

static void XN_CALLBACK_TYPE OnLostUser(xn::UserGenerator& generator, const XnUserID nUserId, void* pCookie)
{	
	if (NULL != g_pfnUserLost)
	{
		g_pfnUserLost(nUserId);
	}
}

static void XN_CALLBACK_TYPE OnPoseDetected(xn::PoseDetectionCapability& poseDetection, const XnChar* strPose, XnUserID nId, void* pCookie)
{
	// Stop detecting the pose
	poseDetection.StopPoseDetection(nId);

	// Start calibrating if we are looking for players
	if (g_bSearchingForPlayers)
	{
		g_UserGenerator.GetSkeletonCap().RequestCalibration(nId, TRUE);
	}
}

static void XN_CALLBACK_TYPE OnCalibrationStart(xn::SkeletonCapability& skeleton, const XnUserID nUserId, void* pCookie)
{
	// callback
	if (NULL != g_pfnCalibrationStarted)
	{
		g_pfnCalibrationStarted(nUserId);
	}
}

static void XN_CALLBACK_TYPE OnCalibrationEnd(xn::SkeletonCapability& skeleton, const XnUserID nUserId, XnBool bSuccess, void* pCookie)
{
	// If this was a successful calibration
	if (bSuccess)
	{
		// start tracking
		skeleton.StartTracking(nUserId);

		// callback
		if (NULL != g_pfnCalibrationSucceeded)
		{
			g_pfnCalibrationSucceeded(nUserId);
		}
	}
	else
	{
		// failure callback
		if (NULL != g_pfnCalibrationFailed)
		{
			g_pfnCalibrationFailed(nUserId);
		}

		// Restart pose detection if still searching for players
		if (g_bSearchingForPlayers)
		{
			g_UserGenerator.GetPoseDetectionCap().StartPoseDetection("Psi", nUserId);
		}
	}	
}

// Main slider
void XN_CALLBACK_TYPE MainSlider_OnHover(XnInt32 nItem, void* cxt)
{
	// TODO: Unhighlight old one?

	if (NULL != g_pfnHighlighted)
	{
		g_pfnHighlighted(nItem, true);
	}
}

void XN_CALLBACK_TYPE MainSlider_OnSelect(XnInt32 nItem, XnVDirection dir, void* cxt)
{
	if (NULL != g_pfnSelected)
	{
		g_pfnSelected(nItem, dir);
	}
}

void XN_CALLBACK_TYPE MainSlider_OnValueChange(XnFloat fValue, void* cxt)
{
	if (NULL != g_pfnValueChanged)
	{
		g_pfnValueChanged(fValue);
	}
}

void XN_CALLBACK_TYPE MainSlider_OnScroll(XnFloat fScrollValue, void* pUserCxt)
{
	if (NULL != g_pfnScroll)
	{
		g_pfnScroll(fScrollValue);
	}
}


//-----------------------------------------------------------------------------
// UnityInterface API
//-----------------------------------------------------------------------------

XnStatus Init(const char * strXmlPath)
{
	XnStatus rc = XN_STATUS_OK;

	// Init NITE context
	rc = g_Context.InitFromXmlFile(strXmlPath);
	if (XN_STATUS_OK != rc) 
	{
		return rc;
	}

	// Save width & height of our depth generator (also ensures we will have one)
	
	XnMapOutputMode mapOutputMode;
	rc = g_Context.FindExistingNode(XN_NODE_TYPE_DEPTH, g_DepthGenerator);
	if (XN_STATUS_OK != rc)
	{
		Shutdown();
		return rc;
	}	
	rc = g_DepthGenerator.GetMapOutputMode(mapOutputMode);
	if (XN_STATUS_OK != rc)
	{
		Shutdown();
		return rc;
	}
	g_nDepthWidth = mapOutputMode.nXRes;
	g_nDepthHeight = mapOutputMode.nYRes;
	
	// Get our UserGenerator for later (also ensures we have one)
	rc = g_Context.FindExistingNode(XN_NODE_TYPE_USER, g_UserGenerator);
	if (XN_STATUS_OK != rc)
	{
		Shutdown();
		return rc;
	}
	g_UserGenerator.RegisterUserCallbacks(OnNewUser, OnLostUser, NULL, g_hUserCallbacks);
	g_UserGenerator.GetSkeletonCap().SetSkeletonProfile(XN_SKEL_PROFILE_ALL);
	g_UserGenerator.GetSkeletonCap().RegisterCalibrationCallbacks(&OnCalibrationStart, &OnCalibrationEnd, NULL, g_hCalibrationCallbacks);
	g_UserGenerator.GetSkeletonCap().SetSmoothing(0.5);
	g_UserGenerator.GetPoseDetectionCap().RegisterToPoseCallbacks(&OnPoseDetected, NULL, NULL, g_hPoseCallbacks);

	// users manager
	g_pUsersManager = new UsersManager(g_UserGenerator);

	// Setup a blank point control for the create/destroy events
	// We will use those for focus events
	g_PointControl.RegisterPrimaryPointCreate(NULL, &OnPrimaryPointCreate);
	g_PointControl.RegisterPrimaryPointDestroy(NULL, &OnPrimaryPointDestroy);
	g_PointControl.RegisterPrimaryPointUpdate(NULL, &OnPrimaryPointUpdate);

	// The point denoiser will smoothen out our hand points
	g_pPointDenoiser = new XnVPointDenoiser();

	g_pDummyGesture = new XnVDummyGesture();

	rc = g_Context.StartGeneratingAll();
	if (XN_STATUS_OK != rc)
	{
		Shutdown();
		return rc;
	}

	return XN_STATUS_OK;
}

void Update(bool async)
{
	// update openni
	if (async)
	{
		g_Context.WaitNoneUpdateAll();
	}
	else
	{
		g_Context.WaitAndUpdateAll();
	}

	// update users manager
	g_pUsersManager->Update();

	// if in UI mode - update ui
	if (NULL != g_pSessionManager)
	{
		g_pSessionManager->Update(&g_Context);
	}
}

void Shutdown()
{
	// Stop generating events
	g_Context.StopGeneratingAll();

	// Unregister any callbacks we may have
	if (NULL != g_hPoseCallbacks)
	{
		g_UserGenerator.GetPoseDetectionCap().UnregisterFromPoseCallbacks(g_hPoseCallbacks);
		g_hPoseCallbacks = NULL;
	}

	if (NULL != g_hCalibrationCallbacks)
	{
		g_UserGenerator.GetSkeletonCap().UnregisterCalibrationCallbacks(g_hCalibrationCallbacks);
		g_hCalibrationCallbacks = NULL;
	}

	if (NULL != g_hUserCallbacks)
	{
		g_UserGenerator.GetSkeletonCap().UnregisterCalibrationCallbacks(g_hUserCallbacks);
		g_hUserCallbacks = NULL;
	}

	// user manager
	delete g_pUsersManager;
	g_pUsersManager = NULL;

	// UI
	if (NULL != g_pSessionManager)
	{
		delete g_pSessionManager;
		g_pSessionManager = NULL;
	}

	if (NULL != g_pDummyGesture)
	{
		delete g_pDummyGesture;
		g_pDummyGesture = NULL;
	}

	// Bye bye open ni
	g_Context.Shutdown();
}

const char * GetStatusString(const XnStatus rc)
{
	return xnGetStatusString(rc);
}

unsigned int GetDepthWidth()
{
	return g_nDepthWidth;	
}

unsigned int GetDepthHeight()
{
	return g_nDepthHeight;
}

void StartLookingForUsers(pfn_user_callback pNewUser, pfn_user_callback pCalibrationStarted, pfn_user_callback pCalibrationFailed, pfn_user_callback pCalibrationSuccess, pfn_user_callback pUserLost)
{
	// save our callbacks for later
	g_pfnNewUser = pNewUser;
	g_pfnCalibrationStarted = pCalibrationStarted;
	g_pfnCalibrationFailed = pCalibrationFailed;
	g_pfnCalibrationSucceeded = pCalibrationSuccess;
	g_pfnUserLost = pUserLost;

	// start pose detection for all existing non-calibrated users
	XnUInt16 nUsers = g_UserGenerator.GetNumberOfUsers();
	XnUserID * pUsers = new XnUserID[nUsers];
	g_UserGenerator.GetUsers(pUsers, nUsers);
	for (int i = 0; i < nUsers; i++)
	{
		if (!g_UserGenerator.GetSkeletonCap().IsCalibrated(pUsers[i]) &&
			!g_UserGenerator.GetSkeletonCap().IsCalibrating(pUsers[i]))
		{
			g_UserGenerator.GetPoseDetectionCap().StartPoseDetection("Psi", pUsers[i]);
		}
	}
	delete [] pUsers;

	// make sure we try to calibrate future players
	g_bSearchingForPlayers = true;
}

void StopLookingForUsers()
{
	g_bSearchingForPlayers = false;
}

void LoseUsers()
{
	// stop tracking all users
	XnUInt16 nUsers = g_UserGenerator.GetNumberOfUsers();
	XnUserID * pUsers = new XnUserID[nUsers];
	g_UserGenerator.GetUsers(pUsers, nUsers);
	for (int i = 0; i < nUsers; i++)
	{
		if (g_UserGenerator.GetSkeletonCap().IsCalibrating(pUsers[i]))
		{
			g_UserGenerator.GetSkeletonCap().Reset(pUsers[i]);
			if (NULL != g_pfnCalibrationFailed)
			{
				g_pfnCalibrationFailed(pUsers[i]);
			}
		}

		if (g_UserGenerator.GetSkeletonCap().IsCalibrated(pUsers[i]))
		{
			g_UserGenerator.GetSkeletonCap().Reset(pUsers[i]);
			if (NULL != g_pfnUserLost)
			{
				g_pfnUserLost(pUsers[i]);
			}
		}
	}
	delete [] pUsers;
}

bool GetUserCenterOfMass(XnUserID userID, OUT XnVector3D * pCom)
{
	return (XN_STATUS_OK == g_UserGenerator.GetCoM(userID, *pCom));
}

float GetUserPausePoseProgress(XnUserID userID)
{
	UserContext * pUser = g_pUsersManager->GetUserContext(userID);
	if (NULL == pUser) return 0.0f;

	return (float)pUser->pauseGesturePoseDetector.GetDetectionProgress();
}

const XnLabel * GetUsersLabelMap()
{
	xn::SceneMetaData smd;
	g_UserGenerator.GetUserPixels(0, smd);

	return smd.Data();
}

const XnDepthPixel * GetUsersDepthMap()
{
	return g_DepthGenerator.GetDepthMap();
}

void SetSkeletonSmoothing(double f)
{
	g_UserGenerator.GetSkeletonCap().SetSmoothing(f);
}

bool GetJointTransformation(XnUserID userID, XnSkeletonJoint joint, OUT XnSkeletonJointTransformation* pTransformation)
{
	xn::SkeletonCapability skelCap = g_UserGenerator.GetSkeletonCap();
	
	if(userID == 0 || !skelCap.IsTracking(userID))
	{
		return false;
	}

	skelCap.GetSkeletonJoint(userID, joint, *pTransformation);

	return true;
}

bool GetJointPosition(XnUserID userID, XnSkeletonJoint joint, OUT XnSkeletonJointPosition* pPosition)
{
	xn::SkeletonCapability skelCap = g_UserGenerator.GetSkeletonCap();

	if(userID == 0 || !skelCap.IsTracking(userID))
	{
		return false;
	}

	skelCap.GetSkeletonJointPosition(userID, joint, *pPosition);

	return true;
}

bool GetJointOrientation(XnUserID userID, XnSkeletonJoint joint, OUT XnSkeletonJointOrientation* pOrientation)
{
	xn::SkeletonCapability skelCap = g_UserGenerator.GetSkeletonCap();

	if(userID == 0 || !skelCap.IsTracking(userID))
	{
		return false;
	}

	skelCap.GetSkeletonJointOrientation(userID, joint, *pOrientation);

	return true;
}

void StartUIMode(pfn_focus_callback pFocus, pfn_handpoint_callback pHandPoint, bool bUseFocusGesture)
{
	// cleanup previous UI mode
	if (NULL != g_pSessionManager)
	{
		StopUIMode();
	}

	// save our callbacks
	g_pfnFocused = pFocus;
	g_pfnHandPoint = pHandPoint;

	// initialize the point tracker
	g_pSessionManager = new XnVSessionManager();
	g_pSessionManager->Initialize(&g_Context, "Click", "RaiseHand");
	if (!bUseFocusGesture)
	{
		g_pSessionManager->SetGesture(g_pDummyGesture);
		g_pSessionManager->SetQRGesture(g_pDummyGesture);
		g_pSessionManager->SetQuickRefocusTimeout(0);
	}
	g_pSessionManager->AddListener(g_pPointDenoiser);
	g_pPointDenoiser->AddListener(&g_PointControl);
}

void ForceUISession(float x, float y, float z)
{
	XnPoint3D ptFocus;
	ptFocus.X = x;
	ptFocus.Y = y;
	ptFocus.Z = z;
	g_pSessionManager->ForceSession(ptFocus);
}

void StopUIMode()
{
	g_pfnHandPoint = NULL;
	g_pfnFocused = NULL;

	if (NULL != g_pSessionManager)
	{
		delete g_pSessionManager;
		g_pSessionManager = NULL;
	}
}

void StartScrollingMenu(bool horiz, int items, float scrollsize, pfn_item_callback selected_cb, pfn_item_callback highlighted_cb, pfn_value_callback value_cb, pfn_value_callback scroll_cb)
{
	// dont do anything if not in UI mode
	if (NULL == g_pSessionManager)
	{
		return;
	}

	// cleanup any existing menu
	if (NULL != g_pMainSlider)
	{
		g_pSessionManager->RemoveListener(g_pMainSlider);
		delete g_pMainSlider;
		g_pMainSlider = NULL;
	}

	// save callbacks
	g_pfnHighlighted = highlighted_cb;
	g_pfnSelected = selected_cb;
	g_pfnValueChanged = value_cb;
	g_pfnScroll = scroll_cb;

	// create our new slider
	g_pMainSlider = new XnVSelectableSlider1D(items, scrollsize, (horiz) ? AXIS_X : AXIS_Y);
	g_pMainSlider->RegisterItemHover(NULL, &MainSlider_OnHover);
	g_pMainSlider->RegisterItemSelect(NULL, &MainSlider_OnSelect);
	g_pMainSlider->RegisterValueChange(NULL, &MainSlider_OnValueChange);
	g_pMainSlider->RegisterScroll(NULL, &MainSlider_OnScroll);

	// recenter around focus point
	XnPoint3D ptCenter;
	g_pSessionManager->GetFocusPoint(ptCenter);
	g_pMainSlider->Reposition(ptCenter);

	// add it to our session manager

	g_pPointDenoiser->AddListener(g_pMainSlider);
}

void StopMenu()
{
	// cleanup any existing menu
	if (NULL != g_pMainSlider)
	{
		g_pPointDenoiser->RemoveListener(g_pMainSlider);
		delete g_pMainSlider;
		g_pMainSlider = NULL;
	}

	g_pfnHighlighted = NULL;
	g_pfnSelected = NULL;
	g_pfnValueChanged = NULL;
	g_pfnScroll = NULL;
}