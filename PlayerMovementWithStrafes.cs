using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementWithStrafes : MonoBehaviour
{
	public CharacterController controller;
	public Transform GroundCheck;
	public LayerMask GroundMask;

	private float wishspeed2;
	private float gravity = -20f;
	float wishspeed;

	public float GroundDistance = 0.4f;
	public float moveSpeed = 7.0f;  // Ground move speed
	public float runAcceleration = 14f;   // Ground accel
	public float runDeacceleration = 10f;   // Deacceleration that occurs when running on the ground
	public float airAcceleration = 2.0f;  // Air accel
	public float airDeacceleration = 2.0f;    // Deacceleration experienced when opposite strafing
	public float airControl = 0.3f;  // How precise air control is
	public float sideStrafeAcceleration = 50f;   // How fast acceleration occurs to get up to sideStrafeSpeed when side strafing
	public float sideStrafeSpeed = 1f;    // What the max speed to generate when side strafing
	public float jumpSpeed = 8.0f;
	public float friction = 6f;
	private float playerTopVelocity = 0;
	public float playerFriction = 0f;
	float addspeed;
	float accelspeed;
	float currentspeed;
	float zspeed;
	float speed;
	float dot;
	float k;
	float accel;
	float newspeed;
	float control;
	float drop;

	public bool JumpQueue = false;
	public bool wishJump = false;

    //UI
	private Vector3 lastPos;
	private Vector3 moved;
	public Vector3 PlayerVel;
	public float ModulasSpeed;
	public float ZVelocity;
	public float XVelocity;
	//End UI

	public Vector3 moveDirection;
	public Vector3 moveDirectionNorm;
	private Vector3 playerVelocity;
	Vector3 wishdir;
	Vector3 vec;
	
	public Transform playerView;

	public float x;
	public float z;

	public bool IsGrounded;

	public Transform player;
	Vector3 udp;


    private void Start()
    {
        //This is for UI, feel free to remove the Start() function.
		lastPos = player.position;
	}

    // Update is called once per frame
    void Update()
	{
		#region //UI, Feel free to remove the region.

		moved = player.position - lastPos;
		lastPos = player.position;
		PlayerVel = moved / Time.fixedDeltaTime;

		ZVelocity = Mathf.Abs(PlayerVel.z);
		XVelocity = Mathf.Abs(PlayerVel.x);


		ModulasSpeed = Mathf.Sqrt(PlayerVel.z * PlayerVel.z + PlayerVel.x * PlayerVel.x);

		#endregion

		IsGrounded = Physics.CheckSphere(GroundCheck.position, GroundDistance, GroundMask);

		QueueJump();

		/* Movement, here's the important part */
		if (controller.isGrounded)
			GroundMove();
		else if (!controller.isGrounded)
			AirMove();

		// Move the controller
		controller.Move(playerVelocity * Time.deltaTime);

		// Calculate top velocity
		udp = playerVelocity;
		udp.y = 0;
		if (udp.magnitude > playerTopVelocity)
			playerTopVelocity = udp.magnitude;
	}
	public void SetMovementDir()
	{
		x = Input.GetAxis("Horizontal");
		z = Input.GetAxis("Vertical");
	}

	//Queues the next jump
	void QueueJump()
	{
		if (Input.GetButtonDown("Jump") && IsGrounded)
		{
			wishJump = true;
		}

		if (!IsGrounded && Input.GetButtonDown("Jump"))
		{
			JumpQueue = true;
		}
		if (IsGrounded && JumpQueue)
		{
			wishJump = true;
			JumpQueue = false;
		}
	}

	//Calculates wish acceleration
	public void Accelerate(Vector3 wishdir, float wishspeed, float accel)
	{
		currentspeed = Vector3.Dot(playerVelocity, wishdir);
		addspeed = wishspeed - currentspeed;
		if (addspeed <= 0)
			return;
		accelspeed = accel * Time.deltaTime * wishspeed;
		if (accelspeed > addspeed)
			accelspeed = addspeed;

		playerVelocity.x += accelspeed * wishdir.x;
		playerVelocity.z += accelspeed * wishdir.z;
	}

	//Execs when the player is in the air
	public void AirMove()
	{
		SetMovementDir();

		wishdir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		wishdir = transform.TransformDirection(wishdir);

		wishspeed = wishdir.magnitude;

		wishspeed *= 7f;

		wishdir.Normalize();
		moveDirectionNorm = wishdir;

		// Aircontrol
		wishspeed2 = wishspeed;
		if (Vector3.Dot(playerVelocity, wishdir) < 0)
			accel = airDeacceleration;
		else
			accel = airAcceleration;

		// If the player is ONLY strafing left or right
		if (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") != 0)
		{
			if (wishspeed > sideStrafeSpeed)
				wishspeed = sideStrafeSpeed;
			accel = sideStrafeAcceleration;
		}

		Accelerate(wishdir, wishspeed, accel);

		AirControl(wishdir, wishspeed2);

		// !Aircontrol

		// Apply gravity
		playerVelocity.y += gravity * Time.deltaTime;

		/**
			* Air control occurs when the player is in the air, it allows
			* players to move side to side much faster rather than being
			* 'sluggish' when it comes to cornering.
			*/

		void AirControl(Vector3 wishdir, float wishspeed)
		{
			// Can't control movement if not moving forward or backward
			if (Input.GetAxis("Horizontal") == 0 || wishspeed == 0)
				return;

			zspeed = playerVelocity.y;
			playerVelocity.y = 0;
			/* Next two lines are equivalent to idTech's VectorNormalize() */
			speed = playerVelocity.magnitude;
			playerVelocity.Normalize();

			dot = Vector3.Dot(playerVelocity, wishdir);
			k = 32;
			k *= airControl * dot * dot * Time.deltaTime;

			// Change direction while slowing down
			if (dot > 0)
			{
				playerVelocity.x = playerVelocity.x * speed + wishdir.x * k;
				playerVelocity.y = playerVelocity.y * speed + wishdir.y * k;
				playerVelocity.z = playerVelocity.z * speed + wishdir.z * k;

				playerVelocity.Normalize();
				moveDirectionNorm = playerVelocity;
			}

			playerVelocity.x *= speed;
			playerVelocity.y = zspeed; // Note this line
			playerVelocity.z *= speed;

		}
	}
	/**
		* Called every frame when the engine detects that the player is on the ground
		*/
	public void GroundMove()
	{
		// Do not apply friction if the player is queueing up the next jump
		if (!wishJump)
			ApplyFriction(1.0f);
		else
			ApplyFriction(0);

		SetMovementDir();

		wishdir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		wishdir = transform.TransformDirection(wishdir);
		wishdir.Normalize();
		moveDirectionNorm = wishdir;

		wishspeed = wishdir.magnitude;
		wishspeed *= moveSpeed;

		Accelerate(wishdir, wishspeed, runAcceleration);

		// Reset the gravity velocity
		playerVelocity.y = 0;

		if (wishJump)
		{
			playerVelocity.y = jumpSpeed;
			wishJump = false;
		}

		/**
			* Applies friction to the player, called in both the air and on the ground
			*/
		void ApplyFriction(float t)
		{
			vec = playerVelocity; // Equivalent to: VectorCopy();
			vec.y = 0f;
			speed = vec.magnitude;
			drop = 0f;

			/* Only if the player is on the ground then apply friction */
			if (controller.isGrounded)
			{
				control = speed < runDeacceleration ? runDeacceleration : speed;
				drop = control * friction * Time.deltaTime * t;
			}

			newspeed = speed - drop;
			playerFriction = newspeed;
			if (newspeed < 0)
				newspeed = 0;
			if (speed > 0)
				newspeed /= speed;

			playerVelocity.x *= newspeed;
			// playerVelocity.y *= newspeed;
			playerVelocity.z *= newspeed;
		}
	}
}
