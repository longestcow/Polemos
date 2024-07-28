using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{   // 0 = idle
    // 1 = running
    // 2 = punch
    public int state = 0;
    List<int> actionTimes = new List<int>(); // fill action times with https://discussions.unity.com/t/how-to-find-animation-clip-length/661298/3 and then make it so that i can toggle on and off states

    public float speed, groundDist;


    public LayerMask terrainLayer;
    Rigidbody rb; 
    SpriteRenderer sr;
    Animator animator;
    /*
    *
    * fix punch, player state set up 
    *
    */
    // Start is called before the first frame update
    void Start()
    {
        rb=GetComponent<Rigidbody>();
        sr=GetComponentInChildren<SpriteRenderer>();
        animator=GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

        RaycastHit hit;
        Vector3 castPos = transform.position; castPos.y+=1;

        if(Physics.Raycast(castPos, -transform.up, out hit, Mathf.Infinity, terrainLayer)){ //correcting height for terrain
            if(hit.collider != null){
                Vector3 movePos = transform.position;
                movePos.y = hit.point.y + groundDist;
                transform.position = movePos;
            }
        }

        rb.velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")) * speed;
        animator.SetBool("running",rb.velocity!=Vector3.zero);//add walking vs running animations
        if(Input.GetAxisRaw("Horizontal") < 0) sr.flipX = true;
        else if (Input.GetAxisRaw("Horizontal") > 0) sr.flipX = false;

        if(Input.GetButtonDown("Fire1")){
            print("punch");
            animator.SetTrigger("punch");
            state=2;
        }


        
    }
}
