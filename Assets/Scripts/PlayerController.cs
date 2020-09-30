using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private float walkSpeed = 2;
    [SerializeField] private float runSpeed = 3;
    [SerializeField] private bool lockCursor = true;

    [SerializeField] private float speedSmoothTime = 0.1f;

    // roughly number of seconds to go from current angle to the target angle.
    [SerializeField] private float turnSmoothTime = 0.2f;
    [SerializeField] private float gravity = -12;

    [SerializeField] private float jumpHeight = 1;

    // how much x,z control do we give the player while character is in air 
    [Range(0, 1)] [SerializeField] private float airControlPercent;

    private Animator _animator;
    private float _turnSmoothVelocity;
    private float _speedSmoothVelocity;
    private float _currentSpeed;
    
    private Transform mainCamera;
    
    private CharacterController controller;
    private float _velocityYAxis;

    private void Start()
    {
        this._animator = GetComponent<Animator>();
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        // need the camera transform in order to move in direction of camera
        mainCamera = Camera.main.transform;
        
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // ========================
        // Input
        // ========================
        // create a vector2 for the keyboard input (x,z).  y is handed separately to allow 
        //   for jumping and gravity
        // --------------------------------------------------------- y is actually z here
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        // turn the input vector into a direction
        //   "When normalized, a vector keeps the same direction but its length is 1.0"
        Vector2 inputDirection = input.normalized;
        
        //
        
        
        bool running = Input.GetKey(KeyCode.LeftShift);
        Move(inputDirection, running);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }
        
        // ========================
        // Animator
        // see: https://www.youtube.com/watch?v=ZwD1UHNCzOc&list=PLXXlQcsWSuUUxIR9opITwTDKQMmuviNDe&index=2
        // ========================
        // calculate the animator's percent used to blend from walk to run
        float animationSpeedPercent = ((running)
            ? _currentSpeed / runSpeed
            : _currentSpeed / walkSpeed * .5f);
        _animator.SetFloat(
            "speedPercent",
            animationSpeedPercent,
            speedSmoothTime,
            Time.deltaTime);
    }

    void Move(Vector2 inputDirection, bool running)
    {
        // determine the character's rotation
        // (-) = atan(y/x) but in unity we rotate anticlockwise 90deg, so
        // r = 90 - (-), or r = atan(x/y)
        // below we could have done Mathf.Atan(input_direction.x/input_direction.y) but Atan2 with 2
        //   params takes care of division by zero
        // ------------------------------------------------------------------ y is actually z here
        Vector3 moveDirection = Vector3.one;
        if (inputDirection != Vector2.zero)
        {
            // adding 
            float targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.y)
                                   * Mathf.Rad2Deg
                                   // this causes the play's forward movement to be that of the camera
                                   + this.mainCamera.eulerAngles.y;
            
            //
            moveDirection = Quaternion.Euler(0, targetRotation, 0) * Vector3.forward;
            
            // Note in brackeys video they us
            // transform.rotation = Quaternion.Euler(0, targetAngle, 0)
            // transform.eulerAngles is susceptible to gimble lock, but ok if rotating on one axis as
            //    we are doing here
            transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(
                transform.eulerAngles.y, // current angle
                targetRotation, // target angle
                ref _turnSmoothVelocity, // allow function to reference the _turnSmoothVelocity var
                GetModifiedSmoothTime(turnSmoothTime) // time in sec to perform rotation
            );
        }
        
        // is inputDirection.magnitude is 0, speed is zero, else the inputDirection.magnitude will be
        //   1, which will not change the speed
        float targetSpeed = ((running) ? this.runSpeed : this.walkSpeed) * inputDirection.magnitude;
        _currentSpeed = Mathf.SmoothDamp(
            _currentSpeed,
            targetSpeed,
            ref _speedSmoothVelocity,
            GetModifiedSmoothTime(speedSmoothTime));


        // move the character in the direction they are facing in worldspace
        // adjust y velocity for gravity
        _velocityYAxis += Time.deltaTime * gravity;
        // combine (x,z) and y velocities                           gravity adjustment
        Vector3 velocity = moveDirection.normalized * _currentSpeed + Vector3.up * _velocityYAxis;
        controller.Move(velocity * Time.deltaTime);

        // update character currentSpeed to actual speed (e.g. 0 if collided with wall)
        // get the speed (.magnitude) in the x,z plane
        _currentSpeed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;


        // if we are on ground reset our velocity in y direction to zero 
        if (controller.isGrounded)
        {
            _velocityYAxis = 0;
        }

        
    }
    private void Jump()
    {
        if (controller.isGrounded)
        {
            // determine jump velocity to allow us to attain jump height
            //                   Kinematic eq; see: https://www.youtube.com/watch?v=v1V3T5BPd7E
            float jumpVelocity = Mathf.Sqrt(-2 * gravity * jumpHeight);
            _velocityYAxis = jumpVelocity;
        }
    }

    /// <summary>
    /// Modify the smooth time used for rotation and movement bases on if the character is airborne
    /// </summary>
    /// <param name="smoothTime"></param>
    /// <returns></returns>
    private float GetModifiedSmoothTime(float smoothTime)
    {
        if (controller.isGrounded)
        {
            return smoothTime;
        }

        // protect against division by 0
        if (airControlPercent == 0)
        {
            return float.MaxValue;
        }

        // less the percent, greater smoothTime will be thus slowing response for rotation and movement
        return smoothTime / airControlPercent;
    }
}