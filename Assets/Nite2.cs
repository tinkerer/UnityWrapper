using UnityEngine;using System;using System.Collections;using System.Collections.Generic;using System.Runtime.InteropServices;using System.IO;
using System.Threading;using System.Text; using xn;using xnv;public class Nite2 : MonoBehaviour{		private readonly string SAMPLE_XML_FILE = @".//OpenNI.xml";
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
    float[] usersHistogramMap;
		DepthMetaData depthMD;
		
		
		private int[] histogram;

        private Dictionary<uint, Dictionary<SkeletonJoint, SkeletonJointPosition>> joints;

        private bool shouldDrawPixels = true;
        private bool shouldDrawBackground = true;
        private bool shouldPrintID = true;
        private bool shouldPrintState = true;
        private bool shouldDrawSkeleton = true;
        
        
        
        
        	void Start()	{


/*really unity?
    
    do you do that for real?
    
    */
    
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

			usersMapSize = mapMode.nXRes * mapMode.nYRes;
			usersMapColors = new Color[usersMapSize];
        	usersMapRect = new Rect(Screen.width - usersLblTex.width / 2, Screen.height - usersLblTex.height / 2, usersLblTex.width / 2, usersLblTex.height / 2);
        usersLabelMap = new short[usersMapSize];
        usersDepthMap = new short[usersMapSize];
        usersHistogramMap = new float[5000];


			
			
			DepthMetaData depthMD = new DepthMetaData(); 
		
			
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

		void Update()
	{
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
							GetJoints(user);
                    	}
                    }
		}
		UpdateUserMap();
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
			Debug.Log("user: "+user+" joint: "+joint + "pos: "+pos.position);
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
		
//		this.depth.GetMetaData(depthMD);
		
		
		
		
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
        	
        	
            
            flipIndex = (int)usersMapSize - i - 1;
            
            
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
    
    
    /*
     private unsafe void CalcHist(DepthMetaData depthMD)
		{
			// reset
			for (int i = 0; i < this.histogram.Length; ++i)
				this.histogram[i] = 0;

			ushort* pDepth = (ushort*)depthMD.DepthMapPtr.ToPointer();

			int points = 0;
			for (int y = 0; y < depthMD.YRes; ++y)
			{
				for (int x = 0; x < depthMD.XRes; ++x, ++pDepth)
				{
					ushort depthVal = *pDepth;
					if (depthVal != 0)
					{
						this.histogram[depthVal]++;
						points++;
					}
				}
			}

			for (int i = 1; i < this.histogram.Length; i++)
			{
				this.histogram[i] += this.histogram[i-1];
			}

			if (points > 0)
			{
				for (int i = 1; i < this.histogram.Length; i++)
				{
					this.histogram[i] = (int)(256 * (1.0f - (this.histogram[i] / (float)points)));
				}
			}
		}
*/

    
}
