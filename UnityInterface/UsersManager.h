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
#include "PausePoseDetector.h"

//-----------------------------------------------------------------------------
// UserContext
//-----------------------------------------------------------------------------

// we will keep a user context for every detected user
struct UserContext
{
	UserContext(xn::UserGenerator &userGenerator, XnUserID userId)
		: pauseGesturePoseDetector(userGenerator, userId) {}

	// pause pose detection related stuff
	PauseGesturePoseDetector pauseGesturePoseDetector;
};

//-----------------------------------------------------------------------------
// UsersManager
//-----------------------------------------------------------------------------

// The usersmanager simply manages the table of users, and updates whatever needs
// updating each frame for each user
class UsersManager
{
public:
	UsersManager(xn::UserGenerator &userGenerator)
		: m_UserGenerator(userGenerator)
	{
		m_UserGenerator.RegisterUserCallbacks(OnNewUser, OnLostUser, this, m_hUserCallbacks);
	}
	~UsersManager()
	{
		m_UserGenerator.UnregisterUserCallbacks(m_hUserCallbacks);
	}

	void Update()
	{
		for (UserContextTable::Iterator i = m_allUsers.begin(); i != m_allUsers.end(); i++)
		{
			i.Value()->pauseGesturePoseDetector.Update();
		}
	}

	UserContext * GetUserContext(XnUserID userId)
	{
		UserContext * pUser;
		if (XN_STATUS_OK != m_allUsers.Get(userId, pUser))
		{
			return NULL;
		}

		return pUser;
	}

protected:
	static void XN_CALLBACK_TYPE OnNewUser(xn::UserGenerator& generator, const XnUserID nUserId, void* pCookie)
	{
		UsersManager * This = (UsersManager *) pCookie;
		UserContext * newUser = new UserContext(This->m_UserGenerator, nUserId);
		This->m_allUsers.Set(nUserId, newUser);
	}

	static void XN_CALLBACK_TYPE OnLostUser(xn::UserGenerator& generator, const XnUserID nUserId, void* pCookie)
	{
		UsersManager * This = (UsersManager *) pCookie;
		UserContext * lostUser = NULL;
		This->m_allUsers.Remove(nUserId, lostUser);
		delete lostUser;
	}

	XnCallbackHandle m_hUserCallbacks;
	xn::UserGenerator& m_UserGenerator;

	// hash table mapping user id -> user context
	XN_DECLARE_DEFAULT_HASH(XnUserID, UserContext *, UserContextTable);
	UserContextTable m_allUsers;
};