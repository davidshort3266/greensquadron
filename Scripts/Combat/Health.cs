using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth = 100;
    public bool isPlayer;
    public bool isDead = false;
    [SerializeField] private bool isInvulnerable = false;
    [Header("(Deprecated, Use corresponding death scripts)")]
    public bool destroyOnDeath = true;
    public bool particlesOnDeath = false;
    private bool particlesOnce = false;
    private bool tookDamageFromPlayer;
    
    public GameObject deathParticles;

    public delegate void MyEventHandler();
    public delegate void MyPlayerDeathEventHandler(string attackerID, bool selfInf, int playerNumber);
    public delegate void MyAIDeathEventHandler(string attackerID, bool selfInf, int playerNumber);
    public delegate void MyPlayerKillEventHandler(string attackerID, bool selfInf, int playerNumber);
    public delegate void MyTargetKillEventHandler(int score, Target.team team);
    public delegate void MyPlayerScoreEventHandler(int score, int playerNumber);

    public static event MyEventHandler OnTakeDamage;
    public static event MyPlayerDeathEventHandler OnPlayerDeath;
    public static event MyPlayerKillEventHandler OnPlayerKill;
    public event MyAIDeathEventHandler OnAIDeath;
    public static event MyTargetKillEventHandler OnTargetKill;
    public static event MyPlayerScoreEventHandler OnPlayerScore;


    Transform keeper;

    private void Start()
    {
        keeper = GameObject.Find("Keeper").transform;
        isDead = false;
        tookDamageFromPlayer = false;
        if(currentHealth <= 0)
        {
            Die("No Health", false, -2);
        }
    }

    private void Update()
    {
        //if (currentHealth <= 0)
        //{
        //    Die();
        //}
    }
    public void TakeDamage(int damage, string attackerID, bool selfInf, int playerNumber)
    {
        if (!isDead && !isInvulnerable)
        {
            currentHealth -= damage;

            if (currentHealth <= 0)
            {
                Die(attackerID, selfInf, playerNumber);
            }

            if (isPlayer)
            {
                OnTakeDamage?.Invoke();
            }
            else
            {
                if (playerNumber > 0)
                {
                    tookDamageFromPlayer = true;
                }

                if(playerNumber == -1)
                {
                    tookDamageFromPlayer = false;
                }
            }
        }
    }

    private void Die(string attackerID, bool selfInf, int playerNumber)
    {
        //Handle death
        isDead = true;

        if (particlesOnDeath && !particlesOnce)
        {
            Instantiate(deathParticles, transform.position, Quaternion.identity, keeper);
            particlesOnce = true;
        }

        if (isPlayer)
        {
            OnPlayerDeath?.Invoke(attackerID, selfInf, playerNumber);
        }
        else
        {
            OnAIDeath?.Invoke(attackerID, selfInf, playerNumber);
        }

        if(tookDamageFromPlayer)
        {
            OnPlayerKill?.Invoke(attackerID, selfInf, playerNumber);
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }

        Target myTarget = GetComponent<Target>();

        if (myTarget != null)
        {
            if (!selfInf)
            {
                OnTargetKill?.Invoke(myTarget.killScore, myTarget.myTeam);
            }
            if(isPlayer && selfInf)
            {
                if(myTarget.suicidePenalty != 0)
                {
                    OnTargetKill?.Invoke(myTarget.suicidePenalty, myTarget.myTeam);
                }
            }
            if (tookDamageFromPlayer)
            {
                OnPlayerScore?.Invoke(myTarget.killScore, playerNumber);
            }
        }
    }

    public void Heal(int health)
    {
        if (currentHealth != maxHealth)
        {
            currentHealth += health;

            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
        }
    }

    public void ResetParticles()
    {
        particlesOnce = false;
    }

    public void SetInvulnerable(bool set)
    {
        isInvulnerable = set;
    }
}
