using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AssaultRifleScript : MonoBehaviour
{
    // Weapon statistics
    [SerializeField] private float timeBetweenShooting, reloadTime, timeBetweenShots;
    [SerializeField] private int magazineSize, bulletsPerTap;
    [SerializeField] private bool fullAutoMode;
    int ammoLeftInARMag, aRBulletsShot;
    public float aRDamage = 20f;
    public float hSMultiplier = 3f;
    public int aRMaxAmmo = 20;
    public int aRTotalAmmo;
    public float scopedFOV = 15f;
    private float normalFOV;

    // States
    bool aRShooting, aRReadyToShoot, aRReloading;
    public bool aRTotalAmmoLeft;
    public bool aDS = false;
    public bool aRStillActive;

    // References
    public Camera aimCam;
    public AudioSource gunSound;
    public AudioClip gunSoundClip;
    public GameObject audioManager;
    public GameObject bulletHole;
    public GameManager gm;
    public EnemyAi enemyAi;
    public ParticleSystem muzzleFlash;
    public GameObject bHContainer;
    public Animator animator;
    public GameObject scopeRedDot;
    public PlayerController pC;
    public GameObject flashLight;

    // HUD
    public TextMeshProUGUI ammunitionDisplay;
    public TextMeshProUGUI totalAmmunitionDisplay;
    public TextMeshProUGUI reloadingText;

    // Debugging
    public bool allowInvoke = true;

    void Start()
    {
        // Sound reference
        gunSound = GetComponent<AudioSource>();
        gunSound = audioManager.GetComponent<AudioSource>();

        // Set ammo values to max and set the right states
        aRTotalAmmo = aRMaxAmmo;
        aRTotalAmmoLeft = true;
        ammoLeftInARMag = magazineSize;
        aRReadyToShoot = true;
        reloadingText.SetText("");
        aRDamage = 20f;
        
    }

    void Update()
    {
        CheckAssaultRifleActions();

        // Toggle Firemode
        if (Input.GetKeyDown(KeyCode.B))
        {
            fullAutoMode = !fullAutoMode;
        }

        // HUD magazine ammo counter
        if (ammunitionDisplay != null)
        {
            ammunitionDisplay.SetText(ammoLeftInARMag / bulletsPerTap + "/" + magazineSize / bulletsPerTap);
        }

        //  HUD total ammo counter
        if (totalAmmunitionDisplay != null)
        {
            totalAmmunitionDisplay.SetText(aRTotalAmmo + "");
        }

        // Check if any ammo left
        if (aRTotalAmmo <= 0)
        {
            aRTotalAmmoLeft = false;
        }
        else
        {
            aRTotalAmmoLeft = true;
        }
    }

    void CheckAssaultRifleActions()
    {
        // Aiming down sights
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            aDS = !aDS;
            animator.SetBool("aimingDownSight", aDS);

            if (aDS)
            {
                StartCoroutine(AimingDownSight());
            }
            else
            {
                UnAimingDownSight();
            }
        }

        // If weapon is not on full auto, only reads the input once
        if (fullAutoMode) aRShooting = Input.GetKey(KeyCode.Mouse0);
        else aRShooting = Input.GetKeyDown(KeyCode.Mouse0);

        // Reload button and reload if trying to shoot with empty magazine
        if (Input.GetKeyDown(KeyCode.R) && ammoLeftInARMag < magazineSize && !aRReloading && aRTotalAmmoLeft)
            ReloadAssaultRifle();
        if (aRReadyToShoot && aRShooting && !aRReloading && ammoLeftInARMag <= 0 && aRTotalAmmoLeft)
            ReloadAssaultRifle();

        // Turn on/off the flashlight
        if(Input.GetKeyDown(KeyCode.T))
        {
            if(flashLight.activeInHierarchy)
            {
                flashLight.SetActive(false);
            }
            else
            {
                flashLight.SetActive(true);
            }
        }

        // Shoot if ammoleft in magazine
        if (aRReadyToShoot && aRShooting && !aRReloading && ammoLeftInARMag > 0 && gm.gamePaused == false)
        {
            aRBulletsShot = 0;
            ShootAssaultRifle();
        }
    }

    void ShootAssaultRifle()
    {
        // Set state
        aRReadyToShoot = false;

        // Raycast
        Vector3 rayOrigin = aimCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f));
        RaycastHit hit;

        // Check if raycast hits a dummy, headshot multiplies damage
        if (Physics.Raycast(rayOrigin, aimCam.transform.forward, out hit))
        {
            EnemyAi enemyAi = hit.transform.GetComponent<EnemyAi>();

            // Check if headshot, apply headshot damage
            if (hit.collider is SphereCollider)
            {
                enemyAi.TakeHeadDamage(aRDamage * hSMultiplier);
            }
            // Apply body damage
            else if (hit.collider is CapsuleCollider)
            {
                enemyAi.TakeBodyDamage(aRDamage);
            }
           
            // If not hitting a dummy, make a bullet hole
            else
            {

                //Instantiate the bullet hole on the hit point of the raycast, offset by 0.001 to avoid clipping
                GameObject bH = Instantiate(bulletHole, hit.point + hit.normal * 0.001f, Quaternion.identity) as GameObject;
                bH.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                float randomBHRot = Random.Range(0f, 360f);
                bH.transform.Rotate(0, randomBHRot, 0f);
                bH.transform.SetParent(bHContainer.transform);
                Destroy(bH, 5f);
            }


        }

        // Play muzzleflash
        muzzleFlash.Play();
        if(aDS == true)
        {
            animator.SetTrigger("arADSShoot");
        }
        else if (aDS == false)
        {
            animator.SetTrigger("arShoot");
        }

        // Play sound
        gunSound.PlayOneShot(gunSoundClip, 1f);

        // Decrease ammunition left
        ammoLeftInARMag--;
        aRBulletsShot++;

        // Debugging
        if (allowInvoke)
        {
            Invoke("ResetShot", timeBetweenShooting);
            allowInvoke = false;
        }
    }

    IEnumerator AimingDownSight()
    {
        // Waits for 0.1s before zooming with the camera. Turns the red dot on. Changes correct states.
        yield return new WaitForSeconds(.10f);
        normalFOV = aimCam.fieldOfView;
        aimCam.fieldOfView = scopedFOV;
        scopeRedDot.SetActive(true);
        pC.isScoped = true;
    }

    public void UnAimingDownSight()
    {
        // Changes correct states. Turns red dot on.
        Debug.Log("Unaiming called");
        aimCam.fieldOfView = normalFOV;
        scopeRedDot.SetActive(false);
        pC.isScoped = false;
    }

    public void ResetShot()
    {
        // Set states
        aRReadyToShoot = true;
        allowInvoke = true;
    }

    void ReloadAssaultRifle()
    {
        // Start reloading state
        aRReloading = true;
        reloadingText.SetText("Reloading...");

        // Wait for reloading time, then call finishing
        Invoke("ReloadAssaultRifleFinished", reloadTime);
        Debug.Log("Reloading");
    }

    public void ReloadAssaultRifleFinished()
    {
        // Player can't reload a weapon he is not currently holding
        if(aRStillActive)
        {
            // Count how many bullets were loaded into the magazine
            int reloadedAmmo = magazineSize - ammoLeftInARMag;

            // Fully loads the magazine if player has enough total ammo left. Otherwise loads all the ammo player has left into the magazine
            if (reloadedAmmo < aRTotalAmmo)
            {
                ammoLeftInARMag = magazineSize;
            }
            else
            {
                reloadedAmmo = aRTotalAmmo;
                ammoLeftInARMag = reloadedAmmo;
            }
            // Decrease total ammo by the amount of ammo reloaded into the magazine
            aRTotalAmmo = aRTotalAmmo - reloadedAmmo;

            // End reloading state
            aRReloading = false;
            reloadingText.SetText("");
            Debug.Log("Reloading Finished");

        }
    }

    void OnEnable()
    {
        aRStillActive = true;
    }

    void OnDisable()
    {
        aRStillActive = false;

        animator.SetBool("aimingDownSight", false);

        if (aRReloading)
        {
            // End reloading state
            aRReloading = false;
            reloadingText.SetText("");
            Debug.Log("Reloading Canceled");
            CancelInvoke("ReloadAssaultRifleFinished");
        }
    }
}
