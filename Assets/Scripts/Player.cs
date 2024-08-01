using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
public class Player : MonoBehaviour
{   // 0 = idle
    // 1 = running
    // 2 = punch
    public int id;
    public int state = 0;
    public int weapon = 0; // 0 = fist, 1 = katana, 2 = gun
    // List<int> actionTimes = new List<int>(); // fill action times with https://discussions.unity.com/t/how-to-find-animation-clip-length/661298/3 and then make it so that i can toggle on and off states
    public float speed, groundDist;

    Rigidbody rb; 
    SpriteRenderer sr;
    Animator animator;
    CinemachineVirtualCamera vcam;
    public GameObject gunLine;

    bool running = false, aiming = false, slowed = false, moonwalking = false;

    void Start()
    {
        rb=GetComponent<Rigidbody>();
        sr=GetComponentInChildren<SpriteRenderer>();
        animator=GetComponent<Animator>();
        vcam = GameObject.Find("vcam").GetComponent<CinemachineVirtualCamera>();
        if(vcam.Follow==null)vcam.Follow=transform;
    }

    // Update is called once per frame
    void Update()
    {
        running = Input.GetAxisRaw("Horizontal")!=0 || Input.GetAxisRaw("Vertical")!=0;
        aiming = Input.GetAxisRaw("Trigger")==1;
        slowed = (aiming && weapon==2) || animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.StartsWith("punch");

        rb.velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * (speed/((slowed)?2f:1f));

        
        if(!aiming){
            if(Input.GetAxisRaw("Horizontal") < 0) sr.flipX = true;
            else if (Input.GetAxisRaw("Horizontal") > 0) sr.flipX = false;
            moonwalking=false;
        }
        else if (weapon==2) {
            if(Input.GetAxisRaw("RightHorizontal") < 0) {
                if(Input.GetAxisRaw("Horizontal") > 0) moonwalking = true;
                else moonwalking = false;
                sr.flipX = true;
            }
            else if(Input.GetAxisRaw("RightHorizontal") > 0) {
                if(Input.GetAxisRaw("Horizontal") < 0) moonwalking = true;
                else moonwalking = false;
                sr.flipX = false;
            } 
            else moonwalking = false;
        }

        animator.SetBool("running",running);
        animator.SetBool("aiming",aiming);
        animator.SetBool("moonwalking",moonwalking);

        //position gunLine based on player sprite
        if(sr.flipX) gunLine.transform.localPosition = new Vector3(-0.34f,0.133f,0.0094f);
        else gunLine.transform.localPosition = new Vector3(0.34f,0.133f,0.0094f);

        if(Input.GetButtonDown("Fire1")) {
            if(!aiming && weapon == 2)return;
            animator.SetTrigger("attack");
        }
        if(Input.GetButtonDown("Fire2") && !running){
            weapon+=1; if (weapon == 3) weapon=0;
            animator.SetInteger("weapon", weapon);
        }

        if(weapon==2 && aiming){
            if(!gunLine.activeSelf) gunLine.SetActive(true);
            rotateGunLine();
        }
        else gunLine.SetActive(false);
        
    }

    void rotateGunLine()
    {
        float x = Input.GetAxis("RightHorizontal"), y = Input.GetAxis("RightVertical");
        if(x==0 && y==0) return;
        float angleRadians = Mathf.Atan2(y, x);
        float angleDegrees = -angleRadians * Mathf.Rad2Deg;
        gunLine.transform.rotation = Quaternion.AngleAxis(angleDegrees, Vector3.up);
    }
}
