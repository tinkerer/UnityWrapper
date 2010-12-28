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

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text; 

public class NiteWrapper
{
	public enum SkeletonJoint
	{ 
		NONE = 0,
		HEAD = 1,
        NECK = 2,
        TORSO_CENTER = 3,
		WAIST = 4,

		LEFT_COLLAR = 5,
		LEFT_SHOULDER = 6,
        LEFT_ELBOW = 7,
        LEFT_WRIST = 8,
        LEFT_HAND = 9,
        LEFT_FINGERTIP = 10,

        RIGHT_COLLAR = 11,
		RIGHT_SHOULDER = 12,
		RIGHT_ELBOW = 13,
		RIGHT_WRIST = 14,
		RIGHT_HAND = 15,
        RIGHT_FINGERTIP = 16,

        LEFT_HIP = 17,
        LEFT_KNEE = 18,
        LEFT_ANKLE = 19,
        LEFT_FOOT = 20,

        RIGHT_HIP = 21,
		RIGHT_KNEE = 22,
        RIGHT_ANKLE = 23,
		RIGHT_FOOT = 24,

		END 
	};

    [StructLayout(LayoutKind.Sequential)]
    public struct SkeletonJointPosition
    {
        public float x, y, z;
        public float confidence;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SkeletonJointOrientation
    {
        public float    m00, m01, m02,
                        m10, m11, m12,
                        m20, m21, m22;
        public float confidence;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SkeletonJointTransformation
    {
        public SkeletonJointPosition pos;
        public SkeletonJointOrientation ori;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XnVector3D
    {
        public float x, y, z;
    }

	[DllImport("UnityInterface.dll")]
	public static extern uint Init(StringBuilder strXmlPath);
	[DllImport("UnityInterface.dll")]
	public static extern void Update(bool async);
	[DllImport("UnityInterface.dll")]
	public static extern void Shutdown();
	
	[DllImport("UnityInterface.dll")]
	public static extern IntPtr GetStatusString(uint rc);
	[DllImport("UnityInterface.dll")]
	public static extern int GetDepthWidth();
	[DllImport("UnityInterface.dll")]
	public static extern int GetDepthHeight();
	[DllImport("UnityInterface.dll")]
	public static extern IntPtr GetUsersLabelMap();
    [DllImport("UnityInterface.dll")]
    public static extern IntPtr GetUsersDepthMap();

	[DllImport("UnityInterface.dll")]
    public static extern void SetSkeletonSmoothing(double factor);
    [DllImport("UnityInterface.dll")]
    public static extern bool GetJointTransformation(uint userID, SkeletonJoint joint, ref SkeletonJointTransformation pTransformation);

    [DllImport("UnityInterface.dll")]
    public static extern void StartLookingForUsers(IntPtr NewUser, IntPtr CalibrationStarted, IntPtr CalibrationFailed, IntPtr CalibrationSuccess, IntPtr UserLost);
    [DllImport("UnityInterface.dll")]
    public static extern void StopLookingForUsers();
    [DllImport("UnityInterface.dll")]
    public static extern void LoseUsers();
    [DllImport("UnityInterface.dll")]
    public static extern bool GetUserCenterOfMass(uint userID, ref XnVector3D pCenterOfMass);
    [DllImport("UnityInterface.dll")]
    public static extern float GetUserPausePoseProgress(uint userID);

    public delegate void UserDelegate(uint userId);

    public static void StartLookingForUsers(UserDelegate NewUser, UserDelegate CalibrationStarted, UserDelegate CalibrationFailed, UserDelegate CalibrationSuccess, UserDelegate UserLost)
    {
        StartLookingForUsers(
            Marshal.GetFunctionPointerForDelegate(NewUser),
            Marshal.GetFunctionPointerForDelegate(CalibrationStarted),
            Marshal.GetFunctionPointerForDelegate(CalibrationFailed),
            Marshal.GetFunctionPointerForDelegate(CalibrationSuccess),
            Marshal.GetFunctionPointerForDelegate(UserLost));
    }
}

public class UnityUtils
{
    // Recursive
    public static Transform FindTransform(GameObject parentObj, string objName)
    {
        if (parentObj == null) return null;

        foreach (Transform trans in parentObj.transform)
        {
            if (trans.name == objName)
            {
                return trans;
            }

            Transform foundTransform = FindTransform(trans.gameObject, objName);
            if (foundTransform != null)
            {
                return foundTransform;
            }
        }

        return null;
    }
}

public class SoldierAvatar
{
    private Transform rightElbow;
    private Transform leftElbow;
    private Transform rightArm;
    private Transform leftArm;
    private Transform rightKnee;
    private Transform leftKnee;
    private Transform rightHip;
    private Transform leftHip;
    private Transform spine;
    private Transform root;

    private Quaternion[] initialRotations;
    private Quaternion initialRoot;

    public SoldierAvatar()
    {
    }

    public SoldierAvatar(GameObject go)
    {
        Initialize(go);
    }

    public void Initialize(GameObject go)
    {
        rightElbow = UnityUtils.FindTransform(go, "R_Elbow");
        leftElbow = UnityUtils.FindTransform(go, "L_Elbow");
        rightArm = UnityUtils.FindTransform(go, "R_Arm");
        leftArm = UnityUtils.FindTransform(go, "L_Arm");
        rightKnee = UnityUtils.FindTransform(go, "R_Knee");
        leftKnee = UnityUtils.FindTransform(go, "L_Knee");
        rightHip = UnityUtils.FindTransform(go, "R_Hip");
        leftHip = UnityUtils.FindTransform(go, "L_Hip");
        spine = UnityUtils.FindTransform(go, "Spine_1");
        root = UnityUtils.FindTransform(go, "Root");

        initialRotations = new Quaternion[(int)NiteWrapper.SkeletonJoint.END];
        initialRotations[(int)NiteWrapper.SkeletonJoint.LEFT_ELBOW] = leftElbow.rotation;
        initialRotations[(int)NiteWrapper.SkeletonJoint.RIGHT_ELBOW] = rightElbow.rotation;
        initialRotations[(int)NiteWrapper.SkeletonJoint.LEFT_SHOULDER] = leftArm.rotation;
        initialRotations[(int)NiteWrapper.SkeletonJoint.RIGHT_SHOULDER] = rightArm.rotation;
        initialRotations[(int)NiteWrapper.SkeletonJoint.RIGHT_KNEE] = rightKnee.rotation;
        initialRotations[(int)NiteWrapper.SkeletonJoint.LEFT_KNEE] = leftKnee.rotation;
        initialRotations[(int)NiteWrapper.SkeletonJoint.RIGHT_HIP] = rightHip.rotation;
        initialRotations[(int)NiteWrapper.SkeletonJoint.LEFT_HIP] = leftHip.rotation;
        initialRotations[(int)NiteWrapper.SkeletonJoint.TORSO_CENTER] = spine.rotation;
        initialRoot = root.rotation;

        RotateToCalibrationPose();
    }

    public void UpdateAvatar(uint userId)
    {
        root.rotation = Quaternion.LookRotation(Vector3.forward);

        TransformBone(userId, NiteWrapper.SkeletonJoint.TORSO_CENTER, spine);
        TransformBone(userId, NiteWrapper.SkeletonJoint.RIGHT_SHOULDER, rightArm);
        TransformBone(userId, NiteWrapper.SkeletonJoint.LEFT_SHOULDER, leftArm);
        TransformBone(userId, NiteWrapper.SkeletonJoint.RIGHT_ELBOW, rightElbow);
        TransformBone(userId, NiteWrapper.SkeletonJoint.LEFT_ELBOW, leftElbow);
        TransformBone(userId, NiteWrapper.SkeletonJoint.RIGHT_HIP, rightHip);
        TransformBone(userId, NiteWrapper.SkeletonJoint.LEFT_HIP, leftHip);
        TransformBone(userId, NiteWrapper.SkeletonJoint.RIGHT_KNEE, rightKnee);
        TransformBone(userId, NiteWrapper.SkeletonJoint.LEFT_KNEE, leftKnee);

        root.rotation = Quaternion.LookRotation(-Vector3.forward);	
    }

    public void RotateToInitialPosition()
    {
        root.rotation = initialRoot;
        spine.rotation = initialRotations[(int)NiteWrapper.SkeletonJoint.TORSO_CENTER];
        rightArm.rotation = initialRotations[(int)NiteWrapper.SkeletonJoint.RIGHT_SHOULDER];
        leftArm.rotation = initialRotations[(int)NiteWrapper.SkeletonJoint.LEFT_SHOULDER];
        rightElbow.rotation = initialRotations[(int)NiteWrapper.SkeletonJoint.RIGHT_ELBOW];
        leftElbow.rotation = initialRotations[(int)NiteWrapper.SkeletonJoint.LEFT_ELBOW];
        rightHip.rotation = initialRotations[(int)NiteWrapper.SkeletonJoint.RIGHT_HIP];
        leftHip.rotation = initialRotations[(int)NiteWrapper.SkeletonJoint.LEFT_HIP];
        rightKnee.rotation = initialRotations[(int)NiteWrapper.SkeletonJoint.RIGHT_KNEE];
        leftKnee.rotation = initialRotations[(int)NiteWrapper.SkeletonJoint.LEFT_KNEE];
    }

    public void RotateToCalibrationPose()
    {
        // Calibration pose is simply initial position with hands raised up
        RotateToInitialPosition();
        rightElbow.rotation = Quaternion.Euler(0, -90, 90) * initialRotations[(int)NiteWrapper.SkeletonJoint.RIGHT_ELBOW];
        leftElbow.rotation = Quaternion.Euler(0, 90, -90) * initialRotations[(int)NiteWrapper.SkeletonJoint.LEFT_ELBOW];
    }

    void TransformBone(uint userId, NiteWrapper.SkeletonJoint joint, Transform dest)
    {
        NiteWrapper.SkeletonJointTransformation trans = new NiteWrapper.SkeletonJointTransformation();
        NiteWrapper.GetJointTransformation(userId, joint, ref trans);

        // only modify joint if confidence is high enough in this frame
        if (trans.ori.confidence > 0.5)
        {
            // Z coordinate in OpenNI is opposite from Unity. We will create a quat
            // to rotate from OpenNI to Unity (relative to initial rotation)
            Vector3 worldZVec = new Vector3(-trans.ori.m02, -trans.ori.m12, trans.ori.m22);
            Vector3 worldYVec = new Vector3(trans.ori.m01, trans.ori.m11, -trans.ori.m21);
            Quaternion jointRotation = Quaternion.LookRotation(worldZVec, worldYVec);

            Quaternion newRotation = jointRotation * initialRotations[(int)joint];

            // Some smoothing
            dest.rotation = Quaternion.Slerp(dest.rotation, newRotation, Time.deltaTime * 20);
        }
    }
}

public class Nite : MonoBehaviour
{
    Texture2D usersLblTex;
    Color[] usersMapColors;
    Rect usersMapRect;
    int usersMapSize;
    short[] usersLabelMap;
    short[] usersDepthMap;
    float[] usersHistogramMap;

    SoldierAvatar[] soldiers;
    GUIText caption;

    NiteWrapper.UserDelegate NewUser;
    NiteWrapper.UserDelegate CalibrationStarted;
    NiteWrapper.UserDelegate CalibrationFailed;
    NiteWrapper.UserDelegate CalibrationSuccess;
    NiteWrapper.UserDelegate UserLost;

    List<uint> allUsers;
    Dictionary<uint, SoldierAvatar> calibratedUsers;

    void OnNewUser(uint UserId)
    {
        Debug.Log(String.Format("[{0}] New user", UserId));
        allUsers.Add(UserId);
    }   

    void OnCalibrationStarted(uint UserId)
    {
		Debug.Log(String.Format("[{0}] Calibration started", UserId));
    }

    void OnCalibrationFailed(uint UserId)
    {
        Debug.Log(String.Format("[{0}] Calibration failed", UserId));
    }

    void OnCalibrationSuccess(uint UserId)
    {
        Debug.Log(String.Format("[{0}] Calibration success", UserId));
		
        // Associate this user to an unused soldier avatar
        for (int i=0; i<soldiers.Length; i++)
        {
            if (!calibratedUsers.ContainsValue(soldiers[i]))
            {
                calibratedUsers.Add(UserId, soldiers[i]);
                break;
            }
        }

        // Should we stop looking for users?
        if (calibratedUsers.Count == soldiers.Length)
        {
			Debug.Log("Stopping to look for users");
            NiteWrapper.StopLookingForUsers();
        }
    }

    void OnUserLost(uint UserId)
    {
        Debug.Log(String.Format("[{0}] User lost", UserId));

        // If this was one of our calibrated users, mapped to an avatar
        if (calibratedUsers.ContainsKey(UserId))
        {
            // reset the avatar and remove from list
            calibratedUsers[UserId].RotateToCalibrationPose();
            calibratedUsers.Remove(UserId);

            // Should we start looking for users again?
            if (calibratedUsers.Count < soldiers.Length)
            {
                Debug.Log("Starting to look for users");
                NiteWrapper.StartLookingForUsers(NewUser, CalibrationStarted, CalibrationFailed, CalibrationSuccess, UserLost);
            }
        }

        // remove from global users list
        allUsers.Remove(UserId);
    }

    void Start()
	{
		uint rc = NiteWrapper.Init(new StringBuilder(".\\OpenNI.xml"));
        if (rc != 0)
        {
            Debug.Log(String.Format("Error initing OpenNI: {0}", Marshal.PtrToStringAnsi(NiteWrapper.GetStatusString(rc))));
        }

        // Init depth & label map related stuff
        usersMapSize = NiteWrapper.GetDepthWidth() * NiteWrapper.GetDepthHeight();
        usersLblTex = new Texture2D(NiteWrapper.GetDepthWidth(), NiteWrapper.GetDepthHeight());
        usersMapColors = new Color[usersMapSize];
        usersMapRect = new Rect(Screen.width - usersLblTex.width / 2, Screen.height - usersLblTex.height / 2, usersLblTex.width / 2, usersLblTex.height / 2);
        usersLabelMap = new short[usersMapSize];
        usersDepthMap = new short[usersMapSize];
        usersHistogramMap = new float[5000];

        // text
        caption = GameObject.Find("GUI Text").guiText;

        // init our avatar controllers
        soldiers = new SoldierAvatar[2];
        soldiers[0] = new SoldierAvatar(GameObject.Find("Soldier1"));
        soldiers[1] = new SoldierAvatar(GameObject.Find("Soldier2"));

        // init user lists - one will contain all users, the second will contain only calibrated & mapped users
        allUsers = new List<uint>();
        calibratedUsers = new Dictionary<uint, SoldierAvatar>();
        
        // init user callbacks
        NewUser = new NiteWrapper.UserDelegate(OnNewUser);
        CalibrationStarted = new NiteWrapper.UserDelegate(OnCalibrationStarted);
        CalibrationFailed = new NiteWrapper.UserDelegate(OnCalibrationFailed);
        CalibrationSuccess = new NiteWrapper.UserDelegate(OnCalibrationSuccess);
        UserLost = new NiteWrapper.UserDelegate(OnUserLost);

        // Start looking
        NiteWrapper.StartLookingForUsers(NewUser, CalibrationStarted, CalibrationFailed, CalibrationSuccess, UserLost);
		Debug.Log("Waiting for users to calibrate");
		
		// set default smoothing
		NiteWrapper.SetSkeletonSmoothing(0.6);		
	}
	
	void Update()
	{
        // Next NITE frame
		NiteWrapper.Update(false);

        // update the visual user map
        UpdateUserMap();

        // update avatars
        foreach (KeyValuePair<uint, SoldierAvatar> pair in calibratedUsers)
        {
            pair.Value.UpdateAvatar(pair.Key);
        }
	}

	void OnApplicationQuit()
	{
		NiteWrapper.Shutdown();
	}

    void OnGUI()
    {
        if (calibratedUsers.Count < soldiers.Length)
        {
            GUI.DrawTexture(usersMapRect, usersLblTex);
            caption.text = String.Format("Waiting for {0} more users to calibrate...", soldiers.Length - calibratedUsers.Count);
        }
        else
        {
            caption.text = "All users calibrated";
        }

        foreach (uint userId in allUsers)
        {
            float progress = NiteWrapper.GetUserPausePoseProgress(userId);
            if (NiteWrapper.GetUserPausePoseProgress(userId) > 0.0)
            {
                caption.text = String.Format("[User {0}] Pause pose progress: {1}", userId, progress);
                break;
            }
        }
    }

    void UpdateUserMap()
    {
        // copy over the maps
        Marshal.Copy(NiteWrapper.GetUsersLabelMap(), usersLabelMap, 0, usersMapSize);
        Marshal.Copy(NiteWrapper.GetUsersDepthMap(), usersDepthMap, 0, usersMapSize);

        // we will be flipping the texture as we convert label map to color array
        int flipIndex, i;
        int numOfPoints = 0;
		Array.Clear(usersHistogramMap, 0, usersHistogramMap.Length);

        // calculate cumulative histogram for depth
        for (i = 0; i < usersMapSize; i++)
        {
            // only calculate for depth that contains users
            if (usersLabelMap[i] != 0)
            {
                usersHistogramMap[usersDepthMap[i]]++;
                numOfPoints++;
            }
        }
        if (numOfPoints > 0)
        {
            for (i = 1; i < usersHistogramMap.Length; i++)
	        {   
		        usersHistogramMap[i] += usersHistogramMap[i-1];
	        }
            for (i = 0; i < usersHistogramMap.Length; i++)
	        {
                usersHistogramMap[i] = 1.0f - (usersHistogramMap[i] / numOfPoints);
	        }
        }

        // create the actual users texture based on label map and depth histogram
        for (i = 0; i < usersMapSize; i++)
        {
            flipIndex = usersMapSize - i - 1;
            if (usersLabelMap[i] == 0)
            {
                usersMapColors[flipIndex] = Color.clear;
            }
            else
            {
                // create a blending color based on the depth histogram
                Color c = new Color(usersHistogramMap[usersDepthMap[i]], usersHistogramMap[usersDepthMap[i]], usersHistogramMap[usersDepthMap[i]], 0.9f);
                switch (usersLabelMap[i] % 4)
                {
                    case 0:
                        usersMapColors[flipIndex] = Color.red * c;
                        break;
                    case 1:
                        usersMapColors[flipIndex] = Color.green * c;
                        break;
                    case 2:
                        usersMapColors[flipIndex] = Color.blue * c;
                        break;
                    case 3:
                        usersMapColors[flipIndex] = Color.magenta * c;
                        break;
                }
            }
        }

        usersLblTex.SetPixels(usersMapColors);
        usersLblTex.Apply();
    }
}
