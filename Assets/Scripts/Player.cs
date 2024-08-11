using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
using Random=UnityEngine.Random;
using Unity.Netcode;

public class Player : NetworkBehaviour
{  
     // 0 = idle
    // 1 = running
    // 2 = punch
    Color playerColor;
    public float health = 100;
    public int id;
    public int state = 0;
    public int weapon = 0; // 0 = fist, 1 = katana, 2 = gun
    // List<int> actionTimes = new List<int>(); // fill action times with https://discussions.unity.com/t/how-to-find-animation-clip-length/661298/3 and then make it so that i can toggle on and off states
    public float speed, dashCooldown;

    Rigidbody rb; 
    SpriteRenderer sr;
    Animator animator;
    CinemachineVirtualCamera vcam;
    PlayerInput playerInput;
    public GameObject gunLine, hand;
    public MaterialPropertyBlock mPB;
    InputAction special;

    bool attackButton = false, pAttackButton = false, switchButton = false, pSwitchButton = false, specialButton = false, pSpecialButton = false;
    Vector2 movementVector = Vector2.zero, lookVector= Vector2.zero;
    

    bool running = false, aiming = false, slowed = false, moonwalking = false, stunned = false, dashing = false, canDash = true;
    private NetworkVariable<bool> spriteFlip = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> gunLineEnable = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);



    

    public override void OnNetworkSpawn()
    {
        playerColor = new Color(Random.Range(50f,200f)/255f, Random.Range(50f,200f)/255f, Random.Range(50f,200f)/255f);
        rb=GetComponent<Rigidbody>();
        sr=GetComponentInChildren<SpriteRenderer>();
        animator=GetComponent<Animator>();
        playerInput=GetComponent<PlayerInput>();
        special=playerInput.actions["Special"];
        vcam = GameObject.Find("vcam").GetComponent<CinemachineVirtualCamera>();
        if(IsOwner)vcam.Follow=transform;
        mPB = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mPB);
        mPB.SetColor("_OutlineColor", playerColor);
        sr.SetPropertyBlock(mPB);

        spriteFlip.OnValueChanged += (bool previousValue, bool newValue) => {
            sr.flipX = newValue;
        };
        gunLineEnable.OnValueChanged += (bool previousValue, bool newValue) => {
            gunLine.SetActive(newValue);
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        if(stunned || dashing)return;
        running = movementVector.x!=0 || movementVector.y!=0;
        aiming = special.IsPressed();
        slowed = (aiming && weapon==2) || animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("punch") || animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.Equals("katanaSwing") ;

        
        rb.velocity = new Vector3(movementVector.x, 0, movementVector.y).normalized * (speed/((slowed)?2f:1f));
        if(specialButton && !pSpecialButton && canDash && weapon==1) StartCoroutine(dash());

        
       
        if(movementVector.x < 0) flipSprite(true);
        else if (movementVector.x > 0) flipSprite(false);
        
        if (weapon==2 && aiming) {
            if(lookVector.x < 0) {
                if(movementVector.x > 0) moonwalking = true;
                else moonwalking = false;
                flipSprite(true);
            }
            else if(lookVector.x > 0) {
                if(movementVector.x < 0) moonwalking = true;
                else moonwalking = false;
                flipSprite(false);
            } 
            else moonwalking = false;
        }
        else moonwalking=false;

        

        if(!running && animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.Equals("runningSwing")){
            print(animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f);
            animator.Play("katanaSwing",0,animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f);
        }

        animator.SetBool("running",running);
        animator.SetBool("aiming",aiming);
        animator.SetBool("moonwalking",moonwalking);

        if(attackButton && !pAttackButton) {
            if(!aiming && weapon == 2)return;
            animator.SetTrigger("attack");
        }
        if(switchButton && !pSwitchButton && !running){
            weapon+=1; if (weapon == 3) weapon=0;
            animator.SetInteger("weapon", weapon);
        }

        if(weapon==2 && aiming){
            if(!gunLine.activeSelf) gunToggle(true);
            rotateGunLine();
        }
        else gunToggle(false);

        pAttackButton = attackButton;
        pSwitchButton = switchButton;
        pSpecialButton = specialButton;
    }

    void rotateGunLine()
    {
        float x = lookVector.x, y = lookVector.y;
        if(x==0 && y==0) return;
        float angleRadians = Mathf.Atan2(y, x);
        float angleDegrees = -angleRadians * Mathf.Rad2Deg;
        gunLine.transform.parent.rotation = Quaternion.AngleAxis(angleDegrees, Vector3.up) * Quaternion.Euler(0,0,-2f);
    }



    public void onMove(InputAction.CallbackContext ctx) => movementVector=ctx.ReadValue<Vector2>();
    public void onLook(InputAction.CallbackContext ctx) {
        if(!IsOwner)return;
        if(ctx.control.displayName=="Delta")
            lookVector = Input.mousePosition -(Camera.main.WorldToScreenPoint(transform.position)+new Vector3(0,10,0));
        
        else lookVector = ctx.ReadValue<Vector2>();
    }
    public void onAttack(InputAction.CallbackContext ctx)=> attackButton=ctx.performed;
    public void onSwitch(InputAction.CallbackContext ctx)=> switchButton=ctx.performed;
    public void onSpecial(InputAction.CallbackContext ctx)=> specialButton=ctx.performed;

    void OnTriggerEnter(Collider col){
        if(dashing)return;
        if(col.gameObject.transform.parent == transform) return;
        if(col.gameObject.transform.parent == null || col.gameObject.transform.parent.gameObject.layer!=6)return;
        animator.SetTrigger("hurt");
        if((col.gameObject.transform.parent.position - transform.position).x < 0 && !sr.flipX) flipSprite(true);
        else if((col.gameObject.transform.parent.position - transform.position).x > 0 && sr.flipX) flipSprite(false);
        StartCoroutine(timeStop(0.15f, 0.3f, col.gameObject.transform.parent));
    }

    IEnumerator timeStop(float stopTime, float stunTime, Transform enemy){
        vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain=4f;
        rb.velocity = Vector3.zero;
        stunned=true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(stopTime);
        vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain=0f;
        for(int i = 0; i < 5; i++){
            Time.timeScale+=1f/5f;
            yield return new WaitForSeconds(stopTime/10f);
        }
        Time.timeScale = 1f;
        StartCoroutine(stun(stunTime));
    }

    IEnumerator stun(float stunTime){
        sr.GetPropertyBlock(mPB);
        mPB.SetColor("_Color", new Color(0.5f,0.5f,0.5f));
        sr.SetPropertyBlock(mPB);
        yield return new WaitForSeconds(stunTime);
        sr.GetPropertyBlock(mPB);
        mPB.SetColor("_Color", new Color(0f,0f,0f));
        sr.SetPropertyBlock(mPB);
        stunned=false;
    }

    IEnumerator dash(){
        canDash=false;
        dashing=true;
        animator.SetTrigger("dash");
        rb.velocity = new Vector3(((movementVector==Vector2.zero)?((sr.flipX)?-1:1):movementVector.x), 0, movementVector.y).normalized * (speed*4);
        yield return new WaitForSeconds(0.15f);
        rb.velocity=Vector3.zero;
        dashing=false;
        yield return new WaitForSeconds(dashCooldown);
        canDash=true;
    }

    public void flipSprite(bool val){
        if (!IsOwner) return;
        if(sr.flipX==val) return;
        spriteFlip.Value=val;
        if(val==true)hand.transform.localPosition = new Vector3(-0.34f,0.133f,0.0094f);
        else hand.transform.localPosition = new Vector3(0.34f,0.133f,0.0094f);
    }

    public void gunToggle(bool val){
        if (!IsOwner) return;
        if(gunLine.activeSelf == val) return;
        gunLineEnable.Value=val;
    }





}
