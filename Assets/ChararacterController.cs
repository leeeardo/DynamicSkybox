using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChararacterController : MonoBehaviour
{
    private Animator _animator;
    private CharacterController _controller;
    private bool isJumping=false,isJumpMoveing=false;
    public float speed=1;
    public float rotationSpeed=1;

    private float Gravity = 9.8f;
    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
    }

    private void Movement()
    {
        if (isJumping)
        {
            if (isJumpMoveing)
            {
                _controller.Move(transform.forward *speed*1.5f * Time.deltaTime);
            }
            return;
        }
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");
        var isRunning = Input.GetKey(KeyCode.LeftShift);
        var move = transform.forward *speed* vertical * Time.deltaTime;
        move = isRunning ? 2 * move : move;
        _controller.Move(move);

        if (!_controller.isGrounded)
        {
            _controller.Move(Vector3.down * Gravity*Time.deltaTime);
        }
        
        
        transform.Rotate(Vector3.up,horizontal*rotationSpeed);
        
        _animator.SetFloat("VerticalMovement", vertical*0.5f+0.5f);
        _animator.SetBool("Run",isRunning);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isJumping = true;
            _animator.SetTrigger("Jump");
        }
    }

    public void JumpFinishedEvent()
    {
        isJumping = false;
    }

    public void JumpMovementEvent()
    {
        isJumpMoveing = !isJumpMoveing;
    }
}
