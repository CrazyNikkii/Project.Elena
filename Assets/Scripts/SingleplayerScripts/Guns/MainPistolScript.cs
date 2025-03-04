using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MainPistolScript : MonoBehaviour
{
    // Weapon statistics
    [SerializeField] private float timeBetweenShooting, reloadTime, timeBetweenShots;
    [SerializeField] private int magazineSize, bulletsPerTap;
    [SerializeField] private bool allowButtonDown;
    int ammoLeftInMag, bulletsShot;
    public float damage = 20f;
    public float hSMultiplier = 2f;
    public int maxAmmo = 20;
    public int totalAmmo;
    public float normalFOV;
    public float scopedFOV = 50f;

    // States
    bool shooting, readyToShoot, reloading;
    public bool totalAmmoLeft;
    public bool aDS = false;
    public bool mainPistolStillActive;

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
    public PlayerController pC;

    // HUD
    public TextMeshProUGUI ammunitionDisplay;
    public TextMeshProUGUI totalAmmunitionDisplay;
    public TextMeshProUGUI reloadingText;

    // Debugging
    public bool allowInvoke = true;

    void Start()
    {
        // Sound reference
        gunSound = audioManager.GetComponent<AudioSource>();

        // Set ammo values to max and set the right state
        totalAmmo = maxAmmo;
        totalAmmoLeft = true;
        ammoLeftInMag = magazineSize;
        readyToShoot = true;
        reloadingText.SetText("");
        damage = 20f;
    }

    void Update()
    {
        CheckMainPistolActions();

        if (Input.GetButtonDown("Fire2"))
        {
            aDS = !aDS;
            animator.SetBool("pistolADS", aDS);

            if (aDS)
            {
                StartCoroutine(AimingDownSight());
            }
            else
            {
                UnAimingDownSight();
            }
        }

        // HUD ammocounter
        if (ammunitionDisplay != null)
        {
            ammunitionDisplay.SetText(ammoLeftInMag / bulletsPerTap + "/" + magazineSize / bulletsPerTap);
        }

        if (totalAmmunitionDisplay != null)
        {
            totalAmmunitionDisplay.SetText(totalAmmo + "");
        }

        // Check if any ammo left
        if (totalAmmo <= 0)
        {
            totalAmmoLeft = false;
        }
        else
        {
            totalAmmoLeft = true;
        }
    }

    void CheckMainPistolActions()
    {
        // Shooting button
        shooting = Input.GetKeyDown(KeyCode.Mouse0);

        // Reload button and reload if trying to shoot with empty magazine
        if (Input.GetKeyDown(KeyCode.R) && ammoLeftInMag < magazineSize && !reloading && totalAmmoLeft)
            ReloadMainPistol();
        if (readyToShoot && shooting && !reloading && ammoLeftInMag <= 0 && totalAmmoLeft)
            ReloadMainPistol();

        // Shoot if ammoleft in magazine
        if (readyToShoot && shooting && !reloading && ammoLeftInMag > 0 && gm.gamePaused == false)
        {
            bulletsShot = 0;
            ShootMainPistol();
        }
    }

    IEnumerator AimingDownSight()
    {
        yield return new WaitForSeconds(.10f);
        normalFOV = aimCam.fieldOfView;
        aimCam.fieldOfView = scopedFOV;
    }

    public void UnAimingDownSight()
    {
        aimCam.fieldOfView = normalFOV;
    }

    void ShootMainPistol()
    {
        // Set state
        readyToShoot = false;

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
                enemyAi.TakeHeadDamage(damage * hSMultiplier);
            }
            // Apply body damage
            else if (hit.collider is CapsuleCollider)
            {
                enemyAi.TakeBodyDamage(damage);
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

        // Play sound, muzzleflash and animation
        muzzleFlash.Play();
        animator.SetTrigger("pistolShoot");
        gunSound.PlayOneShot(gunSoundClip, 1f);

        // Decrease ammunition left
        ammoLeftInMag--;
        bulletsShot++;
        
        // Debugging
        if (allowInvoke)
        {
            Invoke("ResetShot", timeBetweenShooting);
            allowInvoke = false;
        }
    }

    public void ResetShot()
    {
        // Set states
        readyToShoot = true;
        animator.SetBool("pistolShoot", false);
        allowInvoke = true;
    }

    void ReloadMainPistol()
    {
        // Start reloading state
        reloading = true;
        reloadingText.SetText("Reloading...");

        // Wait for reloading time, then call finishing
        Invoke("ReloadMainPistolFinished", reloadTime);
        Debug.Log("Reloading");
    }

    public void ReloadMainPistolFinished()
    {
        if(mainPistolStillActive)
        {
            int reloadedAmmo = magazineSize - ammoLeftInMag;

            if (reloadedAmmo < totalAmmo)
            {
                ammoLeftInMag = magazineSize;
            }
            else
            {
                reloadedAmmo = totalAmmo;
                ammoLeftInMag = reloadedAmmo;
            }
            totalAmmo = totalAmmo - reloadedAmmo;

            // End reloading state
            reloading = false;
            reloadingText.SetText("");
            Debug.Log("Reloading Finished");
        }
    }

    void OnEnable()
    {
        mainPistolStillActive = true;
    }
    void OnDisable()
    {
        mainPistolStillActive = false;

        if (reloading)
        {
            // End reloading state
            reloading = false;
            reloadingText.SetText("");
            Debug.Log("Reloading Canceled");
            CancelInvoke("ReloadMainPistolFinished");
        }
    }
}
