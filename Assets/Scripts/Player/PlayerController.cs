using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField] MeshCollider collider;

    Vector3 startPos;
    Quaternion startRotation;

    [SerializeField] float linearAcceleration = 1f;
    [SerializeField] float slowDownSpeed = 1f;
    public Vector3 currentVelocity;
    public float currentSpeed;

    [SerializeField] float turnMin;
    [SerializeField] float turnMax;
    float turnSpeed;

    [SerializeField] float groundAngularAcceleration = 10f;
    [SerializeField] float groundAngularVelocity = 7.5f;

    [SerializeField] float airAngularAcceleration = 10f;
    [SerializeField] float airAngularVelocity = 2f;


    [SerializeField] float maxGroundSpeed = 1f;
    [SerializeField] float groundSpeedExtra = 1f;
    [SerializeField] float duckSpeedMultiplier = 1.5f;
    float maxDuckGroundSpeed;
    float maxCurrentGroundSpeed;

    [SerializeField] float maxAirVelocity = 50f;

    [SerializeField] float jumpForceMultiplier = 10f;
    [SerializeField] float jumpMaxTime = 1f;
    float jumpTimer;
    private bool spaceDown;
    private bool lastFrameSpaceDown;

    private float verticalInput;
    private float horizontalInput;

    [SerializeField] float groundDistanceCutoff = 0.1f;
    [SerializeField] float onGroundDistance = 0.05f;

    [SerializeField] Transform rightTransform;
    RaycastHit right;
    RaycastHit mid;
    [SerializeField] Transform leftTransform;
    RaycastHit left;

    public bool goofy;
    public bool goingUp;

    float startingDrag;
    float fastDrag;

    [SerializeField] float groundAngularDrag = 7.5f;
    [SerializeField] float airAngularDrag = 2f;

    float steepness;

    public bool onGround;
    public bool inAir;

    bool leftShiftDown;

    bool eDown;
    bool qDown;

    [SerializeField] float brakeDrag;
    public bool braking;
    public int brakeTimer;

    // test
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();

        onGround = false;
        inAir = false;

        rb.maxLinearVelocity = maxAirVelocity;

        startingDrag = rb.drag;
        fastDrag = startingDrag * 0.75f;

        goofy = false;
        goingUp = false;

        jumpTimer = 0f;

        brakeTimer = 0;

        currentSpeed = 0;

        startPos = transform.position;
        startRotation = transform.rotation;

        //maxDuckGroundSpeed = maxGroundSpeed * duckSpeedMultiplier;
    }

    private void HandleJump()
    {
        if (onGround)
        {
            if (spaceDown)
            {
                jumpTimer += Time.deltaTime;
            }

            if (!spaceDown && lastFrameSpaceDown)
            {
                if (jumpTimer > jumpMaxTime)
                {
                    jumpTimer = jumpMaxTime;
                }

                rb.AddForce(Vector3.up * jumpTimer * jumpForceMultiplier);

                Debug.Log("Jumped at: " + jumpTimer);

                jumpTimer = 0f;

            }
        }
    }

    private void Brake()
    {
        //transform.Rotate(transform.up, 90f * Time.deltaTime);
        //Vector3 newRot = Vector3.RotateTowards(transform.rotation.eulerAngles.normalized, brakeRotation.eulerAngles.normalized, 90 * Time.deltaTime * 3, 0f);


        if (brakeTimer < 90)
        {
            transform.Rotate(0, 1, 0);
            brakeTimer += 1;
            rb.drag += brakeDrag / 90;
        }

        //transform.localEulerAngles = newRot;

        //if (transform.rotation == brakeRotation)
        //{
        //    rb.drag = brakeDrag;
        //    rotated = true;
        //}
        
    }

    private void UnBrake()
    {
        if (brakeTimer > 0)
        {
            transform.Rotate(0, -1, 0);
            brakeTimer += -1;
            rb.drag -= brakeDrag / 90;
        }

        if (brakeTimer <= 0)
        {
            braking = false;
            brakeTimer = 0;
        }

        //transform.localEulerAngles = newRot;

        //if (transform.rotation == brakeRotation)
        //{
        //    rb.drag = brakeDrag;
        //    rotated = true;
        //}
    }

    float CalculateSteepness()
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, mid.normal);

        float angleX = rotation.eulerAngles.x;
        float angleY = rotation.eulerAngles.y;
        float angleZ = rotation.eulerAngles.z;

        float diffX;
        if (angleX >= 0 && angleX <= 180) // positive rotation on axis
        {
            diffX = angleX;
        }
        else if (angleX < 0)
        {
            diffX = 0f;
        }
        else // negative rotation on axis
        {
            diffX = 360 - angleX;
        }

        float diffY;
        if (angleY >= 0 && angleY <= 180) // positive rotation on axis
        {
            diffY = angleY;
        }
        else if (angleY < 0)
        {
            diffY = 0f;
        }
        else // negative rotation on axis
        {

            diffY = 360 - angleY;
        }

        float diffZ;
        if (angleZ >= 0 && angleZ <= 180) // positive rotation on axis
        {
            diffZ = angleZ;
        }
        else if (angleZ < 0)
        {
            diffZ = 0f;
        }
        else // negative rotation on axis
        {
            diffZ = 360 - angleZ;
        }

        float finalSteepness = (diffX + diffY + diffZ) / 3f;

        return finalSteepness;

    }

    float CalculateMaxSpeed()
    {
        float maxSpeed = maxGroundSpeed + (groundSpeedExtra * steepness);

        if (verticalInput > 0)
        {
            maxSpeed *= duckSpeedMultiplier;
        }

        return maxSpeed;
    }

    void SnapToGround()
    {
        Vector3 downVector = -transform.up * (mid.distance - onGroundDistance);
        //Vector3 downVector = -transform.up * mid.distance;
        // this check is just to make sure it doesnt snape to anything that might be 2000 metres below the snowboard if it clips through the ground for a frame
        if (downVector.magnitude < 5)
        {
            transform.position += downVector;
        }

        //if (mid.normal != transform.up)
        //{
        //    Vector3 diff = mid.normal - transform.up;
        //    transform.Rotate(diff);
        //}

        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, mid.normal) * transform.rotation;

        Quaternion newRot = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime);

        transform.rotation = newRot;

        //transform.rotation = targetRotation;

        //Debug.Log("snapped to ground");
    }

    private void SwitchToOnGroundMode()
    {
        inAir = false;
        onGround = true;
        rb.maxAngularVelocity = groundAngularVelocity;
        //collider.isTrigger = true;
    }

    private void SwitchToInAirMode()
    {
        onGround = false;
        inAir = true;
        rb.maxAngularVelocity = airAngularVelocity;
        //collider.isTrigger = false;
    }

    private void UpdateTurnSpeed()
    {
        turnSpeed = Mathf.Lerp(turnMax, turnMin, rb.velocity.magnitude / 60f);
    }

    private void UpdateUpDown()
    {
        float dotProduct = Vector3.Dot(rb.velocity.normalized, Vector3.up);

        if (dotProduct > 0)
        {
            goingUp = true;
        }
        else
        {
            goingUp = false;
        }

        if (goingUp && rb.velocity.magnitude < 0.1)
        {
            SwitchStance();
        }
    }

    private void UpdateStance()
    {
        // LOGIC HERE TO SEE WHAT STANCE THE PLAYER IS CURRENTLY
        float angleToForward = Vector3.Angle(rb.velocity, transform.forward);
        float angleToBackward = Vector3.Angle(rb.velocity, -transform.forward);

        if (angleToForward < angleToBackward)
        {
            goofy = false;
        }
        else if (angleToBackward < angleToForward)
        {
            goofy = true;
        }
    }

    private void SwitchStance()
    {
        goofy = !goofy;
    }

    private void Update()
    {
        // get button presses

        // vertical input
        verticalInput = Input.GetAxis("Vertical");
        // horizontal input
        horizontalInput = Input.GetAxis("Horizontal");
        // shift press
        if (Input.GetKey(KeyCode.LeftShift))
        {
            leftShiftDown = true;
        }
        else
        {
            leftShiftDown = false;
        }

        if (verticalInput > 0 && onGround)
        {
            rb.drag = fastDrag;
            rb.maxAngularVelocity = 3f;
        }
        else if (!braking)
        {
            rb.drag = startingDrag;
            rb.maxAngularVelocity = 1.75f;
        }

        if (verticalInput < 0 && rb.velocity.magnitude > 10f && onGround)
        {
            braking = true;
            Brake();
        }

        if (verticalInput >= 0)
        {
            UnBrake();
        }

        //if (Input.GetKey(KeyCode.LeftControl) && rb.velocity.magnitude > 10f && onGround)
        //{
        //    braking = true;
        //    Brake();
        //}

        //if (!Input.GetKey(KeyCode.LeftControl))
        //{
        //    UnBrake();
        //}

        if (Input.GetKey(KeyCode.E))
        {
            eDown = true;
        }
        else
        {
            eDown = false;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            qDown = true;
        }
        else
        {
            qDown = false;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            spaceDown = true;
        }
        else
        {
            spaceDown = false;
        }

        HandleJump();

        // calculate steepness
        steepness = CalculateSteepness();
        // calculate maximum speed 
        maxCurrentGroundSpeed = CalculateMaxSpeed();

        //Debug.Log("Steepness: " + steepness);
        //Debug.Log("Max ground speed: " + maxCurrentGroundSpeed);
        //Debug.Log("Current speed: " + rb.velocity.magnitude);
        //Debug.Log("RB max linear velocity: " + rb.maxLinearVelocity);

        // cast raycasts downwards from 3 points of the board
        // to detect wether it is on the ground or not

        // 0 distance means its infinite.
        Physics.Raycast(rightTransform.position, -transform.up, out right, Mathf.Infinity);
        Physics.Raycast(transform.position, -transform.up, out mid, Mathf.Infinity);
        Physics.Raycast(leftTransform.position, -transform.up, out left, Mathf.Infinity);

        int onGroundparts = 0;
        if (right.distance > 0 && right.distance < groundDistanceCutoff)
        {
            onGroundparts++;
        }
        if (mid.distance > 0 && mid.distance < groundDistanceCutoff)
        {
            onGroundparts++;
        }
        if (left.distance > 0 && left.distance < groundDistanceCutoff)
        {
            onGroundparts++;
        }

        if (onGroundparts > 1)
        {
            // if not on ground mode, switch
            if (!onGround)
            {
                SwitchToOnGroundMode();
            }
        }
        else
        {
            // if not in air mode, switch
            if (!inAir)
            {
                SwitchToInAirMode();
            }
        }


        // TODO: IDK TWEAK THIS SOMEHOW IT JUST DOESNT FEEL RIGHT
        if (onGround)
        {
            if (rb.maxLinearVelocity < maxCurrentGroundSpeed)
            {
                rb.maxLinearVelocity += slowDownSpeed * Time.deltaTime;
            }
            if (rb.maxLinearVelocity > maxCurrentGroundSpeed)
            {
                rb.maxLinearVelocity -= slowDownSpeed * Time.deltaTime;
            }

            rb.angularDrag = groundAngularDrag;

            UpdateStance();
            UpdateTurnSpeed();
            SnapToGround();
        }

        //Debug.Log("Current turn speed: " + turnSpeed);

        if (inAir)
        {
            if (rb.maxLinearVelocity < maxAirVelocity)
            {
                rb.maxLinearVelocity += slowDownSpeed * Time.deltaTime;
            }
            if (rb.maxLinearVelocity > maxAirVelocity)
            {
                rb.maxLinearVelocity -= slowDownSpeed * Time.deltaTime;
            }

            rb.angularDrag = airAngularDrag;
        }

        lastFrameSpaceDown = spaceDown;

        currentVelocity = rb.velocity;
        currentSpeed = rb.velocity.magnitude;

        if (Input.GetKey(KeyCode.R))
        {
            transform.position = startPos;
            transform.rotation = startRotation;
            rb.velocity = new Vector3(0, 0, 0);
        }

        Debug.Log("Down distance: " + mid.distance);
    }

    void FixedUpdate()
    {
        // handle rotation on ground
        if (onGround)
        {
            //transform.Rotate(0, horizontalInput * groundAngularAcceleration, 0);

            rb.AddTorque(transform.up * horizontalInput * groundAngularAcceleration);

            // make velocity vector face correct direction
            if (!goofy)
            {
                if (rb.velocity.normalized != transform.forward.normalized)
                {
                    rb.velocity = Vector3.RotateTowards(rb.velocity, transform.forward, Time.deltaTime * turnSpeed, 0f);
                }
            }
            else
            {
                if (rb.velocity.normalized != -transform.forward.normalized)
                {
                    rb.velocity = Vector3.RotateTowards(rb.velocity, -transform.forward, Time.deltaTime * turnSpeed, 0f);
                }
            }
            Debug.DrawRay(transform.position, rb.velocity);
        }


        // handle X/Y/Z rotation in air
        if (inAir)
        {
            if (!goofy && leftShiftDown)
            {
                rb.AddTorque(transform.right * verticalInput * airAngularAcceleration);
            }
            else if (leftShiftDown)
            {
                rb.AddTorque(transform.right * -verticalInput * airAngularAcceleration);
            }

            if (!goofy)
            {
                if (eDown)
                {                    
                    rb.AddTorque(transform.forward * -airAngularAcceleration);
                }

                if (qDown)
                {
                    rb.AddTorque(transform.forward * airAngularAcceleration);
                }
            }
            else
            {
                if (eDown)
                {
                    rb.AddTorque(transform.forward * airAngularAcceleration);
                }

                if (qDown)
                {
                    rb.AddTorque(transform.forward * -airAngularAcceleration);
                }
            }

            rb.AddTorque(transform.up * horizontalInput * airAngularAcceleration);

        }
    }
}
