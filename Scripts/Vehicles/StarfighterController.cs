using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

public class StarfighterController : MonoBehaviour
{
    [Header("Utility Transforms - for scene setup")]
    public Transform tiltModel;
    public Transform fighterModel;
    public Transform camPos;
    public Transform aimStart;
    public Transform crosshair;

    private StarfighterFlight starFlight;
    private StarfighterCombat starComb;
    private StarfighterAudio starAud;
    private PlayerTargeting pTargeting;
    private PlayerCameraEffects camFX;

    private SaveManager save;

    private void Awake()
    {
        starFlight = GetComponent<StarfighterFlight>();
        starComb = GetComponent<StarfighterCombat>();
        starAud = GetComponent<StarfighterAudio>();
        pTargeting = GetComponent<PlayerTargeting>();
        camFX = GetComponent<PlayerCameraEffects>();

        save = FindFirstObjectByType<SaveManager>();
    }

    private void Start()
    {
        InvertPitch(save.LoadInvertPitchSetting());
        SwapControllerSticks(save.LoadSwapControllerSticksOption());
        SwapBoostAndDodgeButtons(save.LoadSwapBoostAndDodgeButtonsOption());
        UseDpadForLanding(save.LoadUseDpadForLandingOption());
    }

    public void SetStarfighter(bool enable)
    {
        starFlight.isActive = enable;
        starComb.isActive = enable;
        starAud.isActive = enable;
    }

    public void SetStarfighterAudio(bool enable)
    {
        starAud.isActive = enable;
    }

    public void InvertPitch(bool invert)
    {
        starFlight.invertPitch = invert;
    }

    public void SwapControllerSticks(bool swap)
    {
        starFlight.swapControllerSticks = swap;
    }

    public void SwapBoostAndDodgeButtons(bool swap)
    {
        starFlight.swapBoostAndDodgeButtons = swap;
    }

    public void UseDpadForLanding(bool enable)
    {
        starFlight.useDpadForLanding = enable;
    }

    public void SetStarfighterToFlightMode()
    {
        starFlight.isLanding = false;
        starFlight.hasLanded = false;
    }

    public void MoveToRespawnPoint(Transform respawnPoint)
    {
        transform.position = respawnPoint.position;

        transform.rotation = respawnPoint.rotation;
    }

    public void ResetAmmo()
    {
        starComb.ResetMissileCount();
    }
    
    public void ActivateStarterBoost()
    {
        starFlight.isBoosting = true;
    }

    public void SetPlayerBoost(bool boosting)
    {
        starFlight.isBoosting = boosting;
    }

    public void CancelLanding()
    {
        starFlight.isLanding = false;
        starFlight.hasLanded = false;
    }

    public void ResetPlayerTargeting()
    {
        pTargeting.ResetTargeting();
    }

    public float GetStarfighterSensitivity()
    {
        return starFlight.starfighterAimSensitivity;
    }

    public void SetStarfighterSensitivity(float sensitivity)
    {
        starFlight.starfighterAimSensitivity = sensitivity;
    }

    public void SetPlayerNumber(int playerNumber)
    {
        starComb.playerNumber = playerNumber;
    }

    public void ResetBoostParticles()
    {
        starFlight.SetBoostParticles(false);
    }

    public void EnablePlayerDamageFX(bool enabled)
    {
        camFX.SetDamageEffectEnabled(enabled);
    }
}
