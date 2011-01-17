using UnityEngine;using System;using System.Collections;using System.Collections.Generic;using System.Runtime.InteropServices;using System.IO;
using System.Threading;using System.Text; using xn;using xnv;public class Nite3 : MonoBehaviour{
	
			private readonly string SAMPLE_XML_FILE = @".//OpenNI.xml";
		private Context context;
		private DepthGenerator depth;
        private UserGenerator userGenerator;
        private SkeletonCapability skeletonCapbility;
        private PoseDetectionCapability poseDetectionCapability;
        private string calibPose;
		private bool shouldRun;

		

	public Transform rightHand;
	public Transform leftHand;
	public Transform leftWrist;
	public Transform rightWrist;
	
    public Transform leftElbow;
	public Transform rightElbow;
    public Transform rightArm;
    public Transform leftArm;
    public Transform rightKnee;
    public Transform leftKnee;
    public Transform rightHip;
    public Transform leftHip;
    public Transform rightAnkle;
    public Transform leftAnkle;
    public Transform rightFoot;
    public Transform leftFoot;
    public Transform waist;
	public Transform torso;

	public Transform neck;
	public Transform leftCollar;
	public Transform rightCollar;
    public Transform head;
	
	public Vector3 bias;
	public float scale;
        
        	void Start()	{

    
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
            this.userGenerator.StartGenerating();

			MapOutputMode mapMode = this.depth.GetMapOutputMode();

			
			this.shouldRun = true;
		}

        void skeletonCapbility_CalibrationEnd(ProductionNode node, uint id, bool success)
        {
            if (success)
            {
            	Debug.Log("callibration ended successfully");
                this.skeletonCapbility.StartTracking(id);
                
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
       //     this.joints.Remove(id);
        }

	void Update()
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
							MoveTransform(user, SkeletonJoint.Head, head);
						//	MoveTransform(user, SkeletonJoint.Neck, neck);
						//	MoveTransform(user, SkeletonJoint.Torso, torso);
						//	MoveTransform(user, SkeletonJoint.Waist, waist);
																					
							MoveTransform(user, SkeletonJoint.RightHand, rightHand);
						//	MoveTransform(user, SkeletonJoint.RightWrist, rightWrist);
							MoveTransform(user, SkeletonJoint.RightElbow, rightElbow);
							MoveTransform(user, SkeletonJoint.RightShoulder, rightArm);
						//	MoveTransform(user, SkeletonJoint.RightCollar, rightCollar);							
							MoveTransform(user, SkeletonJoint.RightHip, rightHip);
							MoveTransform(user, SkeletonJoint.RightKnee, rightKnee);
						//	MoveTransform(user, SkeletonJoint.RightAnkle, rightAnkle);
							MoveTransform(user, SkeletonJoint.RightFoot, rightFoot);														

							
							MoveTransform(user, SkeletonJoint.LeftHand, leftHand);
						//	MoveTransform(user, SkeletonJoint.LeftWrist, leftWrist);
							MoveTransform(user, SkeletonJoint.LeftElbow, leftElbow);
							MoveTransform(user, SkeletonJoint.LeftShoulder, leftArm);
						//	MoveTransform(user, SkeletonJoint.LeftCollar, leftCollar);
							MoveTransform(user, SkeletonJoint.LeftHip, leftHip);
							MoveTransform(user, SkeletonJoint.LeftKnee, leftKnee);
						//	MoveTransform(user, SkeletonJoint.LeftAnkle, leftAnkle);
							MoveTransform(user, SkeletonJoint.LeftFoot, leftFoot);														




							
                    	}
                    }
		}
	}
	
	
	void MoveTransform( uint userId, SkeletonJoint joint, Transform dest)
    {
		SkeletonJointPosition pos = new SkeletonJointPosition();
	    this.skeletonCapbility.GetSkeletonJointPosition(userId, joint, ref pos);
   	 	Vector3 v3pos = new Vector3(pos.position.X, pos.position.Y, pos.position.Z);
    	dest.position = (v3pos / scale) + bias;
            				
							
    }
	

    
}
