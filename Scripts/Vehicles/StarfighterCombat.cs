using UnityEngine;
using UnityEngine.InputSystem;


public class StarfighterCombat : MonoBehaviour
{
    public bool isActive = true;

    //events
    public delegate void MyEventHandler();
    public delegate void MyMissleFailedEventHandler(string secondaryWeaponName);
    public delegate void MyOverheatedEventHandler(bool overheated);

    public static event MyEventHandler OnUpdateHeatEvent;
    public static event MyEventHandler OnFireLasersEvent;
    public static event MyOverheatedEventHandler OnLasersOverheatedEvent;
    public static event MyEventHandler OnFireMissilesEvent;
    public static event MyMissleFailedEventHandler OnFireMissilesFailedEvent;
    public static event MyEventHandler OnUpdateMissilesEvent;


    [Header("Combat Settings")]
    public int playerNumber = 1;
    public bool fireMissilesIndependently = false;
    public bool fireLasersIndependently = false;
    public bool fireMissilesForwardFromLauncher = false;
    public float missileRotationOffsetThreshold = 2f;
    public string secondaryWeaponName = "Missiles";
    [SerializeField] float laserSpeed = 10f;
    [SerializeField] float laserFireRate = 0.5f;
    [SerializeField] float laserHeatCost = 10f;
    [SerializeField] float laserHeatBarRefillRate = 10f;
    [SerializeField] float refillDelay = 1f;
    public int missilesStartCount = 35;
    [SerializeField] float missileSpeed = 20f;
    [SerializeField] float missileFireRate = 1f;
    [SerializeField] Transform crosshairUI;
    [SerializeField] Transform[] laserCannons;
    [SerializeField] Transform[] missileLaunchers;
    [SerializeField] GameObject laserPrefab;
    [SerializeField] GameObject missilePrefab;
    public Transform projectileKeeper;

    float currentHeat;
    bool isOverheated;
    float lastShotTime;
    int currentMissileCount;
    float nextFireLaserTime = 0f;
    float nextFireMissileTime = 0f;
    int fireCount;
    int fireMCount;

    private StarfighterFlight starFlight;
    //private StarfighterUI starUI;
    //private StarfighterAudio starAud;
    private Target iTarget;

    //input
    private float fireLasers;
    private float fireMissiles;

    private void Start()
    {
        projectileKeeper = GameObject.Find("ProjectileKeeper").transform;

        currentHeat = 100;
        starFlight = GetComponent<StarfighterFlight>();
        //starUI = GetComponent<StarfighterUI>();
        //starAud = GetComponent<StarfighterAudio>();
        iTarget = GetComponent<Target>();
        //OnUpdateMissilesEvent?.Invoke();

        fireCount = 0;
        fireMCount = 0;
        SetStarterMissileCount();
    }
    private void Update()
    {
        if (isActive)
        {
            HandleLaserHeat();
            HandleShootingLasers();
            FireMissiles();
        }
    }

    private void HandleLaserHeat()
    {
        // Overheat
        if (currentHeat <= 0)
        {
            isOverheated = true;
            OnLasersOverheatedEvent?.Invoke(true);
        }

        // Refill
        if (currentHeat >= 100)
        {
            isOverheated = false; // Reset overheated state.
            OnLasersOverheatedEvent?.Invoke(false);
        }
        else if (Time.time - lastShotTime > refillDelay)
        {
            currentHeat += laserHeatBarRefillRate * Time.deltaTime;
            currentHeat = Mathf.Clamp(currentHeat, 0, 100);
            OnUpdateHeatEvent?.Invoke();
        }
    }
    private void HandleShootingLasers()
    {
        if (fireLasers == 1 && Time.time > nextFireLaserTime && !starFlight.hasLanded && !starFlight.isBoosting && !starFlight.isDoing180)
        {
            if (!isOverheated)
            {
                ShootLasers();
            }
            //else if (currentHeat >= maxHeat)
            //{
            //    isOverheated = false;  // Reset overheated state.
            //    starUI.SetDefaultHeatBarColor();
            //}

        }
    }
    private void ShootLasers()
    {
        if (fireCount == laserCannons.Length)
        {
            fireCount = 0;
        }

        if (fireLasersIndependently)
        {
            InstantiateLaser(laserCannons[fireCount]);
        }
        else
        {
            foreach (Transform cannon in laserCannons)
            {
                InstantiateLaser(cannon);
            }
        }
        fireCount++;
        

        // Increase heat when shooting.
        currentHeat -= laserHeatCost;
        currentHeat = Mathf.Clamp(currentHeat, 0, 100);
        lastShotTime = Time.time;


        //trigger event for UI and sounds
        OnFireLasersEvent?.Invoke();

        //starAud.PlayLaserSound();

        nextFireLaserTime = Time.time + 1 / laserFireRate;
    }

    private void InstantiateLaser(Transform laserCanon)
    {
        Vector3 shootDirection = (crosshairUI.position - laserCanon.position).normalized;
        GameObject laser = Instantiate(laserPrefab, laserCanon.position, Quaternion.identity, projectileKeeper);
        Rigidbody laserRigidbody = laser.GetComponent<Rigidbody>();
        laserRigidbody.linearVelocity = shootDirection * laserSpeed;
        Projectile p = laser.GetComponent<Projectile>();
        p.attackerID = iTarget.myName;
        p.keeper = projectileKeeper;
        p.playerNumber = playerNumber;
    }

    private void SetStarterMissileCount()
    {
        currentMissileCount = missilesStartCount;
    }

    private void FireMissiles()
    {
        if (currentMissileCount > 0) //input
        {
            if (fireMissiles == 1 && Time.time > nextFireMissileTime && !starFlight.hasLanded && !starFlight.isBoosting && !starFlight.isDoing180)
            {
                if (fireMCount == missileLaunchers.Length)
                {
                    fireMCount = 0;
                }

                if (fireMissilesIndependently)
                {
                    InstantiateMissile(missileLaunchers[fireMCount]);
                    currentMissileCount--;
                }
                else
                {
                    foreach (Transform launcher in missileLaunchers)
                    {
                        if (currentMissileCount == 0)
                        {
                            //OnFireMissilesEvent?.Invoke();
                            return;
                        }

                        InstantiateMissile(launcher);

                        currentMissileCount--; //reduce amount of missiles in current inventory
                    }
                }

                OnFireMissilesEvent?.Invoke();
                fireMCount++;
                //starAud.PlayMissileSound();

                nextFireMissileTime = Time.time + 1 / missileFireRate;
            }
        }
        else
        {
            if(fireMissiles == 1)
            {
                OnFireMissilesFailedEvent?.Invoke(secondaryWeaponName);
            }
        }
    }

    private void InstantiateMissile(Transform missileLauncher)
    {
        Vector3 shootDirection = (crosshairUI.position - missileLauncher.position).normalized;

        if (fireMissilesForwardFromLauncher)
        {
            shootDirection = missileLauncher.forward;
        }

        Quaternion missileRotation = Quaternion.LookRotation(shootDirection);
        GameObject missile = Instantiate(missilePrefab, missileLauncher.position, missileRotation, projectileKeeper);
        Projectile p = missile.GetComponent<Projectile>();
        p.attackerID = iTarget.myName;
        p.keeper = projectileKeeper;
        p.playerNumber = playerNumber;

        missile.transform.rotation = transform.localRotation;

        // Apply random rotation
        float rotationOffset = Random.Range(-missileRotationOffsetThreshold, missileRotationOffsetThreshold); // Adjust the range as needed
        Vector3 rotatedDirection = Quaternion.Euler(0, 0, rotationOffset) * shootDirection;

        Rigidbody missileRigidbody = missile.GetComponent<Rigidbody>();
        missileRigidbody.linearVelocity = rotatedDirection * missileSpeed;

        PlayerTargetLockOn lockOn = GetComponent<PlayerTargetLockOn>();
        PlayerTargeting mTargeting = GetComponent<PlayerTargeting>();

        if(lockOn != null)
        {
            if(lockOn.isLockedOn)
            {
                missile.GetComponent<MissileMovement>().SetTarget(mTargeting.target);
            }
        }
    }

    public float ReturnCurrentHeat()
    {
        return currentHeat;
    }

    public int ReturnCurrentMissileCount()
    {
        return currentMissileCount;
    }

    public void ResetMissileCount()
    {
        currentMissileCount = missilesStartCount;
        OnUpdateMissilesEvent?.Invoke();
    }

    public void ResupplyMissiles(int missiles)
    {
        if(currentMissileCount != missilesStartCount)
        {
            currentMissileCount += missiles;
            

            if (currentMissileCount > missilesStartCount)
            {
                currentMissileCount = missilesStartCount;
            }

            OnUpdateMissilesEvent?.Invoke();
        }
    }

    public void OnFireLasers(InputAction.CallbackContext context)
    {
        fireLasers = context.ReadValue<float>();
    }

    public void OnFireMissiles(InputAction.CallbackContext context)
    {
        fireMissiles = context.ReadValue<float>();
    }
}
