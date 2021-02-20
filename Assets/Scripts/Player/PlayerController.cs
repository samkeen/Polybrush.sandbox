using System;
using System.Collections;
using System.Collections.Generic;
using Dialogue;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

// ReSharper disable InconsistentNaming

/// <summary>
/// Inspired by Sebastian's youtube series:
/// https://www.youtube.com/watch?v=sNmeK3qK7oA&list=PLFt_AvWsXl0djuNM22htmz3BUtHHtOh7v&index=8
/// And converted to Cinemachine with Brackeys video
/// https://www.youtube.com/watch?v=4HpC--2iowE&t=344s
/// </summary>
public class PlayerController : MonoBehaviour
{
    // these are the 'steady-state' speed for walk and run.  We accelerate/decelerate
    // to and from them.
    [SerializeField] private float walkSpeed = 2;
    [SerializeField] private float runSpeed = 3;
    // This is the current speed.  Held between this.walkSpeed and this.runSpeed
    private float _currentHorizontalSpeed;
    //
    private float _currentVerticalSpeed;

    [SerializeField] private bool lockCursor = true;

    // Damping functions are used to go from current to target speed|angle
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private float turnDampTime = 0.2f;

    [SerializeField] private float gravity = -12;

    [SerializeField] private float jumpHeight = 1;

    // how much x,z control do we give the player while character is in air 
    [Range(0, 1)] [SerializeField] private float airControlPercent;

    private Animator _animator;


    private Transform mainCamera;

    private CharacterController characterController;


    private bool isInDialogue;

    private void OnDialogueStart()
    {
        isInDialogue = true;
    }

    private void OnDialogueEnd()
    {
        isInDialogue = false;
    }

    private void Start()
    {
        _animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        // listen for dialog start and end
        FindObjectOfType<DialogEvents>().DialogueStart += OnDialogueStart;
        FindObjectOfType<DialogEvents>().DialogueEnd += OnDialogueEnd;
        // 
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // need the camera transform in order to move in direction of camera
        mainCamera = Camera.main.transform;
    }

    void Update()
    {
        var isRunning = Input.GetKey(KeyCode.LeftShift);
        Move(isRunning);
        if (!isInDialogue)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
        }

        Animate(isRunning);
    }

    private void Animate(bool running)
    {
        // ========================
        // Animator
        // see: https://www.youtube.com/watch?v=ZwD1UHNCzOc&list=PLXXlQcsWSuUUxIR9opITwTDKQMmuviNDe&index=2
        // ========================
        // calculate the animator's percent used to blend from walk to run
        float animationSpeedPercent = 0; // leave at zero if were are inDialogue
        if (!isInDialogue)
        {
            animationSpeedPercent = running
                ? _currentHorizontalSpeed / runSpeed
                : _currentHorizontalSpeed / walkSpeed * .5f;
        }

        _animator.SetFloat(
            "speedPercent",
            animationSpeedPercent,
            speedDampTime,
            Time.deltaTime);
    }

    private static Vector2 DetermineInputDirection()
    {
        // ========================
        // Input
        // ========================
        // create a vector2 for the keyboard input (x,z).  y is handed separately to allow 
        //   for jumping and gravity
        // --------------------------------------------------------- y is actually z here
        var input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        // turn the input vector into a direction
        //   "When normalized, a vector keeps the same direction but its length is 1.0"
        return input.normalized;
    }

    // 1. determine the direction indicated by input device
    // 2. if we've entered a dialog while in a jump, simply allow jump to finish (no other movement)
    // 3. else, calculate the current damped horizontal speed
    // 4.       Move character (characterController.Move())
    // 5. if isGrounded, anchor vertical speed to 0
    private void Move(bool isRunning)
    {
        // this is the direction the input device is indicating in the x/z plane 
        var inputDirection = DetermineInputDirection();
        if (!characterController.isGrounded && isInDialogue) // finish jump if entered dialog in midair
        {
            _currentVerticalSpeed += Time.deltaTime * gravity;
            characterController.Move(Vector3.up * (_currentVerticalSpeed * Time.deltaTime));
        }
        else // perform full movement calculations
        {
            _currentHorizontalSpeed = GetDampedCurrentSpeed(inputDirection.magnitude, isRunning);
            ApplyMovement(inputDirection);
            // update character _currentHorizontalSpeed to actual speed (e.g. 0 if collided with wall)
            // without this, if you stall at a wall while pressing fwd, _currentHorizontalSpeed stays
            // at the value for walk|runSpeed
            _currentHorizontalSpeed =
                new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;
        }

        // if we are on ground reset our velocity in y direction to zero 
        if (characterController.isGrounded)
        {
            _currentVerticalSpeed = 0;
        }
    }

    private void ApplyMovement(Vector2 inputDirection)
    {
        var inputIndicatedHorizontalMoveDirection = CalculateHorizontalMoveDirection(inputDirection);
        // move the character in the direction they are facing in worldspace
        // adjust y velocity for gravity (gravity is negative)
        _currentVerticalSpeed += Time.deltaTime * gravity;
        // combine horizontal (x,z) and vertical (y) velocities
        var velocity = inputIndicatedHorizontalMoveDirection.normalized * _currentHorizontalSpeed
            + Vector3.up * _currentVerticalSpeed;
        characterController.Move(velocity * Time.deltaTime);
    }

    private Vector3 CalculateHorizontalMoveDirection(Vector2 inputDirection)
    {
        // If no indicated direction input, return (1,1,1)
        if (inputDirection == Vector2.zero)
        {
            return Vector3.one;
        }

        // Calculating the angle between where we are facing and the indicated input direction
        // See README:CalculateMoveDirection
        var targetInputAngle = Mathf.Atan2(inputDirection.x, inputDirection.y) * Mathf.Rad2Deg;
        // The rotation as Euler angles in degrees 
        var cameraYAxisRotationAngle = mainCamera.eulerAngles.y;
        // [+ cameraZAngle] causes the play's forward movement to be that of the camera
        // targetInputAngle is 0 if there is no movement input, thus targetYAxisRotationAngle will simply be cameraZAngle; the
        // direction the camera is facing.
        var blendedCameraAndInputYAxisRotationAngle = targetInputAngle + cameraYAxisRotationAngle;
        Debug.Log(
            $"Camera Y Axis rotation angle: {cameraYAxisRotationAngle} :: targetInputAngle: {targetInputAngle} :: blendedCameraAndInputYAxisRotationAngle: {blendedCameraAndInputYAxisRotationAngle}");
        // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        // https://docs.unity3d.com/2020.2/Documentation/Manual/class-Quaternion.html
        //
        // see if we can convert to one of these
        // Unity’s Quaternion class has a number of functions which allow you to create and manipulate rotations
        // without needing to use Euler angles at all, and these are the ones you should use in most typical cases.
        // Each of these links to the Script Reference with code samples:
        //
        // Creating Rotations:
        // Quaternion.LookRotation
        // Quaternion.Angle
        // Quaternion.AngleAxis
        // Quaternion.FromToRotation
        // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        // [* Vector3.forward] converts from Rotation to Direction (https://youtu.be/4HpC--2iowE?t=969)
        // Example: pre:(0.0, -0.2, 0.0, 1.0), post(multiply by Vector3.forward): (-0.4, 0.0, 0.9)
        var moveDirection = Quaternion.Euler(0, blendedCameraAndInputYAxisRotationAngle, 0) * Vector3.forward;
        ApplyDampedYAxisRotation(blendedCameraAndInputYAxisRotationAngle);
        Debug.Log($"Keyboard input [{inputDirection}], returning moveDirection: {moveDirection}");
        return moveDirection;
    }

    private float _speedSmoothVelocity;


    private float GetDampedCurrentSpeed(float inputDirectionMagnitude, bool running)
    {
        return Mathf.SmoothDamp(
            _currentHorizontalSpeed,
            // if inputDirectionMagnitude is 0, speed is zero, else the inputDirection.magnitude will be
            //   1, which will not change the speed
            (running ? runSpeed : walkSpeed) * inputDirectionMagnitude,
            ref _speedSmoothVelocity,
            AirborneAdjustedDampingTime(speedDampTime));
    }

    // https://docs.unity3d.com/2020.2/Documentation/ScriptReference/Transform-eulerAngles.html
    // Note: Never rely on reading the .eulerAngles to increment the rotation
    //   The rotation as Euler angles in degrees; represents rotation in world space (vs  Transform.localEulerAngles)
    // transform.eulerAngles is susceptible to gimble lock, but ok if rotating on one axis as
    //    we are doing here
    private float _turnSmoothVelocity;

    private void ApplyDampedYAxisRotation(float targetYAxisRotationAngle)
    {
        // https://docs.unity3d.com/2020.2/Documentation/ScriptReference/Mathf.SmoothDampAngle.html
        // Gradually changes an angle given in degrees towards a desired goal angle over time.
        var dampedYAxisRotationAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y, // current angle
            targetYAxisRotationAngle, // target angle
            ref _turnSmoothVelocity, // allow function to reference the _turnSmoothVelocity var
            // time varies on if airport or grounded
            AirborneAdjustedDampingTime(turnDampTime)); // time in sec to perform rotation

        // https://docs.unity3d.com/ScriptReference/Transform-eulerAngles.html
        // This sets the angle of rotation about the axis
        transform.eulerAngles = Vector3.up * dampedYAxisRotationAngle;
    }

    private void Jump()
    {
        if (characterController.isGrounded)
        {
            // determine jump velocity to allow us to attain jump height
            //                   Kinematic eq; see: https://www.youtube.com/watch?v=v1V3T5BPd7E
            float jumpVelocity = Mathf.Sqrt(-2 * gravity * jumpHeight);
            _currentVerticalSpeed = jumpVelocity;
        }
    }

    // Modify the smooth time used for rotation and movement based on if the character is airborne
    private float AirborneAdjustedDampingTime(float smoothTime)
    {
        if (characterController.isGrounded)
        {
            return smoothTime;
        }

        return airControlPercent == 0 // protect against division by 0
            ? float.MaxValue
            // less the percent, greater smoothTime will be thus slowing response for rotation and movement
            : smoothTime / airControlPercent;
    }

    private void OnDisable()
    {
        var dialogueEvents = FindObjectOfType<DialogEvents>();
        if (dialogueEvents != null)
        {
            dialogueEvents.DialogueStart += OnDialogueStart;
            dialogueEvents.DialogueEnd += OnDialogueEnd;
        }
    }
}