using UnityEngine;using System;using System.Collections;using System.Collections.Generic;using System.Runtime.InteropServices;using System.IO;
using System.Threading;using System.Text; using xn;using xnv;public class Nite2 : MonoBehaviour{
	
	
	
	
	public Transform rightElbow;
	
	//VT
	public Transform rightWrist;
	
    public Transform leftElbow;
    public Transform rightArm;
    public Transform leftArm;
    public Transform rightKnee;
    public Transform leftKnee;
    public Transform rightHip;
    public Transform leftHip;
    public Transform spine;
    public Transform root;

    private Quaternion[] initialRotations;
    private Quaternion initialRoot;

	
	
	
			private readonly string SAMPLE_XML_FILE = @".//OpenNI.xml";
		private Context context;
		private DepthGenerator depth;
        private UserGenerator userGenerator;
        private SkeletonCapability skeletonCapbility;
        private PoseDetectionCapability poseDetectionCapability;
        private string calibPose;
		private Thread readerThread;
		private bool shouldRun;
		//private Bitmap bitmap;
		
		Texture2D usersLblTex;
		    Color[] usersMapColors;
 		   Rect usersMapRect;
    uint usersMapSize;
    short[] usersLabelMap;
    short[] usersDepthMap;
    MapData<ushort> LabelMap;
    MapData<ushort> DepthMap;
    
    float[] usersHistogramMap;
		DepthMetaData depthMD;
		
		
		private int[] histogram;

        private Dictionary<uint, Dictionary<SkeletonJoint, SkeletonJointPosition>> joints;

        private bool shouldDrawPixels = true;
        private bool shouldDrawBackground = true;
        private bool shouldPrintID = true;
        private bool shouldPrintState = true;
        private bool shouldDrawSkeleton = true;
        
        
      public void RotateToInitialPosition()
    {
        //root.rotation = initialRoot;
        spine.rotation = initialRotations[(int)SkeletonJoint.Torso];
        rightArm.rotation = initialRotations[(int)SkeletonJoint.RightShoulder];
        leftArm.rotation = initialRotations[(int)SkeletonJoint.LeftShoulder];
        rightElbow.rotation = initialRotations[(int)SkeletonJoint.RightElbow];
		
		//VT
		rightWrist.rotation = initialRotations[(int)SkeletonJoint.RightWrist];
        
		leftElbow.rotation = initialRotations[(int)SkeletonJoint.LeftElbow];
        rightHip.rotation = initialRotations[(int)SkeletonJoint.RightHip];
        leftHip.rotation = initialRotations[(int)SkeletonJoint.LeftHip];
        rightKnee.rotation = initialRotations[(int)SkeletonJoint.RightKnee];
        leftKnee.rotation = initialRotations[(int)SkeletonJoint.LeftKnee];
    }

    
    
    public void RotateToCalibrationPose()
    {
        // Calibration pose is simply initial position with hands raised up
        RotateToInitialPosition();
        rightElbow.rotation = Quaternion.Euler(0, -90, 90) * initialRotations[(int)SkeletonJoint.RightElbow];
        leftElbow.rotation = Quaternion.Euler(0, 90, -90) * initialRotations[(int)SkeletonJoint.LeftElbow];
    }
        
    void InitializeCharacter()
    {
    
    
        initialRotations = new Quaternion[24]; //i count 24 joints in xn.SkeletonJoint spec
        initialRotations[(int)SkeletonJoint.LeftElbow] = leftElbow.rotation;
        initialRotations[(int)SkeletonJoint.RightElbow] = rightElbow.rotation;
        initialRotations[(int)SkeletonJoint.LeftShoulder] = leftArm.rotation;
        initialRotations[(int)SkeletonJoint.RightShoulder] = rightArm.rotation;
        initialRotations[(int)SkeletonJoint.RightKnee] = rightKnee.rotation;
        initialRotations[(int)SkeletonJoint.LeftKnee] = leftKnee.rotation;
        initialRotations[(int)SkeletonJoint.RightHip] = rightHip.rotation;
        initialRotations[(int)SkeletonJoint.LeftHip] = leftHip.rotation;
        initialRotations[(int)SkeletonJoint.Torso] = spine.rotation;
		
		//VT
		initialRotations[(int)SkeletonJoint.RightWrist] = rightWrist.rotation;
		
        //initialRoot = root.rotation;

        RotateToCalibrationPose();
    
    }
        
        	void Start()	{


/*really unity?
    
    do you do that for real?
    
    */
    
    InitializeCharacter();
		this.context = new Context(SAMPLE_XML_FILE);
			this.depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
			if (this.depth == null)
			{
				throw new Exception("Viewer must have a depth node!");
			}

            this.userGenerator = new UserGenerator(this.context);
            this.skeletonCapbility = new SkeletonCapability(this.userGenerator);
            this.poseDetectionCapability = new PoseDetectionCapability(this.userGenerator);
            this.calibPose = this.skeletonCapbility.GetCalibrationPose();

            this.userGenerator.NewUser += new UserGenerator.NewUserHandler(userGenerator_NewUser);
            this.userGenerator.LostUser += new UserGenerator.LostUserHandler(userGenerator_LostUser);
            this.poseDetectionCapability.PoseDetected += new PoseDetectionCapability.PoseDetectedHandler(poseDetectionCapability_PoseDetected);
            this.skeletonCapbility.CalibrationEnd += new SkeletonCapability.CalibrationEndHandler(skeletonCapbility_CalibrationEnd);

            this.skeletonCapbility.SetSkeletonProfile(SkeletonProfile.All);
            this.joints = new Dictionary<uint,Dictionary<SkeletonJoint,SkeletonJointPosition>>();
            this.userGenerator.StartGenerating();


			this.histogram = new int[this.depth.GetDeviceMaxDepth()];

			MapOutputMode mapMode = this.depth.GetMapOutputMode();

//			this.bitmap = new Bitmap((int)mapMode.nXRes, (int)mapMode.nYRes/*, System.Drawing.Imaging.PixelFormat.Format24bppRgb*/);
			usersLblTex = new Texture2D((int)mapMode.nXRes, (int)mapMode.nYRes);
		Debug.Log("usersLblTex = w: "+ usersLblTex.width + " h: " + usersLblTex.height );


			usersMapSize = mapMode.nXRes * mapMode.nYRes;
			usersMapColors = new Color[usersMapSize];
        	usersMapRect = new Rect(Screen.width - usersLblTex.width / 2, Screen.height - usersLblTex.height / 2, usersLblTex.width / 2, usersLblTex.height / 2);
        usersLabelMap = new short[usersMapSize];
        usersDepthMap = new short[usersMapSize];

        usersHistogramMap = new float[5000];


			
			
			//DepthMetaData depthMD = new DepthMetaData(); 
		
			
			this.shouldRun = true;
			//this.readerThread = new Thread(ReaderThread);
		//	this.readerThread.Start();
		}

        void skeletonCapbility_CalibrationEnd(ProductionNode node, uint id, bool success)
        {
            if (success)
            {
            	Debug.Log("callibration ended successfully");
                this.skeletonCapbility.StartTracking(id);
                this.joints.Add(id, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
                
            }
            else
            {
                this.poseDetectionCapability.StartPoseDetection(calibPose, id);
            }
        }

        void poseDetectionCapability_PoseDetected(ProductionNode node, string pose, uint id)
        {
            this.poseDetectionCapability.StopPoseDetection(id);
            this.skeletonCapbility.RequestCalibration(id, true);
        }

        void userGenerator_NewUser(ProductionNode node, uint id)
        {
        	Debug.Log("New User Found");
            this.poseDetectionCapability.StartPoseDetection(this.calibPose, id);
        }

        void userGenerator_LostUser(ProductionNode node, uint id)
        {
        	Debug.Log("lost user");
            this.joints.Remove(id);
        }

	public Transform pLHand;
	public Transform pRHand;
	public Transform pLElbow;
	public Transform pRElbow;
	public Transform pHead;
	
	
	public Vector3 bias;
	public float scale;	void Update()
	{
		bool doUpdate = true;
		if (this.shouldRun)
		{
				try
				{
					this.context.WaitOneUpdateAll(this.depth);
				}
				catch (Exception)
				{
				}

				//this.depth.GetMetaData(depthMD);


           uint[] users = this.userGenerator.GetUsers();
           //Debug.Log(users.Length);
		   foreach (uint user in users)
                    {
                    	if (this.skeletonCapbility.IsTracking(user))
                    	{
                    		doUpdate = false;
                    		//Debug.Log("here we go");
							
							
							//GetJoints(user);
							//this.UpdateAvatar(user);
							MoveTransform(user, SkeletonJoint.Head, pHead);
							MoveTransform(user, SkeletonJoint.RightHand, pRHand);
							MoveTransform(user, SkeletonJoint.RightElbow, pRElbow);
							MoveTransform(user, SkeletonJoint.LeftHand, pLHand);
							MoveTransform(user, SkeletonJoint.LeftElbow, pLElbow);

							
                    	}
                    }
		}
		if (doUpdate)
		{
		//	UpdateUserMap();
		}
	}
	
	
	void MoveTransform( uint userId, SkeletonJoint joint, Transform dest)
    {
		SkeletonJointPosition pos = new SkeletonJointPosition();
	    this.skeletonCapbility.GetSkeletonJointPosition(userId, joint, ref pos);
   	 	Vector3 v3pos = new Vector3(pos.position.X, pos.position.Y, pos.position.Z);
    	dest.position = (v3pos / scale) + bias;
            				
							
    }
	void UpdateAvatar(uint userId)
    {
        //root.rotation = Quaternion.LookRotation(Vector3.forward);

        TransformBone(userId, SkeletonJoint.Torso, spine, true);
        TransformBone(userId, SkeletonJoint.RightShoulder, rightArm, false);
        TransformBone(userId, SkeletonJoint.LeftShoulder, leftArm, false);
        TransformBone(userId, SkeletonJoint.RightElbow, rightElbow, false);
		
        TransformBone(userId, SkeletonJoint.LeftElbow, leftElbow, false);
        TransformBone(userId, SkeletonJoint.RightHip, rightHip, false);
        TransformBone(userId, SkeletonJoint.LeftHip, leftHip, false);
        TransformBone(userId, SkeletonJoint.RightKnee, rightKnee, false);
        TransformBone(userId, SkeletonJoint.LeftKnee, leftKnee, false);
    }
	
	
	  void TransformBone(uint userId, SkeletonJoint joint, Transform dest, bool move)
    {
    	
        SkeletonJointPosition sjp = this.joints[userId][joint];
        Point3D pos = sjp.position;
        
        //Debug.Log("joint " + joint + "x " + pos.X + " y " + pos.Y + " z " + pos.Z);
        
      //  SkeletonJointOrientation ori = new SkeletonJointOrientation();
      //  this.skeletonCapbility.GetSkeletonJointOrientation(userId, joint, out ori);
       /* float [] m = ori.Orientation.elements;                       
        
        //Debug.Log(m.Length);
        
        
        // only modify joint if confidence is high enough in this frame
        if (ori.Confidence > 0.5)
        {
            // Z coordinate in OpenNI is opposite from Unity. We will create a quat
            // to rotate from OpenNI to Unity (relative to initial rotation)
        //    Vector3 worldZVec = new Vector3(-ori.m02, -ori.m12, ori.m22);
           Vector3 worldZVec = new Vector3(-m[2], -m[5], m[8]);

         //   Vector3 worldYVec = new Vector3(trans.ori.m01, trans.ori.m11, -trans.ori.m21);
           Vector3 worldYVec = new Vector3(m[1], m[4], -m[7]);

            Quaternion jointRotation = Quaternion.LookRotation(worldZVec, worldYVec);

            Quaternion newRotation = jointRotation * initialRotations[(int)joint];

            // Some smoothing
            dest.rotation = Quaternion.Slerp(dest.rotation, newRotation, Time.deltaTime * 20);
        }
		
		if (move)
		{
//			dest.position = new Vector3(trans.pos.x/1000, trans.pos.y/1000 -1, -trans.pos.z/1000);
			dest.position = new Vector3(pos.X/1000, pos.Y/1000 -1, -pos.Z/1000);

		
        }
       */
        
        		
    }
	
	
	
	
		  private void GetJoint(uint user, SkeletonJoint joint)
        {
            SkeletonJointPosition pos = new SkeletonJointPosition();
            this.skeletonCapbility.GetSkeletonJointPosition(user, joint, ref pos);
			if (pos.position.Z == 0)
			{
				pos.fConfidence = 0;
			}
			else
			{
				pos.position = this.depth.ConvertRealWorldToProjective(pos.position);
			}
			this.joints[user][joint] = pos;
		
        }

        private void GetJoints(uint user)
        {
            GetJoint(user, SkeletonJoint.Head);
            GetJoint(user, SkeletonJoint.Neck);

            GetJoint(user, SkeletonJoint.LeftShoulder);
            GetJoint(user, SkeletonJoint.LeftElbow);
            GetJoint(user, SkeletonJoint.LeftHand);

            GetJoint(user, SkeletonJoint.RightShoulder);
            GetJoint(user, SkeletonJoint.RightElbow);
            GetJoint(user, SkeletonJoint.RightHand);

            GetJoint(user, SkeletonJoint.Torso);

            GetJoint(user, SkeletonJoint.LeftHip);
            GetJoint(user, SkeletonJoint.LeftKnee);
            GetJoint(user, SkeletonJoint.LeftFoot);

            GetJoint(user, SkeletonJoint.RightHip);
            GetJoint(user, SkeletonJoint.RightKnee);
            GetJoint(user, SkeletonJoint.RightFoot);
        }	void OnGUI()
	{
		
			 GUI.DrawTexture(usersMapRect, usersLblTex);
		
	}
	

		
	
	 void UpdateUserMap()
    {
        // copy over the maps
        //Marshal.Copy(NiteWrapper.GetUsersLabelMap(), usersLabelMap, 0, usersMapSize);
        //Marshal.Copy(NiteWrapper.GetUsersDepthMap(), usersDepthMap, 0, usersMapSize);
		
		//this.depth.GetMetaData(depthMD);
		

		DepthMap = this.depth.GetDepthMap();
		LabelMap = this.userGenerator.GetUserPixels(0).GetSceneMap();
		
        // we will be flipping the texture as we convert label map to color array
        int flipIndex, i;
        int numOfPoints = 0;
		Array.Clear(usersHistogramMap, 0, usersHistogramMap.Length);

        // calculate cumulative histogram for depth
        for (i = 0; i < usersMapSize; i++)
        {
        
        
        
            // only calculate for depth that contains users
            //if (usersLabelMap[i] != 0)
			if (LabelMap[i] != 0)

            {
            	
            	
            	
//                usersHistogramMap[usersDepthMap[i]]++;
                  usersHistogramMap[DepthMap[i]]++;              
                
                
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
        	
        	
            
            flipIndex = (int)usersMapSize - i - 1;
            
            
        //    if (usersLabelMap[i] == 0)
              if (LabelMap[i] == 0)
  
            
            {
            	
            	
                usersMapColors[flipIndex] = Color.clear;
            }
            else
            {
            	
            	
            	
            	
            	
                // create a blending color based on the depth histogram
       //Color c = new Color(usersHistogramMap[usersDepthMap[i]], usersHistogramMap[usersDepthMap[i]], usersHistogramMap[usersDepthMap[i]], 0.9f);
       Color c = new Color(usersHistogramMap[DepthMap[i]], usersHistogramMap[DepthMap[i]], usersHistogramMap[DepthMap[i]], 0.9f);
            //    switch (usersLabelMap[i] % 4)
                switch (LabelMap[i] % 4)

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
