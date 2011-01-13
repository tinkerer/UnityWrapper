// This script blends between "walk", "runSlow" animations based on the speed and adjusts the playback speed to match
// It also additively blends in a lean animation, the lean animation

var walkSpeed = 2.3;
var runSpeed = 3.6;
var idleThreshold = 0.3;
var speedSmoothing = 8.0;


private var run : AnimationState;
private var walk : AnimationState;
private var leanLeft : AnimationState;
private var leanRight : AnimationState;

private var currentSpeed = 0.0;
private var smoothedSpeed = 0.0;
private var currentLean = 0.0;
private var smoothedLean = 0.0;

function SetCurrentSpeed ( newSpeed : float)
{
	currentSpeed = newSpeed;
}

function SetCurrentLean ( newSpeed : float)
{
	currentLean = newSpeed;
}


function Start ()
{
	// We are in full control here - don't let any other animations play when we start
	animation.Stop();

	// By default loop all animations
	animation.wrapMode = WrapMode.Loop;

	walk = animation["walk"];
	run = animation["runSlow"];

	leanLeft = animation["leanLeft"];
	leanRight = animation["leanRight"];

	// Do we actually have leaning animations? They are not necessary	
	if (leanLeft)
	{
		leanLeft.blendMode = AnimationBlendMode.Additive;
		leanRight.blendMode = AnimationBlendMode.Additive;
		leanLeft.wrapMode = WrapMode.ClampForever;
		leanRight.wrapMode = WrapMode.ClampForever;
		leanLeft.layer = 100;
		leanRight.layer = 100;
		leanLeft.weight = 1;
		leanRight.weight = 1;
		leanLeft.enabled = true;
		leanRight.enabled = true;
	}

	// Enable all walk / run cycles 
	// We will manually adjust the blend weights every frame
	walk.enabled = true;
	run.enabled = true;

	// Synchronize all walk / run cycle animations
	animation.SyncLayer(0);

	// Put the idle animation in a lower layer.
	// This will make it only play if no walk / run cycle is faded in
	animation["idle"].layer = -1;
	animation["idle"].enabled = true;
	animation["idle"].weight = 1;
}

function Update () {
	smoothedSpeed = Mathf.Lerp(smoothedSpeed, currentSpeed, Time.deltaTime * speedSmoothing);

	// Calculate the weight between walk and run from the current speed
	var runWeight = Mathf.InverseLerp(walkSpeed, runSpeed, Mathf.Abs(smoothedSpeed));
	
	// Only fast running animations get the lean added on top. A walking human does not lean
	var targetLean = currentLean * runWeight;
	smoothedLean = Mathf.Lerp(smoothedLean, targetLean, Time.deltaTime * speedSmoothing);
	
	// adjust the animation playback to match the characters speed
	run.speed = smoothedSpeed / runSpeed;
	walk.speed = smoothedSpeed / walkSpeed;

	// When the character slows down fade out the run and walk weights
	if (Mathf.Abs(smoothedSpeed) < idleThreshold)
	{
		run.weight  -= Time.deltaTime * 5.0;
		walk.weight -= Time.deltaTime * 5.0;
	}
	else
	{
		var totalWeight = run.weight + walk.weight;
		totalWeight += Time.deltaTime * 12;
		totalWeight = Mathf.Clamp01(totalWeight);

		// Set the weights based on the current speed
		run.weight = runWeight * totalWeight;
		walk.weight = (1.0 - runWeight) * totalWeight;
	}

	if (leanLeft)	
	{
		if (smoothedLean > 0)
		{
			leanLeft.normalizedTime = 0;
			leanRight.normalizedTime = smoothedLean;
		}
		else
		{
			leanLeft.normalizedTime = -smoothedLean;
			leanRight.normalizedTime = 0;
		}
	}
}