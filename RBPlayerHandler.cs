using System;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.UI;
using UnityEngine.Rendering.Universal;

public class RBPlayerHandler : MonoBehaviour
{

    [Header("MOVEMENT TYPE: 1 FOR QUAKE 1 MOVEMENT, 2 FOR QUAKE 3, 3 FOR CPMA, 4 FOR QUAKE 4 MOVEMENT, 5 FOR WARFORK MOVEMENT, 6 FOR QL GRAPPLE HOOK MOVEMENT, 7 FOR SPECTATOR.")]
    [SerializeField] public int movement_type = 1; // 1 for quake1 movement, 2 for quake 3 movement, 3 for cpma movement, 4 for quake 4 movement, 5 for warfork movement, 6 for ql grapple hook movement, 7 for noclip/spectator 

    private float QUtoUM = 0.03125f;

    [SerializeField] private Vector3 player_input_vector;
    [SerializeField] private Vector2 mouse_input;

    PlayerInput player_input;
    InputAction move_action;
    InputAction move_camera;

    InputAction jump_input;

    InputAction special_input;

    [SerializeField] private bool jump_bool;
    [SerializeField] private bool special_input_bool;

    [SerializeField] private float speed;
    [SerializeField] private float sensitivity;

    private static readonly Vector3 k_XZPlane = new Vector3(1.0f, 0.0f, 1.0f);

    [SerializeField] private Transform player_camera;
    [SerializeField] private Rigidbody rigid_body_controller;

    [SerializeField] private PhysicsMaterial physics_material;

    [SerializeField] private TextMeshProUGUI debug_text;

    [SerializeField] private Transform head_position;
    [SerializeField] private ConsoleScript console_script;

    [SerializeField] private bool is_grounded = false;

    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private LayerMask wallLayer;
    private bool is_friction_delay = true;

    private float friction = 5f;

    private bool is_on_slope = false;

    private Vector3 slope_vector = Vector3.zero;

    private bool is_wall_jumping = false;
    private Vector3 wall_jump_normal;

    private Vector2 old_velocity;

    private Vector3 grappling_point;

    private bool is_grappling;

    private SpringJoint grapple_joint;

    [SerializeField] private LineRenderer grapple_renderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player_input = GetComponent<PlayerInput>();
        move_action = player_input.actions.FindAction("Move");
        move_camera = player_input.actions.FindAction("Look");
        jump_input = player_input.actions.FindAction("Jump");
        special_input = player_input.actions.FindAction("Special");
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input_direction = move_action.ReadValue<Vector2>();
        Vector2 look_direction = move_camera.ReadValue<Vector2>();
        player_input_vector = new Vector3(input_direction.x, 0f, input_direction.y);
        //mouse_input = new Vector2(look_direction.x, look_direction.y);
        Vector3 quake_move_vector = player_input_vector.normalized;
        Vector3 ground_wish_direction = (head_position.rotation * quake_move_vector).normalized;
        float wishspeed = (transform.rotation * quake_move_vector).magnitude;
        float currentspeed = Vector3.Dot(rigid_body_controller.linearVelocity, ground_wish_direction);

        jump_bool = player_input.actions.FindAction("Jump").IsPressed();

        special_input_bool = player_input.actions.FindAction("Special").IsPressed();

        if (player_input.actions.FindAction("Special").triggered && movement_type == 6)
            {
                HookCollisionStart();
            }
        if (player_input.actions.FindAction("Special").IsPressed() && movement_type == 6)
        {
            HookCollisionPlaying();
        }
        else
        {
            is_grappling = false;
            if (grapple_joint != null)
            {
                grapple_renderer.positionCount = 0;
                Destroy(grapple_joint);
            }
            //rigid_body_controller.useGravity = true;
        }

        close_console();
        if (IsGrounded())
        {


            if (is_friction_delay == false)
            {
                rigid_body_controller.useGravity = false;
                if (rigid_body_controller.linearVelocity.magnitude > 0.5f * QUtoUM)
                {
                quake_friction();    
                }
            }

            if (jump_bool)
            {
                if (is_on_slope)
                {
                    rigid_body_controller.linearVelocity = new Vector3(rigid_body_controller.linearVelocity.x, 230f * QUtoUM + rigid_body_controller.linearVelocity.y * 0.2f, rigid_body_controller.linearVelocity.z);
                }
                else
                {
                    rigid_body_controller.linearVelocity = new Vector3(rigid_body_controller.linearVelocity.x, 230f * QUtoUM, rigid_body_controller.linearVelocity.z);
                }

            }
            if (special_input_bool && movement_type == 5)
                {
                    EnableFriction();
                    warfork_dash(input_direction, ground_wish_direction);
                    rigid_body_controller.linearVelocity = new Vector3(rigid_body_controller.linearVelocity.x, 150f * QUtoUM, rigid_body_controller.linearVelocity.z);
                }
            switch (movement_type)
            {

                default:
                    quake_accelerate(ground_wish_direction);
                    break;
                case 4:
                    if (special_input_bool)
                    {
                        quake_accelerate(ground_wish_direction);
                        friction = 1f;
                    }
                    else
                    {
                        quake_accelerate(ground_wish_direction);
                        friction = 5f;
                    }
                    break;
            }
        }
        else
        {
            is_friction_delay = true;
            rigid_body_controller.useGravity = true;
            
            if (!IsGrounded() && !is_wall_jumping && (rigid_body_controller.linearVelocity.magnitude / QUtoUM) > 10f)
            {
                old_velocity = new Vector2(rigid_body_controller.linearVelocity.x, rigid_body_controller.linearVelocity.z);
                //Debug.Log(old_velocity);
            }
            switch (movement_type)
            {
                case 1: // Q1
                    quake_air_accelerate(ground_wish_direction);
                    break;
                case 2: // Q3
                    quake3_air_accelerate(ground_wish_direction);
                    break;
                case 3: // CPMA
                    if ((input_direction.x == -1f || input_direction.x == 1f) && input_direction.y == 0f)
                    {
                        quake_air_accelerate(ground_wish_direction);
                    }
                    if ((input_direction.x == -1f || input_direction.x == 1f) && (input_direction.y == -1f || input_direction.y == 1f))
                    {
                        quake3_air_accelerate(ground_wish_direction);
                    }
                    if ((input_direction.x == 0f) && (input_direction.y == -1f || input_direction.y == 1f))
                    {
                        cpm_w_air_accelerate(ground_wish_direction);
                    }
                    break;
                case 4: // Quake 4
                    quake3_air_accelerate(ground_wish_direction);
                    break;
                case 5: // Warfork

                    if (special_input_bool && is_wall_jumping && movement_type == 5)
                    {
                        warfork_wall_dash();
                    }

                    if ((input_direction.x == -1f || input_direction.x == 1f) && input_direction.y == 0f)
                    {
                        quake_air_accelerate(ground_wish_direction);
                    }
                    if ((input_direction.x == -1f || input_direction.x == 1f) && (input_direction.y == -1f || input_direction.y == 1f))
                    {
                        quake3_air_accelerate(ground_wish_direction);
                    }
                    if ((input_direction.x == 0f) && (input_direction.y == -1f || input_direction.y == 1f))
                    {
                        cpm_w_air_accelerate(ground_wish_direction);
                    }
                    break;
                case 6: // QL/Q2 Grapple Hook Movement
                    quake3_air_accelerate(ground_wish_direction);
                    break;
                default:
                    quake_air_accelerate(ground_wish_direction);
                    break;
            }
        }
        debug_text.text = "IS JUMPING: " + jump_bool.ToString() + "\n" + "IS FRICTION DELAY: " + is_friction_delay.ToString() + "\n" + "QUAKE MOVE VECTOR" + quake_move_vector.ToString() + "\n" + "QUAKE WISH DIRECTION" + ground_wish_direction.ToString() + "\n" + "QUAKE WISH SPEED" + wishspeed.ToString() + "\n" + "QUAKE CURRENT SPEED" + currentspeed.ToString() + "\n"  + "IS GROUNDED: " + IsGrounded().ToString() + "\n" + "VELOCITY" + (rigid_body_controller.linearVelocity.magnitude / QUtoUM).ToString();
    }

    private void OnCollisionEnter(Collision other)
    {
        int layer = other.gameObject.layer;
        //if (groundLayer != (groundLayer | (1 << layer))) return;
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;

            if (IsGrounded())
                Invoke("EnableFriction", 0.01f); // enable friction late so bunnyhopping works consistently.
        }
    }

    void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
        Vector3 normal = collision.contacts[i].normal;
            if (IsSlope(normal))
            {
                is_on_slope = true;
                slope_vector = normal;
            }
            else
            {
                is_on_slope = false;
                slope_vector = Vector3.zero;
            }
        if (!IsGrounded())
            {
                wall_jump_normal = normal;
                is_wall_jumping = true;
                //clip_velocity(normal, 1f);
            }
        }             
    }

    void EnableFriction()
    {
        is_friction_delay = false;
    }

    // This magic with fixedupdate and late update are done to keep the camera from not shitting itself.

    void FixedUpdate()
    {
        if (rigid_body_controller.linearVelocity.magnitude >= (1f * QUtoUM))
        {
            Vector3 cameraEuler = player_camera.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, cameraEuler.y, 0f);
        }
    }

    void LateUpdate()
    {
        if (rigid_body_controller.linearVelocity.magnitude <= (1f * QUtoUM))
        {
            Vector3 cameraEuler = player_camera.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, cameraEuler.y, 0f);
        }
    }

    public bool IsGrounded()
    {
        Vector3 spherePosition = rigid_body_controller.position + Vector3.down * 0.93f;
        float radius = 0.3f;
        return Physics.CheckSphere(spherePosition, radius, groundLayer);
    }

    private bool IsSlope(Vector3 v) {
        float angle = Vector3.Angle(Vector3.up, v);
        if(v != Vector3.up)
            return angle < 45;
        return false;
    }

    private void close_console()
    {
        if (console_script.is_console_open)
        {
            player_input.actions.FindAction("Jump").Disable();
            player_input.actions.FindAction("Special").Disable();
            player_input.actions.FindAction("Move").Disable();
            Cursor.visible = true;
        }
        else
        {
            player_input.actions.FindAction("Jump").Enable();
            player_input.actions.FindAction("Special").Enable();
            player_input.actions.FindAction("Move").Enable();
            Cursor.visible = false;
        }

    }


    private void quake_accelerate(Vector3 ground_wish_direction)
    {

        float alignment = Vector3.Dot(rigid_body_controller.linearVelocity, ground_wish_direction.normalized);
        float addSpeed = 12f - alignment;
        if (addSpeed <= 0)
            return;

        float accelSpeed = Mathf.Min(12f * Time.deltaTime * 7, addSpeed);
        rigid_body_controller.linearVelocity += accelSpeed * Vector3.Scale(ground_wish_direction.normalized, k_XZPlane);
    }

    private void quake_air_accelerate(Vector3 ground_wish_direction)
    {


        float current_speed = Mathf.Min(ground_wish_direction.magnitude, 1.07f);
        float alignment = Vector3.Dot(rigid_body_controller.linearVelocity, ground_wish_direction);

        float addSpeed = current_speed - alignment * 2f;
        if (addSpeed <= 0.0f)
            return;

        float accelSpeed = Mathf.Min(1000f * current_speed * Time.deltaTime, addSpeed);
        rigid_body_controller.linearVelocity += ground_wish_direction * accelSpeed;
    }

    private void quake3_air_accelerate(Vector3 ground_wish_direction)
    {


        float currentspeed = Vector3.Dot(rigid_body_controller.linearVelocity, ground_wish_direction.normalized);
        float wishspeed = ground_wish_direction.magnitude;
        //float addSpeed = 11f - alignment;
        // old values
        //float addSpeed = wishspeed * 11f - currentspeed;
        //float accelSpeed = Mathf.Min(15f * Time.deltaTime, addSpeed);
        // SIX NINE!!!
        float addSpeed = wishspeed * 6f - currentspeed;
        float accelSpeed = Mathf.Min(9f * Time.deltaTime, addSpeed);
        if (addSpeed <= 0)
            return;
        rigid_body_controller.linearVelocity += accelSpeed * Vector3.Scale(ground_wish_direction.normalized, k_XZPlane);
    }

    private void cpm_w_air_accelerate(Vector3 ground_wish_direction)
    {
        float currentspeed;
        float wishspeed;
        currentspeed = Vector3.Dot(rigid_body_controller.linearVelocity, ground_wish_direction.normalized);
        wishspeed = ground_wish_direction.magnitude;
        //float addSpeed = 11f - alignment;
        float addSpeed = wishspeed * 11f - currentspeed;
        //float accelSpeed = Mathf.Min(10f * Time.deltaTime, addSpeed);
        float accelSpeed = 10f * Time.deltaTime;

        if (currentspeed > 0f && (rigid_body_controller.linearVelocity.magnitude / QUtoUM) > 400f)
        {
            Vector3 current_velocity = rigid_body_controller.linearVelocity;
            current_velocity.x = ground_wish_direction.x * currentspeed;
            current_velocity.z = ground_wish_direction.z * currentspeed;
            rigid_body_controller.linearVelocity = current_velocity;
        }
        else
        {
            rigid_body_controller.linearVelocity += accelSpeed * Vector3.Scale(ground_wish_direction.normalized, k_XZPlane);
        }
    }

    private void warfork_dash(Vector2 input_direction, Vector3 ground_wish_direction)
    {
        float currentspeed;

        if (input_direction.x == 0 && input_direction.y == 0)
        {
            ground_wish_direction = (transform.rotation * new Vector3(0, 0, 1)).normalized;
        }
        if ((rigid_body_controller.linearVelocity.magnitude / QUtoUM) > 450f)
        {
            currentspeed = rigid_body_controller.linearVelocity.magnitude;
            Vector3 current_velocity = new Vector3 (rigid_body_controller.linearVelocity.x, 0f, rigid_body_controller.linearVelocity.z);
            current_velocity.x = ground_wish_direction.x * currentspeed;
            current_velocity.z = ground_wish_direction.z * currentspeed;
            rigid_body_controller.linearVelocity = current_velocity;
        }
        if ((rigid_body_controller.linearVelocity.magnitude / QUtoUM) < 370f)
        {
            currentspeed = (370f * QUtoUM);
            Vector3 current_velocity = new Vector3 (rigid_body_controller.linearVelocity.x, 0f, rigid_body_controller.linearVelocity.z);
            current_velocity.x = ground_wish_direction.x * currentspeed;
            current_velocity.z = ground_wish_direction.z * currentspeed;
            rigid_body_controller.linearVelocity = current_velocity;
        }
    }

    private void warfork_wall_dash()
    {
        float currentspeed;
        if (is_wall_jumping && !IsGrounded())
        {
            currentspeed = old_velocity.magnitude;
            player_input_vector = new Vector3(0f, 0f, 0f);
            Vector3 current_velocity = new Vector3(rigid_body_controller.linearVelocity.x, 0f, rigid_body_controller.linearVelocity.z);
            rigid_body_controller.linearVelocity = current_velocity + (wall_jump_normal * currentspeed * 0.5f);
            rigid_body_controller.linearVelocity = new Vector3(rigid_body_controller.linearVelocity.x, 200f * QUtoUM, rigid_body_controller.linearVelocity.z);
            is_wall_jumping = false;
        }
    }

    private void quake_friction()
    {
        if (IsGrounded())
        {
            float speed = Vector3.Scale(k_XZPlane, rigid_body_controller.linearVelocity).magnitude;
            if (speed < 0.01f)
            {
                rigid_body_controller.linearVelocity = Vector3.Scale(rigid_body_controller.linearVelocity, Vector3.up);
                return;
            }

            float drop = 0.0f;
            if (IsGrounded())
            {
                float control = speed < 3.57f ? 3.57f : speed;
                drop += control * friction * Time.deltaTime;
            }

            float newSpeed = Mathf.Max(speed - drop, 0.0f) / speed;
            rigid_body_controller.linearVelocity *= newSpeed;
        }
    }

    /*private void HookCollisionStart()
    {
        Transform cameraTransform = Camera.main.transform;
        RaycastHit grapple_hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out grapple_hit, 50.0f))
        {
            grappling_point = grapple_hit.point;
        }
    }

    private void HookCollisionPlaying()
    {
        RaycastHit grapple_hit;
        Transform cameraTransform = Camera.main.transform;
        Vector3 move_vector = ((grappling_point) - cameraTransform.position);
        Vector3 grapple_direction = move_vector.normalized;
        float grapple_vector_distance = move_vector.magnitude;
        if (Physics.Raycast(cameraTransform.position, grapple_direction, out grapple_hit, grapple_vector_distance * 5f))
        {
            rigid_body_controller.useGravity = false;
            is_grappling = true;
            Debug.DrawRay(cameraTransform.position, grapple_direction * grapple_vector_distance, Color.green);
            rigid_body_controller.linearVelocity = (grapple_direction * 25f);
        }
    }*/

private void HookCollisionStart()
    {
        Transform cameraTransform = Camera.main.transform;
        RaycastHit grapple_hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out grapple_hit, 50.0f))
        {
            grappling_point = grapple_hit.point;
            grapple_joint = rigid_body_controller.gameObject.AddComponent<SpringJoint>();
            grapple_joint.autoConfigureConnectedAnchor = false;
            grapple_joint.connectedAnchor = grappling_point;

            float distance_from_point = Vector3.Distance(rigid_body_controller.position, grappling_point);
            Vector3 move_vector = ((grappling_point) - cameraTransform.position);
            Vector3 grapple_direction = move_vector.normalized;
            //rigid_body_controller.linearVelocity += (grapple_direction * 5f);
            grapple_joint.maxDistance = distance_from_point * 0.5f;
            grapple_joint.minDistance = distance_from_point * 0.25f;

            grapple_joint.spring = 4.5f;
            grapple_joint.damper = 2f;
            grapple_joint.massScale = 10f;

            grapple_renderer.positionCount = 2;
        }
    }

    private void HookCollisionPlaying()
    {
        //RaycastHit grapple_hit;
        Transform cameraTransform = Camera.main.transform;
        Vector3 move_vector = ((grappling_point) - cameraTransform.position);
        Vector3 grapple_direction = move_vector.normalized;
        float grapple_vector_distance = move_vector.magnitude;

        grapple_renderer.SetPosition(0, cameraTransform.position);
        grapple_renderer.SetPosition(1, grappling_point);
        /*if (Physics.Raycast(cameraTransform.position, grapple_direction, out grapple_hit, grapple_vector_distance * 5f))
        {
        }*/
    }


    private void clip_velocity(Vector3 normal, float overbounce)
    {
        var backoff = Vector3.Dot(rigid_body_controller.linearVelocity, normal) * overbounce;

        if (backoff >= 0)
        {
            return;
        }

        var change = normal * backoff;

        rigid_body_controller.linearVelocity -= change;
        var adjust = Vector3.Dot(rigid_body_controller.linearVelocity, normal);

        if (adjust < 0.0f)
        {
            rigid_body_controller.linearVelocity += normal * adjust * Time.deltaTime;
        }

    }

}
