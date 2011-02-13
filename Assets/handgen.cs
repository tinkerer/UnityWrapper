//made by Matt Thomas and Amir Hirsch, sorta
using UnityEngine;
using System.Collections;
using xn;

public class handgen : MonoBehaviour
{
	public Transform hand;
	public float scale = 1.0f;
	public Vector3 bias;
	
	private readonly string SAMPLE_XML_FILE = @"OpenNI.xml";
	private Context context;
	private DepthGenerator depth;
	private HandsGenerator hands;
	private GestureGenerator gestures;
	
	void Start ()
	{
		Debug.Log ("Start(): Initializing nodes.");
		
		this.context = new Context (SAMPLE_XML_FILE);
		this.depth = context.FindExistingNode (NodeType.Depth) as DepthGenerator;
		if (this.depth == null) {
			Debug.LogError ("Viewer must have a depth node!");
		}
		this.hands = context.FindExistingNode (NodeType.Hands) as HandsGenerator;
		if (this.hands == null) {
			Debug.LogError ("Viewer must have a hands node!");
		}
		this.gestures = context.FindExistingNode (NodeType.Gesture) as GestureGenerator;
		if (this.gestures == null) {
			Debug.LogError ("Viewer must have a gestures node!");
		}
		
		this.hands.HandCreate += new HandsGenerator.HandCreateHandler (hands_HandCreate);
		this.hands.HandUpdate += new HandsGenerator.HandUpdateHandler (hands_HandUpdate);
		this.hands.HandDestroy += new HandsGenerator.HandDestroyHandler (hands_HandDestroy);
		
		this.gestures.AddGesture ("Wave");
		this.gestures.AddGesture ("RaiseHand");
		this.gestures.GestureRecognized += new GestureGenerator.GestureRecognizedHandler (gestures_GestureRecognized);
		this.gestures.StartGenerating ();
	}

	void Update ()
	{
		this.context.WaitOneUpdateAll (this.depth);
	}

	void gestures_GestureRecognized (ProductionNode node, string strGesture, ref Point3D idPosition, ref Point3D endPosition)
	{
		Debug.Log ("gestures_GestureRecognized(): Here");
		switch (strGesture) {
		case "Wave":
			this.hands.StartTracking (ref idPosition);
			break;
		default:
			break;
		}
	}

	void hands_HandDestroy (ProductionNode node, uint id, float fTime)
	{
		Debug.Log ("hands_HandDestroy(): Here");
	}

	void hands_HandUpdate (ProductionNode node, uint id, ref Point3D position, float fTime)
	{
		float x = position.X;
		float y = position.Y;
		float z = position.Z;
	//	Debug.Log ("hands_HandUpdate(): Position X: " + x + " Y: " + y + " Z: " + z);
		Vector3 pos = new Vector3(x * scale, y * scale, -z * scale);
		pos -= bias;
	//	if (pos.z > 0) pos.z = 0;
		hand.position = pos;
	}

	void hands_HandCreate (ProductionNode node, uint id, ref Point3D position, float fTime)
	{
		Debug.Log ("hands_HandCreate(): Here");
		float x = position.X;
		float y = position.Y;
		float z = position.Z;
		Debug.Log ("hands_HandUpdate(): Position X: " + x + " Y: " + y + " Z: " + z);
		Vector3 pos = new Vector3(x * scale, y * scale, -z * scale);
		bias = pos;
	}
	
	void OnApplicationQuit()
	{			
		this.context.Shutdown();		
	}	
	
}
