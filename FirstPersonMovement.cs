using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;

    [Header("Headbob Settings")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.11f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.025f;

    [Header("Jump Sounds")]
    [SerializeField] private AudioClip[] jumpClips;

    [Header("Crouch Sounds")]
    [SerializeField] private AudioClip[] crouchDownClips;
    [SerializeField] private AudioClip[] crouchUpClips;

    [Header("Keybinds")]
    [SerializeField] public KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] public KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Footstep Sounds (Terrain)")]
    [Tooltip("Pași pentru Grass, Stone, Sand etc. - ordinea trebuie să fie identică cu Terrain Layers din Inspector la teren.")]
    [SerializeField] private AudioClip[] grassFootsteps;
    [SerializeField] private AudioClip[] stoneFootsteps;
    [SerializeField] private AudioClip[] sandFootsteps;

    [Tooltip("Interval minim între pași (secunde)")]
    [SerializeField] private float footstepInterval = 0.5f;

    private CharacterController controller;
    private Camera playerCamera;
    private AudioSource audioSource;

    private Vector3 moveDirection;
    private float rotationX;
    private float defaultYPos;
    private Vector2 currentInput;

    private bool isCrouching;
    private bool duringCrouchAnimation;
    private float bobTimer;
    private float footstepTimer;

    public Vector2 CurrentInput => currentInput;
    public bool IsGrounded => controller.isGrounded;
    public bool CanMove { get; set; } = true;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        defaultYPos = playerCamera.transform.localPosition.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!CanMove) return;

        HandleMovement();
        HandleMouseLook();
        HandleJump();
        HandleCrouch();
        HandleHeadbob();
        HandleFootsteps();
        ApplyGravity();
        ApplyMovement();
    }

    private void HandleMovement()
    {
        bool isTryingToSprint = Input.GetKey(sprintKey) && !isCrouching;

        float speed = walkSpeed;

        if (isCrouching)
            speed = crouchSpeed;
        else if (isTryingToSprint)
            speed = sprintSpeed;

        currentInput = new Vector2(Input.GetAxis("Vertical") * speed, Input.GetAxis("Horizontal") * speed);

        float y = moveDirection.y;
        moveDirection = (transform.forward * currentInput.x) + (transform.right * currentInput.y);
        moveDirection.y = y;
    }

    private void HandleMouseLook()
    {
        float lookX = Input.GetAxis("Mouse X");
        float lookY = Input.GetAxis("Mouse Y");

        rotationX -= lookY;
        rotationX = Mathf.Clamp(rotationX, -80f, 80f);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.Rotate(Vector3.up * lookX);
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(jumpKey))
        {
            if (controller.isGrounded)
            {
                moveDirection.y = jumpForce;
                PlayJumpSound();
            }
        }
    }

    private void PlayJumpSound()
    {
        if (jumpClips.Length > 0)
        {
            AudioClip clip = jumpClips[Random.Range(0, jumpClips.Length)];
            audioSource.PlayOneShot(clip);
        }
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(crouchKey) && controller.isGrounded && !duringCrouchAnimation)
        {
            StartCoroutine(CrouchStand());
        }
    }

    private IEnumerator CrouchStand()
    {
        if (isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f))
            yield break;

        PlayCrouchSound(!isCrouching);

        duringCrouchAnimation = true;
        float timeElapsed = 0f;
        float startHeight = controller.height;
        float targetHeight = isCrouching ? standingHeight : crouchHeight;

        while (timeElapsed < timeToCrouch)
        {
            controller.height = Mathf.Lerp(startHeight, targetHeight, timeElapsed / timeToCrouch);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        controller.height = targetHeight;
        isCrouching = !isCrouching;
        duringCrouchAnimation = false;
    }

    private void PlayCrouchSound(bool toCrouch)
    {
        AudioClip[] clips = toCrouch ? crouchDownClips : crouchUpClips;
        if (clips != null && clips.Length > 0)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioSource.PlayOneShot(clip);
        }
    }

    private void HandleHeadbob()
    {
        if (!controller.isGrounded || currentInput.magnitude <= 0.1f)
        {
            bobTimer = 0f;
            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x,
                Mathf.Lerp(playerCamera.transform.localPosition.y, defaultYPos, Time.deltaTime * 8f),
                playerCamera.transform.localPosition.z
            );
            return;
        }

        float speed = walkBobSpeed;
        float amount = walkBobAmount;

        if (Input.GetKey(sprintKey) && !isCrouching)
        {
            speed = sprintBobSpeed;
            amount = sprintBobAmount;
        }
        else if (isCrouching)
        {
            speed = crouchBobSpeed;
            amount = crouchBobAmount;
        }

        bobTimer += Time.deltaTime * speed;
        playerCamera.transform.localPosition = new Vector3(
            playerCamera.transform.localPosition.x,
            defaultYPos + Mathf.Sin(bobTimer) * amount,
            playerCamera.transform.localPosition.z
        );
    }

    private void HandleFootsteps()
    {
        // Joacă pas doar dacă te miști și ești pe pământ
        if (controller.isGrounded && currentInput.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                PlayFootstep();
                footstepTimer = footstepInterval / (Input.GetKey(sprintKey) ? 1.5f : 1f); // pași mai rapizi la sprint
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        // Raycast sub player pentru a detecta tipul de teren (Terrain Layer)
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out hit, 2f))
        {
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                TerrainData terrainData = terrain.terrainData;
                Vector3 terrainPos = terrain.transform.position;

                // Coordonate pentru alphamap
                int mapX = Mathf.Clamp(
                    (int)(((transform.position.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth),
                    0, terrainData.alphamapWidth - 1);
                int mapZ = Mathf.Clamp(
                    (int)(((transform.position.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight),
                    0, terrainData.alphamapHeight - 1);

                float[,,] alphas = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

                int dominantLayer = 0;
                float max = 0f;
                for (int i = 0; i < alphas.GetLength(2); i++)
                {
                    if (alphas[0, 0, i] > max)
                    {
                        dominantLayer = i;
                        max = alphas[0, 0, i];
                    }
                }

                // Sunet în funcție de layer
                AudioClip[] footClips = GetFootstepClipsForLayer(dominantLayer);
                if (footClips != null && footClips.Length > 0)
                {
                    audioSource.PlayOneShot(footClips[Random.Range(0, footClips.Length)]);
                }
                return;
            }
        }

        // fallback: pași pe "default" dacă nu e teren
        if (grassFootsteps != null && grassFootsteps.Length > 0)
            audioSource.PlayOneShot(grassFootsteps[Random.Range(0, grassFootsteps.Length)]);
    }

    private AudioClip[] GetFootstepClipsForLayer(int layer)
    {
        // Ordinea trebuie să corespundă cu Terrain Layers din inspector!
        switch (layer)
        {
            case 0: return grassFootsteps;
            case 1: return stoneFootsteps;
            case 2: return sandFootsteps;
            default: return grassFootsteps;
        }
    }

    private void ApplyGravity()
    {
        if (!controller.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        controller.Move(moveDirection * Time.deltaTime);
    }
}
