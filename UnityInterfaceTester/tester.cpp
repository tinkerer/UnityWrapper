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
// Simple command line tester
// Make sure to set working directory to $(OutDir) 
// (Properties->Debugging->Working Directory)
//-----------------------------------------------------------------------------

#include "../UnityInterface/UnityInterface.h"
#include <conio.h>
#include <stdio.h>

void XN_CALLBACK_TYPE OnNewUser(int userId)
{
	printf("[%d] New user\n", userId);
}

void XN_CALLBACK_TYPE OnCalibrationStarted(int userId)
{
	printf("[%d] Calibration started\n", userId);
}

void XN_CALLBACK_TYPE OnCalibrationFailed(int userId)
{
	printf("[%d] Calibration failed\n", userId);
}

void XN_CALLBACK_TYPE OnCalibrationSuccess(int userId)
{
	printf("[%d] Calibration success\n", userId);
}

void XN_CALLBACK_TYPE OnUserLost(int userId)
{
	printf("[%d] User lost\n", userId);
}

int main()
{
	printf("Initing OpenNI...");
	XnStatus rc = Init("./OpenNI.xml");
	if (XN_STATUS_OK != rc)
	{
		printf("Error: %s\n", GetStatusString(rc));
		return 1;
	}
	printf("Success\n");

	StartLookingForUsers(OnNewUser, OnCalibrationStarted, OnCalibrationFailed, OnCalibrationSuccess, OnUserLost);

	while (true)
	{
		// check for exit
		if (kbhit())
		{
			char c = getch();
			if (27 == c)
			{
				break;
			}
		}

		// Next frame
		Update(false);
	}

	Shutdown();
}