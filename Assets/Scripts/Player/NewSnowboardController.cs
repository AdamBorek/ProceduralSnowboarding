using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class NewSnowboardController : MonoBehaviour
{
    [SerializeField] Camera camera;

    LineRenderer lineRenderer;

    // save starting position and rotation for reset
    Vector3 startPos;
    Quaternion startRot;
    Vector3 startLinVel;
    Vector3 startAngVel;

    // inputs
    private float verticalInput;
    private float horizontalInput;
    private bool leftShiftDown;
    private bool spaceDown;

    // components
    Rigidbody rb;
    [SerializeField] MeshCollider meshCollider;
    [SerializeField] GameObject snowboardVisual;

    // front and back of board
    [SerializeField] Transform rightTransform;
    [SerializeField] Transform leftTransform;
    [SerializeField] Transform midTransform;
    [SerializeField] Transform frontTransform;
    [SerializeField] Transform backTransform;

    // raycasts local downwards from different parts of the board
    RaycastHit localRightHit;
    RaycastHit localLeftHit;
    RaycastHit localMidHit;
    RaycastHit localFrontHit;
    RaycastHit localBackHit;

    // raycasts global downwards from different parts of the board
    RaycastHit globalRightHit;
    RaycastHit globalLeftHit;
    RaycastHit globalMidHit;
    RaycastHit globalFrontHit;
    RaycastHit globalBackHit;

    // ignore layer, for own collider 
    [SerializeField] LayerMask ignoreLayer;

    // steepness calculated under board
    float rightSteepness;
    float midSteepness;
    float leftSteepness;

    public float currentLinearSpeed;
    public float currentAngularSpeed;

    [SerializeField] float steepnessDiffLimit = 3f;

    // different states of the player
    public bool onGround;
    public bool inAir;
    public bool inGrind;

    private bool canEnterInAir = true;
    private float inAirCooldownTimer = 0.5f;

    public bool canEnterGrind = true;
    private float grindCooldownTimer = 0.5f;

    public SplineContainer currentGrindSpline;
    float currentGrindPos;
    float grindSpeed;
    float beforeGrindSpeed;

    // goofy / regular stance
    // player is regular by default
    public bool goofy;

    // going up
    public bool goingUp;
    [SerializeField] float gravityDown = -20f;
    [SerializeField] float gravityUp = -10f;
    public Vector3 currentGravity;

    // distance cutoff for being reported as on the ground
    [SerializeField] float groundDistanceCutoff = 0.1f;
    [SerializeField] float onGroundOffset = 0.01f;

    // controls
    // ground
    [SerializeField] float groundTorqueMultiplier;
    [SerializeField] float groundLinearDrag;
    [SerializeField] float groundLinearVelocityCap;
    [SerializeField] float groundAngularDrag;
    [SerializeField] float groundAngularVelocityCap;
    // air
    [SerializeField] float airTorqueMultiplier;
    [SerializeField] float airLinearDrag;
    [SerializeField] float airLinearVelocityCap;
    [SerializeField] float airAngularDrag;
    [SerializeField] float airAngularVelocityCap;

    // duck / brake drag
    [SerializeField] float fastDrag;
    [SerializeField] float brakeDrag;
    [SerializeField] float brakeMultiplier;

    // jump
    // Add a field to keep track of whether the player is allowed to jump
    private bool canJump = true;
    private float jumpCooldownTimer = 0.25f; // Adjust as needed
    [SerializeField] float jumpMultiplier;

    // velocity direction correction
    [SerializeField] float velocityCorrectionMultiplier;

    // board turn visual
    Vector3 baseVisualRotation;
    [SerializeField] float turnVisualMax;

    // Start is called before the first frame update
    void Start()
    {
        // get components
        rb = gameObject.GetComponent<Rigidbody>();

        // set default values
        startPos = transform.position;
        startRot = transform.rotation;
        startLinVel = rb.velocity;
        startAngVel = rb.angularVelocity;

        onGround = false;
        inAir = false;
        inGrind = false;

        baseVisualRotation = snowboardVisual.transform.localEulerAngles;

        lineRenderer = gameObject.GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // inputs

        // vertical input
        verticalInput = Input.GetAxis("Vertical");
        // horizontal input
        horizontalInput = Input.GetAxis("Horizontal");
        // shift
        if (Input.GetKey(KeyCode.LeftShift))
        {
            leftShiftDown = true;
        }
        else
        {
            leftShiftDown = false;
        }
        // space
        if (Input.GetKey(KeyCode.Space))
        {
            spaceDown = true;
        }
        else
        {
            spaceDown = false;
        }

        UpdatePlayerState();
        UpdateRaycastHits();
        UpdateGravity();

        if (!canJump)
        {
            // If on the ground and the cooldown timer is active
            // Decrease the cooldown timer
            jumpCooldownTimer -= Time.deltaTime;

            // If the cooldown timer has elapsed, allow jumping again
            if (jumpCooldownTimer <= 0)
            {
                canJump = true;
            }
        }


        // logic only on ground
        if (onGround)
        {
            // jumping
            if (spaceDown && canJump)
            {
                rb.AddForce(gameObject.transform.up * jumpMultiplier);
                // Prevent jumping until the cooldown elapses
                canJump = false;
                jumpCooldownTimer = 0.25f; // Reset the cooldown timer
            }
            rightSteepness = CalculateSteepness(globalRightHit);
            midSteepness = CalculateSteepness(globalMidHit);
            leftSteepness = CalculateSteepness(globalLeftHit);
            UpdateStance();
            UpdateVisualsGround();
        }

        // logic only in air
        if (inAir)
        {
            UpdateVisualsAir();
        }

        // update speed
        currentLinearSpeed = rb.velocity.magnitude;
        currentAngularSpeed = rb.angularVelocity.magnitude;

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + rb.velocity);

        
        if (!rb.isKinematic)
        {
            camera.fieldOfView = 70 + currentLinearSpeed * 0.6f;
        }
        else
        {
            camera.fieldOfView = 70 + beforeGrindSpeed * 0.6f;
        }


        if (camera.fieldOfView > 109)
        {
            camera.fieldOfView = 109;
        }
    }

    private void FixedUpdate()
    {
        if (!inGrind && !canEnterGrind)
        {
            // If not in the grind state and the cooldown timer is active
            // Decrease the cooldown timer
            grindCooldownTimer -= Time.deltaTime;

            // If the cooldown timer has elapsed, allow entering the grind state again
            if (grindCooldownTimer <= 0)
            {
                canEnterGrind = true;
            }
        }

        if (onGround)
        {
            HandleOnGroundControls();
        }

        if (inAir)
        {
            HandleInAirControls();
        }

        if (inGrind)
        {
            currentGrindPos += grindSpeed * Time.deltaTime;
            MoveAlongRail(currentGrindSpline, currentGrindPos, grindSpeed);
        }
    }

    void UpdateGravity()
    {
        float upAngle = Vector3.Angle(rb.velocity.normalized, Vector3.up);
        float downAngle = Vector3.Angle(-Vector3.up, rb.velocity.normalized);

        if (upAngle < downAngle)
        {
            goingUp = true;
            Physics.gravity = new Vector3(0f, gravityUp, 0f);
        }
        else
        {
            goingUp = false;
            Physics.gravity = new Vector3(0f, gravityDown, 0f);
        }

        currentGravity = Physics.gravity;
    }

    void UpdateVisualsGround()
    {
        // reset visuals
        snowboardVisual.transform.localEulerAngles = new Vector3(0, 0, 0);

        // line up with ground

        if (globalMidHit.collider.gameObject.tag != "Stunt" && globalRightHit.collider.gameObject.tag != "Stunt" && globalLeftHit.collider.gameObject.tag != "Stunt")
        {

            // Rotation on Z axis
            if (globalRightHit.distance != globalLeftHit.distance) //  && frontSteepnessDiff < steepnessDiffLimit
            {
                // calculate difference between distances
                float diff = globalRightHit.distance - globalLeftHit.distance;
                // calculate angle needed to rotate
                float rotationAngle = Mathf.Atan2(diff, Vector3.Distance(leftTransform.position, rightTransform.position)) * Mathf.Rad2Deg;
                // rotate board
                snowboardVisual.transform.Rotate(Vector3.right, rotationAngle);
            }

            // Rotation on X axis
            if (globalFrontHit.distance != globalBackHit.distance)
            {
                // calculate difference between distances
                float diff = globalFrontHit.distance - globalBackHit.distance;
                // calculate angle needed to rotate
                float rotationAngle = Mathf.Atan2(diff, Vector3.Distance(backTransform.position, frontTransform.position)) * Mathf.Rad2Deg;
                // rotate board
                snowboardVisual.transform.Rotate(Vector3.forward, rotationAngle);
                // update raycasts
                UpdateRaycastHits();
            }

        }

        // add rotation to the side
        if (!goofy)
            snowboardVisual.transform.localEulerAngles += new Vector3(0, 0, baseVisualRotation.x - horizontalInput * turnVisualMax);
        else
            snowboardVisual.transform.localEulerAngles += new Vector3(0, 0, baseVisualRotation.x + horizontalInput * turnVisualMax);
    }

    void UpdateVisualsAir()
    {
        snowboardVisual.transform.localEulerAngles = new Vector3(0, 0, 0);
    }

    void UpdatePlayerState()
    {
        if (!inGrind)
        {
            //check which parts of the board are close to the ground
            int onGroundparts = 0;
            if (globalRightHit.collider != null && globalRightHit.distance < groundDistanceCutoff)
            {
                onGroundparts++;
            }
            if (globalMidHit.collider != null && globalMidHit.distance < groundDistanceCutoff)
            {
                onGroundparts++;
            }
            if (globalLeftHit.collider != null && globalLeftHit.distance < groundDistanceCutoff)
            {
                onGroundparts++;
            }

            if (onGroundparts > 0)
            {
                // if not on ground mode, switch
                if (!onGround)
                {
                    SwitchToOnGround();
                }

                // Reset the inAir cooldown timer if it's not already running
                if (canEnterInAir)
                {
                    inAirCooldownTimer = 0.5f;
                }
            }
            else
            {
                // If not on the ground, decrement the inAirCooldownTimer
                if (inAirCooldownTimer > 0)
                {
                    inAirCooldownTimer -= Time.deltaTime;
                }

                // If the timer has elapsed, allow entering the InAir state
                if (inAirCooldownTimer <= 0)
                {
                    canEnterInAir = true;
                }

                // if not in air mode, switch
                if (!inAir)
                {
                    SwitchToInAir();
                }
            }
        }
    }

    void SwitchToOnGround()
    {
        if (globalMidHit.collider.gameObject.tag != "Stunt")
        {
            RotateToGroundInstant();
        }

        Physics.SyncTransforms();

        onGround = true;
        inAir = false;
        inGrind = false;

        rb.drag = groundLinearDrag;
        rb.maxLinearVelocity = groundLinearVelocityCap;
        rb.angularDrag = groundAngularDrag;
        rb.maxAngularVelocity = groundAngularVelocityCap;
    }

    void SwitchToInAir()
    {
        inAir = true;
        onGround = false;
        inGrind = false;

        rb.drag = airLinearDrag;
        rb.maxLinearVelocity = airLinearVelocityCap;
        rb.angularDrag = airAngularDrag;
        rb.maxAngularVelocity = airAngularVelocityCap;

        canEnterInAir = false;
    }

    public void SwitchToInGrind(SplineContainer grindSpline, float grindStart, float grindSpeed)
    {
        beforeGrindSpeed = currentLinearSpeed;

        rb.isKinematic = true;

        inGrind = true;
        inAir = false;
        onGround = false;

        currentGrindSpline = grindSpline;

        currentGrindPos = grindStart;

        this.grindSpeed = grindSpeed;

        Vector3 grindOffset = camera.transform.position - transform.position;
        camera.GetComponent<LatestCamera>().grindOffset = grindOffset;
    }

    void OutOfGrind()
    {
        inGrind = false;
        grindCooldownTimer = 0.5f;
        canEnterGrind = false;
        rb.isKinematic = false;

        rb.velocity = currentGrindSpline.gameObject.transform.forward * beforeGrindSpeed;
    }

    void MoveAlongRail(SplineContainer grindSpline, float splinePos, float grindSpeed)
    {

        Vector3 nextPos = grindSpline.gameObject.transform.TransformPoint(grindSpline.Spline.EvaluatePosition(splinePos));

        if (splinePos < 0.97f)
        {
            transform.position = nextPos;
            rb.position = nextPos;
        }
        else
        {
            // end the grind
            OutOfGrind();
        }

    }

    private void UpdateStance()
    {
        // work out angles of each end of board
        float angleToForward = Vector3.Angle(rb.velocity, transform.forward);
        float angleToBackward = Vector3.Angle(rb.velocity, -transform.forward);

        // set stance depending on angle
        if (angleToForward < angleToBackward)
        {
            goofy = false;
        }
        else if (angleToBackward < angleToForward)
        {
            goofy = true;
        }
    }

    void UpdateRaycastHits()
    {
        // if any of the distances come back as 0, that means there is no hit
        Physics.Raycast(rightTransform.position, -transform.up, out localRightHit, Mathf.Infinity, ~ignoreLayer);
        Physics.Raycast(leftTransform.position, -transform.up, out localLeftHit, Mathf.Infinity, ~ignoreLayer);
        Physics.Raycast(midTransform.position, -transform.up, out localMidHit, Mathf.Infinity, ~ignoreLayer);
        Physics.Raycast(frontTransform.position, -transform.up, out localFrontHit, Mathf.Infinity, ~ignoreLayer);
        Physics.Raycast(backTransform.position, -transform.up, out localBackHit, Mathf.Infinity, ~ignoreLayer);

        Physics.Raycast(rightTransform.position, -Vector3.up, out globalRightHit, Mathf.Infinity, ~ignoreLayer);
        Physics.Raycast(leftTransform.position, -Vector3.up, out globalLeftHit, Mathf.Infinity, ~ignoreLayer);
        Physics.Raycast(midTransform.position, -Vector3.up, out globalMidHit, Mathf.Infinity, ~ignoreLayer);
        Physics.Raycast(frontTransform.position, -Vector3.up, out globalFrontHit, Mathf.Infinity, ~ignoreLayer);
        Physics.Raycast(backTransform.position, -Vector3.up, out globalBackHit, Mathf.Infinity, ~ignoreLayer);

        // debug logs
        //Debug.Log("Right hit distance: " + rightHit.distance + ", normal: " + rightHit.normal);
        //Debug.Log("Left hit distance: " + leftHit.distance + ", normal: " + leftHit.normal);
        //Debug.Log("Mid hit distance: " + midHit.distance + ", normal: " + midHit.normal);
        //Debug.Log("Front hit distance: " + frontHit.distance + ", normal: " + frontHit.normal);
        //Debug.Log("Back hit distance: " + backHit.distance + ", normal: " + backHit.normal);
    }

    void RotateToGroundInstant()
    {
        // Rotation on Z axis
        if (globalRightHit.distance != globalLeftHit.distance) //  && frontSteepnessDiff < steepnessDiffLimit
        {
            // calculate difference between distances
            float diff = globalRightHit.distance - globalLeftHit.distance;
            // calculate angle needed to rotate
            float rotationAngle = Mathf.Atan2(diff, Vector3.Distance(leftTransform.position, rightTransform.position)) * Mathf.Rad2Deg;
            // rotate board
            transform.Rotate(Vector3.right, rotationAngle);
            // update raycasts
            UpdateRaycastHits();
        }

        // Rotation on X axis
        if (globalFrontHit.distance != globalBackHit.distance)
        {
            // calculate difference between distances
            float diff = globalFrontHit.distance - globalBackHit.distance;
            // calculate angle needed to rotate
            float rotationAngle = Mathf.Atan2(diff, Vector3.Distance(backTransform.position, frontTransform.position)) * Mathf.Rad2Deg;
            // rotate board
            transform.Rotate(Vector3.forward, rotationAngle);
            // update raycasts
            UpdateRaycastHits();
        }
    }

    float CalculateSteepness(RaycastHit hit)
    {
        Vector3 normal = hit.normal;

        float angle = Vector3.Angle(normal, Vector3.up);

        return angle;
    }

    void HandleOnGroundControls()
    {
        // turning
        rb.AddTorque(transform.up * horizontalInput * Time.fixedDeltaTime * groundTorqueMultiplier);

        // make velocity vector face correct direction
        if (!goofy)
        {
            if (rb.velocity.normalized != transform.forward.normalized)
            {
                rb.velocity = Vector3.RotateTowards(rb.velocity, transform.forward, Time.fixedDeltaTime * velocityCorrectionMultiplier, 0f);
            }
        }
        else
        {
            if (rb.velocity.normalized != -transform.forward.normalized)
            {
                rb.velocity = Vector3.RotateTowards(rb.velocity, -transform.forward, Time.fixedDeltaTime * velocityCorrectionMultiplier, 0f);
            }
        }

        // drag
        if (verticalInput > 0f)
        {
            rb.drag = fastDrag;
        }
        else if (verticalInput == 0f)
        {
            rb.drag = groundLinearDrag;
        }
        else
        {
            if (rb.drag <= brakeDrag)
            {
                rb.drag += Time.fixedDeltaTime * brakeMultiplier;
            }
        }


    }

    void HandleInAirControls()
    {
        // y
        rb.AddTorque(transform.up * horizontalInput * Time.fixedDeltaTime * airTorqueMultiplier);

        // z
        if (!goofy && leftShiftDown)
        {
            rb.AddTorque(transform.right * verticalInput * Time.fixedDeltaTime * airTorqueMultiplier);
        }
        else if (leftShiftDown)
        {
            rb.AddTorque(-transform.right * verticalInput * Time.fixedDeltaTime * airTorqueMultiplier);
        }
    }
}
