using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StarfighterCrash : MonoBehaviour
{
    [Header("General Settings")]
    public bool destroyOnDeath = false;
    public bool particlesOnDeath = false;
    public GameObject explosionParticles;
    [Header("Crash Settings")]
    public bool crashEnabled = true;
    public float crashSpeed = 35000f;
    public float turnSpeed = 1.5f;
    public float spinSpeed = 10000f;

    [Header("Starfighter")]
    public ParticleSystem starfighterCrashParticles;
    public ParticleSystem secondaryStarfighterCrashParticles;

    private GameObject shipModel;

    private Transform keeper;
    private Rigidbody rb;
    private bool doCrash = false;
    private Vector3 crashTarget;

    private float minCrashTime = 3;
    private float maxCrashTime = 8;
    private float crashTime = 0;
    private float crashTimer = 0;

    private void Start()
    {
        keeper = GameObject.Find("Keeper").transform;
        rb = GetComponent<Rigidbody>();
        doCrash = false;
        crashTimer = 0;
    }

    private void FixedUpdate()
    {
        if (doCrash)
        {
            if(crashTimer <= crashTime)
            {
                MoveTowards(crashTarget, crashSpeed, turnSpeed, spinSpeed);
            }
            else
            {
                HandleEndOfLife(shipModel);
                doCrash = false;
                crashTimer = 0;
            }
            crashTimer += Time.deltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (doCrash)
        {
            doCrash = false;
            HandleEndOfLife(shipModel);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<Projectile>() && doCrash && crashTimer > 1)
        {
            doCrash = false;
            HandleEndOfLife(shipModel);
        }
    }

    public void HandleStarfighterCrash(GameObject shipMesh)
    {
        if (crashEnabled)
        {
            int randomValue = Random.Range(1, 100);

            shipModel = shipMesh;

            if (randomValue > 50)
            {
                CrashStarfighter();
            }
            else
            {
                InstantExplode(shipMesh);
            }
        }
        else
        {
            InstantExplode(shipMesh);
        }
    }

    private void CrashStarfighter()
    {
        crashTarget = new Vector3(transform.position.x + Random.Range(-500f, 500f), transform.position.y - 5000f, transform.position.z + Random.Range(750f, 2000f));
        crashTime = Random.Range(minCrashTime, maxCrashTime);
        starfighterCrashParticles.Play();

        if(secondaryStarfighterCrashParticles != null)
        {
            secondaryStarfighterCrashParticles.Play();
        }

        doCrash = true;
    }

    public void InstantExplode(GameObject shipMesh)
    {
        HandleEndOfLife(shipMesh);
    }

    private void HandleEndOfLife(GameObject shipMesh)
    {

        starfighterCrashParticles.Stop();

        if (particlesOnDeath)
        {
            Instantiate(explosionParticles, shipMesh.transform.position, transform.rotation, keeper);
        }

        if (!destroyOnDeath)
        {
            if(shipMesh != null)
            {
                DisableShip(shipMesh);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void DisableShip(GameObject shipMesh)
    {
        rb.isKinematic = true;
        shipMesh.SetActive(false);
    }

    public void CancelCrash()
    {
        doCrash = false;
        starfighterCrashParticles.Stop();
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void MoveTowards(Vector3 target, float movementSpeed, float rotationSpeed, float spinSpeed)
    {
        // Calculate the direction to the current patrol point
        Vector3 targetDirection = target - transform.position;

        // Normalize the direction to have a consistent speed
        Vector3 moveDirection = targetDirection.normalized;

        // Calculate the rotation towards the patrol point
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        // Rotate smoothly towards the target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

        // Move forwards
        rb.AddRelativeForce(Vector3.forward * movementSpeed * Time.deltaTime);
        // Spin
        rb.AddRelativeTorque(Vector3.back * spinSpeed * Time.deltaTime);
    }
}
