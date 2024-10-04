using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarfighterCollision : MonoBehaviour
{
    public bool isActive = true;
    [Header("Collision Options")]
    public float minBounceVelocity = 2f;
    public float bounceForce = 10f;
    [Header("Damage Options")]
    public int minDamage = 25;
    public float damageMultiplier = 0.1f;
    public float collisionStayDamageTime = 2.0f;
    public int stayDamageAmount = 300;
    public float bounceCooldown = 1f;

    private bool canBounce = true;
    private bool collisionStaying = false;
    private float contactTimer;

    private Health health;
    private Rigidbody shipRigidbody;
    private StarfighterAudio starAud;

    private Collision stayCollision;

    private float damageTimer = 0f;

    private void Start()
    {
        starAud = GetComponent<StarfighterAudio>();
        shipRigidbody = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        collisionStaying = false;
    }

    private void OnCollisionEnter(Collision collision)
    {

        float collisionVelocity = collision.relativeVelocity.magnitude;

        if (isActive && canBounce && collisionVelocity > minBounceVelocity)
        {
            // Calculate damage based on velocity.
            int damage = Mathf.Max(minDamage, Mathf.RoundToInt(collisionVelocity * damageMultiplier));

            // Apply damage to the player's health.
            health.TakeDamage(damage, "Themself", true, -2);

            starAud.PlayCollisionSound();
            //Debug.Log("Collision Enter");

            damageTimer = collisionStayDamageTime;

            foreach (ContactPoint contact in collision.contacts)
            {
                Vector3 bounceDirection = Vector3.Reflect(-collision.relativeVelocity.normalized, contact.normal);

                ApplyBounceForce(bounceDirection);
                AimAwayFromSurface(bounceDirection);

                // Start the cooldown timer
                canBounce = false;
                StartCoroutine(BounceCooldown());
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collisionStaying || !isActive)
            return;

        collisionStaying = true;
        StartCoroutine(HandleCollision(collision));
    }

    private IEnumerator HandleCollision(Collision collision)
    {
        while (collisionStaying && isActive)
        {
            contactTimer += Time.deltaTime;

            if (contactTimer >= collisionStayDamageTime)
            {
                health.TakeDamage(stayDamageAmount, "Themself", true, -2);
                starAud.PlayCollisionSound();
                //Debug.Log("Collision Staying and my active status is " + isActive);
                contactTimer = 0f; // Reset the contact timer after applying damage
            }

            yield return null; // Wait for the next frame
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        collisionStaying = false;

        contactTimer = 0;
    }

    private IEnumerator BounceCooldown()
    {
        yield return new WaitForSeconds(bounceCooldown);
        canBounce = true;
    }

    private void ApplyBounceForce(Vector3 direction)
    {
        shipRigidbody.AddForce(direction * bounceForce, ForceMode.Impulse);
    }

    private void AimAwayFromSurface(Vector3 direction)
    {
        Quaternion newRotation = Quaternion.LookRotation(-direction, Vector3.up);
        shipRigidbody.MoveRotation(newRotation);
    }
}
