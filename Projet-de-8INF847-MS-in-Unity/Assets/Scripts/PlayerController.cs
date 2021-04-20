using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Transform player_View;
    private Transform firstPerson_Camera;

    private Vector3 player_View_Rotation = Vector3.zero;

    public float walkSpeed = 6.75f;
    public float runSpeed = 10f;
    public float crouchSpeed = 4f;
    public float jumpSpeed = 8f;
    public float gravity = 20f;

    private float speed;

    private bool is_Moving, is_Grounded, is_Crouching;

    private float inputX, inputZ;
    private float inputX_Set, inputZ_Set;
    private float inputModifyFactor;

    private bool limitDiagonalSpeed = true;

    private float antiBumpFactor = 0.75f;

    private CharacterController charController;
    private Vector3 moveDirection = Vector3.zero;

    public LayerMask groundLayer;
    private float rayDistance;
    private float default_ControllerHeight;
    private Vector3 default_Campos;
    private float camHeight;

    private PlayerAnimations playerAnimations;

    private Camera mainCam;
    public PlayerMouseLook[] mouseLook;

    // Start is called before the first frame update
    void Start()
    {
        player_View = transform.Find("bot View").transform;  //we can use"GameObject.find("bot View")"but we are looking for children
        charController = GetComponent<CharacterController>();
        speed = walkSpeed;
        is_Moving = false;

        rayDistance = charController.height * 0.5f + charController.radius;
        default_ControllerHeight = charController.height;
        default_Campos = player_View.localPosition;

        playerAnimations = GetComponent<PlayerAnimations>();


        mainCam = transform.Find("bot View").Find("Main Camera").GetComponent<Camera>();
        mainCam.gameObject.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {

        PlayerMovement();

    }
    void PlayerMovement()
    {
        //****************************************************
        //to move forward and backward
        //****************************************************
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
        {
            if (Input.GetKey(KeyCode.W))
            {
                inputZ_Set = 1f;
            }
            else
            {
                inputZ_Set = -1f;
            }
        }
        else
        {
            inputZ_Set = 0f;
        }
        //****************************************************
        //to move left and right
        //****************************************************
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            if (Input.GetKey(KeyCode.A))
            {
                inputX_Set = -1f;
            }
            else
            {
                inputX_Set = 1f;
            }
        }
        else
        {
            inputX_Set = 0f;
        }

        inputZ = Mathf.Lerp(inputZ, inputZ_Set, Time.deltaTime * 19f);
        inputX = Mathf.Lerp(inputX, inputX_Set, Time.deltaTime * 19f);

        //****************************************************
        //to avoid exceeding the limit speed while moving diagonnal
        //****************************************************

        inputModifyFactor = Mathf.Lerp(inputModifyFactor, (inputZ_Set != 0 && inputX_Set != 0 && limitDiagonalSpeed) ? 0.75f : 1f, Time.deltaTime * 19f);

        //****************************************************
        //player view rotation update
        //local rotation relative to its parent
        //****************************************************

        player_View_Rotation = Vector3.Lerp(player_View_Rotation, Vector3.zero, Time.deltaTime * 5f);
        player_View.localEulerAngles = player_View_Rotation;

        if (is_Grounded)
        {
            //HERE WE ARE GONNA CALL CROUCH AND SPRINT
            PlayerCrouchingAndSprinting();

            moveDirection = new Vector3(inputX * inputModifyFactor, -antiBumpFactor, inputZ * inputModifyFactor);

            moveDirection = transform.TransformDirection(moveDirection) * speed;

            //HERE WE ARE GONNA CALL JUMP
            PlayerJump();
        }
        moveDirection.y -= gravity * Time.deltaTime;

        is_Grounded = (charController.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;

        is_Moving = charController.velocity.magnitude > 0.15f;

        HandleAnimations();

    }
    void PlayerCrouchingAndSprinting()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!is_Crouching)
            {
                is_Crouching = true;
            }
            else
            {
                if (CanGetUp())
                {
                    is_Crouching = false;
                }
            }
            StopCoroutine(MoveCameraCrouch());
            StartCoroutine(MoveCameraCrouch());
        }
        if (is_Crouching)
        {
            speed = crouchSpeed;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed = runSpeed;
            }
            else
            {
                speed = walkSpeed;
            }
        }
        playerAnimations.PlayerCrouch(is_Crouching);
    }

    bool CanGetUp()
    {
        Ray groundRay = new Ray(transform.position, transform.up);
        RaycastHit groundHit;

        if (Physics.SphereCast(groundRay, charController.radius + 0.05f,
            out groundHit, rayDistance, groundLayer))
        {
            if (Vector3.Distance(transform.position, groundHit.point) < 2.3f)
            {
                return false;
            }
        }

        return true;
    }

    IEnumerator MoveCameraCrouch()
    {
        charController.height = is_Crouching ? default_ControllerHeight / 1.5f : default_ControllerHeight;
        charController.center = new Vector3(0f, charController.height / 2f, 0f);

        camHeight = is_Crouching ? default_Campos.y / 1.5f : default_Campos.y;

        while (Mathf.Abs(camHeight - player_View.localPosition.y) > 0.01f)
        {
            player_View.localPosition = Vector3.Lerp(player_View.localPosition,
                new Vector3(default_Campos.x, camHeight, default_Campos.z),
                Time.deltaTime * 11f);

            yield return null;
        }
    }

    void PlayerJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (is_Crouching)
            {
                if (CanGetUp())
                {
                    is_Crouching = false;

                    playerAnimations.PlayerCrouch(is_Crouching);

                    StopCoroutine(MoveCameraCrouch());
                    StartCoroutine(MoveCameraCrouch());
                }
            }
            else
            {
                moveDirection.y = jumpSpeed;
            }
        }

    }

    void HandleAnimations()
    {
        playerAnimations.Movement(charController.velocity.magnitude);
        playerAnimations.PlayerJump(charController.velocity.y);

        if (is_Crouching && charController.velocity.magnitude > 0f)
        {
            playerAnimations.PlayerCrouchWalk(charController.velocity.magnitude);
        }

    }
}
