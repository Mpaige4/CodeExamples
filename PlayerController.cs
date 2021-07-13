using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{

    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    private float verticalRotStore;
    private Vector2 mouseInput;
    public bool invertLook;

    public float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDir , movement;

    public CharacterController charCon;

    private Camera cam;

    public float jumpForce = 12f, gravityMod = 2.5f;

    public Transform groundCheckPoint;
    public bool isGrounded;
    public LayerMask groundLayers;

    public GameObject bulletImpact;
   // public float timeBetweenShots;
    private float shotCounter;
    public float muzzleDisplayTime;
    private float muzzleCounter;

    public float maxHeat = 15f, /* heatPerShot = 1f ,*/ coolRate = 4f, overheatCool = 5f ;
    private float heatCounter;
    private bool overHeated;

    

    public Gun[] allGuns;
    private int selectedGun;

    public GameObject playerHitImpact;

    public int maxHealth = 100;
    private int currentHealth;

    public Animator anim;
    public GameObject playerModel;

    public Transform modelGunPoint, gunHolder;

    public Material[] skins;

    public float adsSpeed = 5f;

    public Transform adsOut, adsIn;

    public AudioSource footStepSlow, footStepFast;
    
    // Start is called before the first frame update
    void Start()
    {
        
       
        Cursor.lockState = CursorLockMode.Locked;
        cam = Camera.main;

        //SwitchGun();
        photonView.RPC("SetGun", RpcTarget.All, selectedGun);


        currentHealth = maxHealth;

        if(photonView.IsMine)
        {
            playerModel.SetActive(false);
            UIController.instance.weaponTempSlider.maxValue = maxHeat;
            UIController.instance.healthSlider.maxValue = maxHealth;
            UIController.instance.healthSlider.value = currentHealth;
        }
        else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }

        playerModel.GetComponent<Renderer>().material = skins[photonView.Owner.ActorNumber % skins.Length];

        //Transform newTransform = SpawnManager.instance.GetSpawnPoint();
        //transform.position = newTransform.position;
        //transform.rotation = newTransform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
       
        if(photonView.IsMine)
        {
            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            //rotations use Quaternions & use XYZW

            //Euler will make it so we can use Vector3 

            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

            verticalRotStore += mouseInput.y;

            //the clamp is used to limit the angle at which we can look 
            verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);


            if (invertLook)
            {
                viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }
            else
            {

                viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }

            //normalize will make it so that while moving diagonal, it will not be faster than moving forward or sideways
            moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));


            if (Input.GetKey(KeyCode.LeftShift))
            {
                activeMoveSpeed = runSpeed;
                if(!footStepFast.isPlaying && moveDir != Vector3.zero) //not standing still or walking
                {
                    footStepFast.Play();
                    footStepSlow.Stop();
                }
            }
            else
            {
                activeMoveSpeed = moveSpeed;
                if (!footStepSlow.isPlaying && moveDir != Vector3.zero) //not standing still or walking
                {
                    footStepSlow.Play();
                    footStepFast.Stop();
                    
                }

            }

            if(moveDir == Vector3.zero || !isGrounded)
            {
                footStepSlow.Stop();
                footStepFast.Stop();
            }



            float yVel = movement.y;

            //to start using gravity we moved activemovespeed to movement to not modify the y
            movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;
            movement.y = yVel;


            if (charCon.isGrounded)
            {
                movement.y = 0f;
            }

            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);

            if (Input.GetButton("Jump") && isGrounded)
            {

                movement.y = jumpForce;
            }


            movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

            charCon.Move(movement * Time.deltaTime);

            if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
            {
                muzzleCounter -= Time.deltaTime;

                if (muzzleCounter <= 0)
                {
                    allGuns[selectedGun].muzzleFlash.SetActive(false);
                }


            }



            if (!overHeated)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Shoot();

                }

                if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
                {
                    shotCounter -= Time.deltaTime;
                    if (shotCounter <= 0)
                    {
                        Shoot();
                    }
                }

                heatCounter -= coolRate * Time.deltaTime;

            }
            else
            {
                heatCounter -= overheatCool * Time.deltaTime;
                if (heatCounter <= 0)
                {
                    heatCounter = 0;
                    overHeated = false;
                    UIController.instance.overheatedMessage.gameObject.SetActive(false);
                }

            }

            if (heatCounter < 0)
            {
                heatCounter = 0f;
            }

            UIController.instance.weaponTempSlider.value = heatCounter;


            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                selectedGun++;

                if (selectedGun >= allGuns.Length)
                {
                    selectedGun = 0;

                }
                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                selectedGun--;
                if (selectedGun < 0)
                {
                    selectedGun = allGuns.Length - 1;
                }
                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }



            for (int i = 0; i < allGuns.Length; i++)
            {

                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    selectedGun = i;
                    //SwitchGun();
                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                }


            }

            anim.SetBool("grounded", isGrounded);
            anim.SetFloat("speed", moveDir.magnitude);


            if(Input.GetMouseButton(1))
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, allGuns[selectedGun].adsZoom, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsIn.position, adsSpeed * Time.deltaTime);
            }
            else
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 60f, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsOut.position, adsSpeed * Time.deltaTime);
            }


            if (Input.GetKey(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState == CursorLockMode.None && !UIController.instance.optionsScreen.activeInHierarchy)
            {
                if (Input.GetMouseButton(0))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }

        }



    }

    private void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = cam.transform.position;
        
        
        if(Physics.Raycast(ray, out RaycastHit hit))
        {
      
           //Debug.Log("We hit " + hit.collider.gameObject.name);

            

            if(hit.collider.tag == "Player")
            {
                Debug.Log("Hit " + hit.collider.gameObject.GetPhotonView().Owner.NickName);
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);
                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].shotDamage, PhotonNetwork.LocalPlayer.ActorNumber) ;
            }
            else
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.point * -.002f), Quaternion.LookRotation(hit.normal, Vector3.up));

                Destroy(bulletImpactObject, 1f);
            }

          
        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;

        heatCounter += allGuns[selectedGun].heatPerShot;
        if(heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;

            overHeated = true;

            UIController.instance.overheatedMessage.gameObject.SetActive(true);

        }

        
        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;
        allGuns[selectedGun].shotSound.Stop();
        allGuns[selectedGun].shotSound.Play();

    }

    
    
    [PunRPC]
    public void DealDamage(string damager, int damageAmount, int actor)
    {
        TakeDamage(damager, damageAmount, actor);
    }

    public void TakeDamage(string damager, int damageAmount, int actor)
    {
        if(photonView.IsMine)
        {
            //Debug.Log(photonView.Owner.NickName + " been hit by " + damager);

            currentHealth -= damageAmount;
            UIController.instance.healthSlider.value = currentHealth;

            
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                PlayerSpawner.instance.Die(damager);

                MatchManager.instance.UpdateStatsSend(actor, 0, 1);
            }
            
            
            
        }
        
    }
    
    
    private void LateUpdate()
    {
        if(photonView.IsMine)
        {
            if(MatchManager.instance.state == MatchManager.GameState.Playing)
            {
                cam.transform.position = viewPoint.position;
                cam.transform.rotation = viewPoint.rotation;

            }

            if (MatchManager.instance.state == MatchManager.GameState.Ending)
            {
                cam.transform.position = MatchManager.instance.mapCamPoint.position;
                cam.transform.rotation = MatchManager.instance.mapCamPoint.rotation;
            }
        }
        
    }

    public void SwitchGun()
    {
        foreach(Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].muzzleFlash.SetActive(false);
        heatCounter = 0;

    }

    [PunRPC]
    public void SetGun(int gunToSwitchTo)
    {
        if(gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }



}

