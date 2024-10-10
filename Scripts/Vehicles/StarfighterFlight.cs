// Orignial Code by Muffin Man Productions
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class StarfighterFlight : MonoBehaviour
{
    public bool isActive;

    

    [Header("Movement Settings")]
    public bool swapControllerSticks = false;
    public bool swapBoostAndDodgeButtons = false;
    public bool useDpadForLanding = false;
    public bool invertPitch = false;
    [SerializeField] private float yawTorque = 500f;
    [SerializeField] private float pitchTorque = 1000f;
    [SerializeField] private float increasedThrust = 30000f;
    [SerializeField] private float brakingThrust = 10000f;
    [SerializeField] private float defaultThrust = 20000f;
    [SerializeField] private float accelerationTime = 1f;
    public float aimSpeedPenalty = 1f;
    public float starfighterAimSensitivity = 1f;
    [SerializeField] private float maxUpDownAngle = 10f;

    [Header("Zoom Settings")]
    public float zoomReductionFactor = 2;

    float c_yawTorque;
    float c_pitchTorque;

    [Header("Dodge Settings")]
    [SerializeField] private bool canDodge = true;
    [SerializeField] private float barrelRollThrust = 500f;
    [SerializeField] private float dodgeCooldown = 3f;

    bool dodgeTimerActive;
    float dodgeTimer;

    [Header("Boost Settings")]
    [SerializeField] private bool canBoost = true;
    [SerializeField] private bool alternateBoostMode;
    [SerializeField] private float boostTime = 5f;
    [SerializeField] private float minBoostTime = 1f;
    [SerializeField] private float boostCooldown = 2f;
    [SerializeField] private float boostThrust = 1000f;

    private Vector3 dodgeDirection;

    [Header("Tilt Settings")]
    [SerializeField] private float tiltAmount = 10f;
    [SerializeField] private float maxTiltVelocity = 10f;
    [SerializeField] private float maxTilt = 50f;
    [SerializeField] private float tiltSmoothingTime = 1f;

    float angularTiltVelocity;
    float lastAngularTiltVelocity;
    float angularTiltVelocitySmoothing = 0.0f;

    [Header("Landing Settings")]
    [SerializeField] private bool canLand = true;
    [SerializeField] float landingInputTimer = 2f;
    [SerializeField] float landingSpeed = 2f;
    [SerializeField] float landHeight = 5f;
    [SerializeField] float landingThreshold = 2f;
    [SerializeField] float takeOffThreshold = 5f;
    [SerializeField] float stopThreshold = 1.5f;
    [SerializeField] float maxLandDistance = 500f;
    [SerializeField] float takeOffDistance = 10f;
    [SerializeField] LayerMask groundLayer;

    //VFX
    [Header("Particles")]
    public ParticleSystem[] boostParticles;

    //Landing
    Vector3 adjustedLandingPosition;
    float timerDuration;
    bool isTimerRunning = false;
    Vector3 groundNormal;
    Vector3 landingPosition;
    Vector3 takeOffPosition;

    //Boosting
    private float boostDuration;
    private float minBoostDuration = 0;
    private float boostCooldownDuration;

    //Pitch yaw calculation
    private float velocitySmooth;
    private float currentSpeed;
    private float yawVelocitySmooth;
    private float pitchVelocitySmooth;

    //Tilt
    public Transform tiltTransform;

    //Animation
    private Animator anim;


    [Header("Debug")]
    public bool isLanding = false;
    public bool hasLanded = false;
    public bool isTakingOff = false;
    public bool isDodging = false;
    public bool isDoing180 = false;
    public bool isBoosting = false;
    public float currentSpeedDebug;
    public bool wingsOpen = false;

    //Physics
    private Rigidbody rb;

    
    

    //events
    public delegate void MyEventHandler();
    public static event MyEventHandler OnWingsOpen;
    public static event MyEventHandler OnWingsClose;
    public static event MyEventHandler OnTurn;
    public static event MyEventHandler OnBoostStart;
    public static event MyEventHandler OnBoostStop;

    //input values
    private float thrust1D;
    private Vector2 pitchYaw;
    private float takeOffLand;
    private float dodge;
    private float boost;
    private float zoom;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        Health.OnPlayerDeath += ResetShip;
        LevelManager.OnPlayerControl += OnPlayerSpawn;

        isDodging = false;
        isDoing180 = false;
        dodgeTimerActive = false;

        dodgeTimer = 0;

        currentSpeed = defaultThrust;

        

        SetBoostParticles(false);
        //rotationDodgeTargetTorque = 180f / rotationTime;

        tiltTransform = GetComponent<StarfighterController>().tiltModel;
    }

    private void FixedUpdate()
    {
        if (isActive)
        {
            HandleZoom();
            HandleMovement();

            if (canDodge)
            {
                HandleDodge();
            }
            if (canBoost)
            {
                HandleBoost();
            }
            ShipTilt();

            if (canLand)
            {
                Landing();
            }
        }
        else
        {
            rb.AddRelativeForce(Vector3.forward * currentSpeed * Time.deltaTime);
        }
    }

    private void HandleZoom()
    {
        if(zoom > 0 && !hasLanded && !isLanding || isBoosting)
        {
            c_pitchTorque = pitchTorque / zoomReductionFactor;
            c_yawTorque = yawTorque / zoomReductionFactor;
        }

        else
        {
            c_pitchTorque = pitchTorque;
            c_yawTorque = yawTorque;
        }
    }
    private void HandleMovement()
    {
        if (!hasLanded && !isLanding)
        {

            // pitch
            float pitchInput = Mathf.Clamp(pitchYaw.y, -1f, 1f);

            // yaw
            float yawInput = Mathf.Clamp(pitchYaw.x, -1f, 1f);

            // Get the ship's up direction in world space
            Vector3 upDirection = transform.TransformDirection(Vector3.up);

            // Determine the pitch angle between the world up vector and the ship's up direction
            float pitchAngle = Vector3.Angle(Vector3.up, upDirection);

            // Adjust the pitch torque based on the ship's pitch orientation
            //float adjustedPitchTorque = CalculateAdjustedTorque(pitchAngle, maxUpDownAngle, c_pitchTorque);

            // Calculate sensitivity factor based on current speed
            float sensitivityFactor = Mathf.Clamp01(currentSpeed / increasedThrust) * starfighterAimSensitivity;

            // Smoothly adjust pitch input with sensitivity factor
            float smoothPitchInput = Mathf.SmoothDamp(rb.angularVelocity.x, (invertPitch ? -pitchInput : pitchInput) * c_pitchTorque, ref pitchVelocitySmooth, aimSpeedPenalty * sensitivityFactor);

            // Apply pitch torque
            rb.AddRelativeTorque(Vector3.right * smoothPitchInput * Time.deltaTime);


            // Smoothly adjust yaw input with sensitivity factor
            float smoothYawInput = Mathf.SmoothDamp(rb.angularVelocity.y, yawInput * c_yawTorque, ref yawVelocitySmooth, aimSpeedPenalty * (isBoosting ? starfighterAimSensitivity : sensitivityFactor));

            // Apply yaw torque
            rb.AddRelativeTorque(Vector3.up * smoothYawInput * Time.deltaTime);
        }

        if (!isLanding && !hasLanded)
        {
            // Calculate the thrust based on thrust1D
            float targetThrust = 0f;

            if (thrust1D == 1)
            {
                targetThrust = increasedThrust;
            }
            else if (thrust1D == -1)
            {
                targetThrust = brakingThrust;
            }
            else
            {
                targetThrust = defaultThrust;
            }

            // Smoothly adjust the current speed towards the target thrust
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetThrust, ref velocitySmooth, accelerationTime);
            currentSpeedDebug = currentSpeed;

            if (!isBoosting)
            {
                // Apply thrust relative to the ship's forward direction
                rb.AddRelativeForce(Vector3.forward * currentSpeed * Time.deltaTime);
            }
        }

        //keep z axis rotation set to 0
        //no longer needed due to tilt
        //transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
    }

    private void HandleDodge()
    {
        if(dodge == 1 && !dodgeTimerActive && !isDodging && !isDoing180 && !hasLanded && !isLanding && !isBoosting)
        {
            // yaw
            float yawInput = Mathf.Clamp(pitchYaw.x, -1f, 1f);
            //pitch
            float pitchInput = Mathf.Clamp(pitchYaw.y, -1f, 1f);

            // Calculate the dodge direction based on input
            dodgeDirection = new Vector3(yawInput, pitchInput, 0f).normalized;

            // Trigger the appropriate animation
            if (yawInput > 0.1f)
            {
                anim.SetTrigger("RollRight");
                isDodging = true;
                dodgeTimerActive = true;
            }
            else if (yawInput < -0.1f)
            {
                anim.SetTrigger("RollLeft");
                isDodging = true;
                dodgeTimerActive = true;
            }
            else
            {
                //Do 180
                //transform.Rotate(15, 180, 0, Space.Self);
                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x + 180, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
                //anim.SetTrigger("RollRight");
                anim.SetTrigger("Do180");

                isDoing180 = true;
                dodgeTimerActive = true;
                OnTurn?.Invoke();
            }
        }
        if (isDodging)
        {
            rb.AddRelativeForce(dodgeDirection * barrelRollThrust);
        }

        if (isDoing180)
        {
            //rb.AddRelativeForce(Vector3.back * barrelRollThrust);

            // Calculate the torque needed to achieve the desired rotation
            //float torqueToApply = CalculateTorqueToReachTarget();

            // Apply torque in the local x-axis
            //rb.AddTorque(rotationDodgeTargetTorque * Vector3.up);
            //rb.AddTorque(rotationDodgeTargetTorque * Vector3.right);

            //LimitAngularVelocity();
        }

        if(dodgeTimerActive)
        {
            dodgeTimer += Time.deltaTime;

            if(dodgeTimer >= dodgeCooldown)
            {
                dodgeTimerActive = false;
                dodgeTimer = 0;
            }
        }
    }

    public void DodgeComplete()
    {
        isDodging = false;
        isDoing180 = false;
    }

    private void HandleBoost()
    {
        if (alternateBoostMode)
        {
            // Alternate boost mode logic
            if (boost == 1 && boostCooldownDuration >= boostCooldown && !isDodging && !hasLanded && !isLanding && !isBoosting)
            {
                isBoosting = true;
                SetBoostParticles(true);
                boostCooldownDuration = 0;
                OnBoostStart?.Invoke();
            }
            else if (isBoosting)
            {
                minBoostDuration += Time.deltaTime;

                // Stop boosting if boost != 1 and minimum boost time is met
                if (boost != 1 && minBoostDuration >= minBoostTime)
                {
                    isBoosting = false;
                    SetBoostParticles(false);
                    OnBoostStop?.Invoke();
                    AnimateStarfighterBoosting();
                    boostDuration = 0;
                    minBoostDuration = 0;
                }
            }
        }
        else
        {
            // Regular boost mode logic
            if (boost == 1 && boostCooldownDuration >= boostCooldown && !isDodging && !hasLanded && !isLanding && !isBoosting)
            {
                isBoosting = true;
                SetBoostParticles(true);
                boostCooldownDuration = 0;
                OnBoostStart?.Invoke();
            }
        }

        if (isBoosting)
        {
            AnimateStarfighterBoosting();

            boostDuration += Time.deltaTime;

            rb.AddRelativeForce(Vector3.forward * boostThrust * Time.deltaTime);

            if (boostDuration >= boostTime)
            {
                isBoosting = false;
                SetBoostParticles(false);
                OnBoostStop?.Invoke();
                AnimateStarfighterBoosting();
                boostDuration = 0;
            }
        }

        if (!isBoosting && boostCooldownDuration <= boostCooldown)
        {
            boostCooldownDuration += Time.deltaTime;
        }

        //if (boost == 1 && boostCooldownDuration >= boostCooldown && !isDodging && !hasLanded && !isLanding && !isBoosting)
        //{
        //    isBoosting = true;
        //    SetBoostParticles(true);
        //    boostCooldownDuration = 0;
        //    OnBoostStart?.Invoke();
        //    //StartCoroutine(ActivateBoostVFX());
        //}

        //if (isBoosting)
        //{
        //    AnimateStarfighterBoosting();

        //    boostDuration += Time.deltaTime;

        //    rb.AddRelativeForce(Vector3.forward * boostThrust * Time.deltaTime);

        //    if (boostDuration >= boostTime)
        //    {
        //        isBoosting = false;
        //        SetBoostParticles(false);
        //        OnBoostStop?.Invoke();
        //        AnimateStarfighterBoosting();
        //        //StartCoroutine(ActivateBoostVFX());
        //        boostDuration = 0;
        //    }
        //}
        //else if (!isBoosting && boostCooldownDuration <= boostCooldown)
        //{
        //    boostCooldownDuration += Time.deltaTime;
        //}
    }

    public void SetBoostParticles(bool active)
    {
        if (active)
        {
            foreach(ParticleSystem boostPart in boostParticles)
            {
                boostPart.Play();
            }
        }
        else
        {
            foreach (ParticleSystem boostPart in boostParticles)
            {
                boostPart.Stop();
            }
        }
    }

    private void ShipTilt()
    {

        //if (!hasLanded)
        //{
        //    // Calculate the ship's pitch and yaw independently
        //    float pitch = transform.rotation.eulerAngles.x;
        //    float yaw = transform.rotation.eulerAngles.y;

        //    // Apply tilt based on velocity
        //    float angularVelocityY = rb.angularVelocity.y;
        //    angularTiltVelocity = (angularVelocityY * -tiltAmount) / maxTiltVelocity;

        //    // Apply some additional smoothing
        //    angularTiltVelocity = Mathf.SmoothDamp(lastAngularTiltVelocity, angularTiltVelocity, ref angularTiltVelocitySmoothing, tiltSmoothingTime);

        //    // Update lastAngularTiltVelocity for the next frame
        //    lastAngularTiltVelocity = angularTiltVelocity;

        //    // Apply the tilt
        //    transform.rotation = Quaternion.Euler(pitch, yaw, angularTiltVelocity * maxTilt);
        //}

        if (hasLanded) return;

        // Calculate the ship's pitch and yaw independently
        float pitch = transform.rotation.eulerAngles.x;
        float yaw = transform.rotation.eulerAngles.y;

        // Apply tilt based on velocity
        float angularVelocityY = rb.angularVelocity.y;
        angularTiltVelocity = (angularVelocityY * -tiltAmount) / maxTiltVelocity;

        // Apply some additional smoothing
        angularTiltVelocity = Mathf.SmoothDamp(lastAngularTiltVelocity, angularTiltVelocity, ref angularTiltVelocitySmoothing, tiltSmoothingTime);

        // Update lastAngularTiltVelocity for the next frame
        lastAngularTiltVelocity = angularTiltVelocity;

        // Apply the tilt
        tiltTransform.rotation = Quaternion.Euler(pitch, yaw, angularTiltVelocity * maxTilt);
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0f);
    }

    private bool LandingPreCheck()
    {
        if (takeOffLand == 1 && !isTimerRunning && currentSpeed <= brakingThrust*3)
        {
            if (!hasLanded)
            {
                LandingRaycast();
            }
            else
            {
                isLanding = !isLanding;
            }

            timerDuration = landingInputTimer;
            isTimerRunning = true;
        }
        if (isTimerRunning)
        {
            timerDuration -= Time.deltaTime;
            if (timerDuration <= 0.0f)
            {
                isTimerRunning = false;
            }
        }

        return isLanding;
    }

    private void LandingRaycast()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, maxLandDistance, groundLayer))
        {
            groundNormal = hitInfo.normal;
            landingPosition = hitInfo.point;
            isLanding = !isLanding;
        }
        else
        {
            Debug.Log("Ground not detected");
        }
    }

    private void Landing()
    {
        if (LandingPreCheck())
        {
            AnimateStarfighterLanding();
            //land
            adjustedLandingPosition = new Vector3(transform.position.x, landingPosition.y + landHeight, transform.position.z);

            if (Vector3.Distance(transform.position, adjustedLandingPosition) > stopThreshold)
            {
                transform.position = Vector3.Lerp(transform.position, adjustedLandingPosition, landingSpeed * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, adjustedLandingPosition) < landingThreshold)
            {
                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, groundNormal);
                targetRotation *= Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0); // Keep the existing y rotation
                if (Vector3.Distance(transform.position, adjustedLandingPosition) > stopThreshold)
                {
                    tiltTransform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, landingSpeed * Time.deltaTime);
                }

                hasLanded = true;
                rb.isKinematic = true;
            }
        }
        else
        {
            //takeoff
            if (takeOffLand == 1 && hasLanded)
            {
                //AnimateStarfighterLanding();
                isTakingOff = true;
                rb.isKinematic = false;
                
                
                takeOffPosition = new Vector3(transform.position.x, transform.position.y + takeOffDistance, transform.position.z);
                //rb.AddRelativeForce(Vector3.up * takeOffThrust * Time.deltaTime);
            }
            if (isTakingOff)
            {
                transform.position = Vector3.Lerp(transform.position, takeOffPosition, landingSpeed * Time.deltaTime);
                Quaternion targetRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                tiltTransform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, landingSpeed * 2 * Time.deltaTime);

                if (Vector3.Distance(transform.position, takeOffPosition) < takeOffThreshold)
                {
                    //Flying
                    AnimateStarfighterLanding();
                    hasLanded = false;
                    isTakingOff = false;
                }
            }
        }
    }

    private void AnimateStarfighterLanding()
    {
        if (isLanding && wingsOpen)
        {
            anim.SetBool("Open", false);
            wingsOpen = false;
            OnWingsClose?.Invoke();
        }
        if (!isLanding && !wingsOpen)
        {
            anim.SetBool("Open", true);
            wingsOpen = true;
            OnWingsOpen?.Invoke();
        }
    }

    private void AnimateStarfighterBoosting()
    {
        if (isBoosting && wingsOpen)
        {
            anim.SetBool("Open", false);
            wingsOpen = false;
            OnWingsClose?.Invoke();
        }
        else if (!isBoosting && !wingsOpen)
        {
            anim.SetBool("Open", true);
            wingsOpen = true;
            OnWingsOpen?.Invoke();
        }
    }

    private void ResetShip(string attackerID, bool selfInf, int playerNumber)
    {
        anim.SetBool("Open", true);
        wingsOpen = true;
        SetBoostParticles(false);
        boostDuration = 0;
    }

    private float CalculateAdjustedTorque(float currentAngle, float threshold, float originalTorque)
    {
        // Calculate the adjusted torque based on the ship's pitch orientation
        if (Mathf.Abs(currentAngle - 90f) < threshold || Mathf.Abs(currentAngle + 90f) < threshold)
        {
            // Ship is aiming straight up or straight down, reduce the torque
            float t = Mathf.Clamp01((Mathf.Abs(currentAngle) - threshold) / (90f - threshold));
            return Mathf.Lerp(originalTorque, 0f, t);
        }
        else
        {
            // Ship is not aiming straight up or straight down, use the original torque
            return originalTorque;
        }
    }

    public void OnPlayerSpawn()
    {
        if (wingsOpen)
        {
            anim.SetBool("Open", true);
            OnWingsOpen?.Invoke();
        }
    }

    //private IEnumerator ActivateBoostVFX()
    //{
    //    if (isBoosting)
    //    {
    //        boostSpeedVFX.Play();

    //        float amount = boostSpeedVFX.GetFloat("WarpAmount");
    //        while (amount < 1 && isBoosting)
    //        {
    //            amount += rate;
    //            boostSpeedVFX.SetFloat("WarpAmount", amount);
    //            yield return new WaitForSeconds(0.1f);
    //        }
    //    }
    //    else
    //    {
    //        float amount = boostSpeedVFX.GetFloat("WarpAmount");
    //        while (amount > 0 && !isBoosting)
    //        {
    //            amount -= rate;
    //            boostSpeedVFX.SetFloat("WarpAmount", amount);
    //            yield return new WaitForSeconds(0.1f);

    //            if(amount <= 0 + rate)
    //            {
    //                amount = 0;
    //                boostSpeedVFX.SetFloat("WarpAmount", amount);

    //                boostSpeedVFX.Stop();
    //            }
    //        }
    //    }
    //}
    
    private void OnDestroy()
    {
        Health.OnPlayerDeath -= ResetShip;
        LevelManager.OnPlayerControl -= OnPlayerSpawn;
    }

    //Input

    public void OnTrust(InputAction.CallbackContext context)
    {
        if(!swapControllerSticks)
        {
            thrust1D = context.ReadValue<float>();
        }
        
    }

    public void OnTrustAlt(InputAction.CallbackContext context)
    {
        if (swapControllerSticks)
        {
            thrust1D = context.ReadValue<float>();
        }
    }

    public void OnPitchYaw(InputAction.CallbackContext context)
    {
        if (!swapControllerSticks)
        {
            pitchYaw = context.ReadValue<Vector2>();
        }
    }

    public void OnPitchYawAlt(InputAction.CallbackContext context)
    {
        if (swapControllerSticks)
        {
            pitchYaw = context.ReadValue<Vector2>();
        }
    }

    public void OnTakeOffLand(InputAction.CallbackContext context)
    {
        if (!useDpadForLanding)
        {
            takeOffLand = context.ReadValue<float>();
        }
    }
    public void OnTakeOffLandAlt(InputAction.CallbackContext context)
    {
        if (useDpadForLanding)
        {
            takeOffLand = context.ReadValue<float>();
        }
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        if (!swapBoostAndDodgeButtons)
        {
            dodge = context.ReadValue<float>();
        }
    }

    public void OnDodgeAlt(InputAction.CallbackContext context)
    {
        if (swapBoostAndDodgeButtons)
        {
            dodge = context.ReadValue<float>();
        }
    }

    public void OnBoost(InputAction.CallbackContext context)
    {
        if (!swapBoostAndDodgeButtons)
        {
            boost = context.ReadValue<float>();
        }
    }

    public void OnBoostAlt(InputAction.CallbackContext context)
    {
        if (swapBoostAndDodgeButtons)
        {
            boost = context.ReadValue<float>();
        }
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        zoom = context.ReadValue<float>();
    }
}

