﻿using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CharacterController2D : MonoBehaviour
{
    [SerializeField] private GameObject knife;                                      // The player's weapon
    [SerializeField] private Transform catchPos;
    [SerializeField] private Vector2 catchBoxSize;
    [SerializeField] private float m_JumpVel = .13f;                                // Upward jumping force
    [SerializeField] private float m_JumpHeight = 10;                               // How high to jump **note this is an inverse value. the higher the m_JumpHeight
    [SerializeField] private float m_AirStop = 1;                                   // How quickly the player stops going up after space is released
    [SerializeField] private bool m_AirControl = false;                             // Whether or not a player can steer while jumping;
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;              // Amount of maxSpeed applied to crouching movement. 1 = 100%
    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;      // How much to smooth out the movement
    [SerializeField] private LayerMask m_WhatIsGround;                              // A mask determining what is ground to the character
    [SerializeField] private Transform m_GroundCheck;                               // A position marking where to check if the player is grounded.
    [SerializeField] private Transform m_CeilingCheck;                              // A position marking where to check for ceilings
    [SerializeField] private Collider2D m_CrouchDisableCollider;                    // A collider that will be disabled when crouching

    [Header("ParticleSystems")]
    [Space]

    [SerializeField] ParticleSystem runParticles;

    const float k_GroundRadius = .2f;               // Radius of the overlap circle to determine if the player is grounded
    const float k_CeilingRadius = .2f;              // Radius of the overlap circle to determine if the player can stand up
    private Rigidbody2D m_Rigidbody2D;              // Player rigid body
    private bool m_FacingRight = true;              // For determining which way the player is currently facing.
    private Vector3 m_Velocity = Vector3.zero;      // Velocity of player
    private bool m_Attacking;                       // If the player is in an attack animation
    private bool m_Grounded;                        // Whether or not the player is grounded.
    private float jumpTime = 0;                     // How long the player has been in the air
    private Animator animator;                      // Controlls the animations for the player
    private SoundManager soundManager;
    private Knife knifeController;



    [Header("Events")]
    [Space]

    public UnityEvent OnJumpEvent;
    public UnityEvent OnLandEvent;
    public UnityEvent OnThrowEvent;
    public UnityEvent OnCatchEvent;
    public UnityEvent OnPickupEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    public BoolEvent OnCrouchEvent;
    private bool m_wasCrouching = false;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        knifeController = knife.GetComponent<Knife>();
        soundManager = GetComponentInChildren<SoundManager>();

        if (OnJumpEvent == null)
            OnJumpEvent = new UnityEvent();

        if (OnLandEvent == null)
            OnLandEvent = new UnityEvent();

        if (OnThrowEvent == null)
            OnThrowEvent = new UnityEvent();

        if (OnCatchEvent == null)
            OnCatchEvent = new UnityEvent();

        if (OnPickupEvent == null)
            OnPickupEvent = new UnityEvent();

        if (OnCrouchEvent == null)
            OnCrouchEvent = new BoolEvent();
    }

    private void FixedUpdate()
    {
        bool wasGrounded = m_Grounded;

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundRadius, m_WhatIsGround);

        if (colliders.Length > 0)
        {
            m_Grounded = true;
            if (!wasGrounded)
                OnLandEvent.Invoke();
        }
        else
        {
            m_Grounded = false;
        }
    }


    public void Move(float move, bool crouch, bool jump)
    {
        // If crouching, check to see if the character can stand up
        if (!crouch)
        {
            // If the character has a ceiling preventing them from standing up, keep them crouching
            if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
            {
                crouch = true;
            }
        }

        //only control the player if grounded or airControl is turned on
        if (m_Grounded || m_AirControl)
        {

            // If crouching
            if (crouch && m_Grounded)
            {
                if (!m_wasCrouching)
                {
                    m_wasCrouching = true;
                    OnCrouchEvent.Invoke(true);
                }

                // Reduce the speed by the crouchSpeed multiplier
                move *= m_CrouchSpeed;

                // Disable one of the colliders when crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = false;
            }
            else
            {
                // Enable the collider when not crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = true;

                if (m_wasCrouching)
                {
                    m_wasCrouching = false;
                    OnCrouchEvent.Invoke(false);
                }
            }

            Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("KnifeThrow") && m_Grounded)
            {
                targetVelocity = Vector2.zero;
            }

            // And then smoothing it out and applying it to the character
            m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

            // If the input is moving the player right and the player is facing left...
            if (move > 0 && !m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (move < 0 && m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
        }

        // Lots of conditions for the player to go up
        if (jump)
        {
            if (jumpTime == 0)
            {
                OnJumpEvent.Invoke();
            }

            jumpTime += Time.fixedDeltaTime;

            float jumpVel = LinearVelDec(jumpTime, m_JumpHeight);

            //controlls the upward jumping force
            if (jumpVel > 0 && m_Rigidbody2D.velocity.y > -0.1)
            {
                m_Rigidbody2D.AddForce(new Vector2(0, jumpVel * m_JumpVel));
            }
            //if force would be negative, instead it is 0
            else
            {
                m_Rigidbody2D.AddForce(Vector2.zero);
            }
        }

        //snappier falls
        else if (!jump && m_Rigidbody2D.velocity.y > .01)
        {
            m_Rigidbody2D.AddForce(new Vector2(0, -m_AirStop));
        }

        //reset jumpTime
        if (m_Grounded && !jump)
        {
            jumpTime = 0;
        }

        animator.SetFloat("velocityY", m_Rigidbody2D.velocity.y);
        animator.SetBool("isGrounded", m_Grounded);

        if (Mathf.Abs(m_Rigidbody2D.velocity.x) > .1)
        {
            animator.SetBool("isRunning", true);
        }
        else if (Mathf.Abs(m_Rigidbody2D.velocity.x) < .1)
        {
            animator.SetBool("isRunning", false);
        }


    }

    void PlayWalkSound()
    {
        soundManager.PlaySound("Walk");
    }

    public void PlayWalkParticles()
    {
        runParticles.Play();
    }
    public void StopWalkParticles()
    {
        runParticles.Stop();
    }

    public void Attack(Vector2 throwVelocity, float returnSpeed, float catchDistance, bool isThrowing, bool isReturning)
    {
        float distanceFromKnife = Vector3.Magnitude(catchPos.position - knife.transform.position);

        if (isThrowing && !knifeController.GetThrown())
        {
            if (!m_FacingRight)
                throwVelocity = new Vector2(-throwVelocity.x, throwVelocity.y);
            StartCoroutine(knifeController.KnifeThrow(throwVelocity));
            OnThrowEvent.Invoke();
        }
        else if (isReturning && knifeController.GetThrown() && !knifeController.GetReturning())
        {
            StartCoroutine(knifeController.KnifeReturn(returnSpeed));
        }

        if (distanceFromKnife < catchDistance)
        {
            if (knifeController.GetReturning())
                OnCatchEvent.Invoke();
            else if (knifeController.GetLanded())
                OnPickupEvent.Invoke();
        }
    }

    //
    //functins for controlling air motion
    //

    //Linear foce decrease
    float LinearVelDec(float time, float slope)
    {
        return -slope * time + 1;
    }
    //sigmoid force decrease
    float SigmoidVelDec(float time)
    {
        return -1 / (1 + Mathf.Pow(20000, -time + .5f)) + 1;
    }

    // Switch the way the player is labelled as facing.
    private void Flip()
    {
        m_FacingRight = !m_FacingRight;

        transform.rotation *= Quaternion.Euler(0, 180, 0);
    }
}
