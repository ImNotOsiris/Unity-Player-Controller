using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player_Controller : MonoBehaviour
{
	public bool CanMove { get; private set; } = true;
	private bool IsSprinting => canSprint && Input.GetKey(sprintKey);

	private bool ShouldCrouch =>
		Input.GetKeyDown(crouchKey) && !_duringCrouchAnimation && _characterController.isGrounded;

	# region Variables
	[Header("Functional Options")]
	[SerializeField] private bool canSprint = true;
	[SerializeField] private bool canCrouch = true;
	[SerializeField] private bool canUseHeadbob = true;
	[SerializeField] private bool useFootsteps = true;

	[Header("Controls")] 
	[SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
	[SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
	
	[Header("Movement Parameters")] 
	[SerializeField] private float walkSpeed = 3.0f;
	[SerializeField] private float sprintSpeed = 6.0f;
	[SerializeField] private float gravity = 30.0f;

	[Header("Crouch Parameters")] 
	[SerializeField] private float crouchHeight = 0.5f;
	[SerializeField] private float standingHeight = 2f;
	[SerializeField] private float timeToCrouch = 0.25f;
	[SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
	[SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
	private bool _isCrouching;
	private bool _duringCrouchAnimation;

	[Header("Headbob Parameters")] 
	[SerializeField] private float walkBobSpeed = 14f;
	[SerializeField] private float walkBobAmount = 0.05f;
	[SerializeField] private float sprintBobSpeed = 18f;
	[SerializeField] private float sprintBobAmount = 0.11f;
	[SerializeField] private float crouchBobSpeed = 8f;
	[SerializeField] private float crouchBobAmount = 0.025f;
	private float _defaultYPos = 0;
	private float _timer;

	[Header("Look Parameters")] 
	[SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
	[SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
	[SerializeField, Range(1, 180)] private float upperLockLimit = 80.0f;
	[SerializeField, Range(1, 180)] private float lowerLockLimit = 80.0f;

	[Header("Footstep Parameters")] 
	[SerializeField] private float baseStepSpeed = 0.5f;
	[SerializeField] private float crouchStepMult = 1.5f;
	[SerializeField] private float sprintStepMult = 0.6f;
	
	// TODO: Add sound clips for other materials.
	[SerializeField] private AudioSource footstepAudioSource = default;
	[SerializeField] private AudioClip[] grassClips = default;
	[SerializeField] private AudioClip[] woodClips = default;
	private float footstepTimer = 0;
	private float GetCurrentOffset => _isCrouching ? baseStepSpeed * crouchStepMult : IsSprinting ? baseStepSpeed * sprintStepMult : baseStepSpeed;

	private Camera _playerCamera;
	private CharacterController _characterController;

	private Vector3 _moveDirection;
	private Vector2 _currentInput;

	private float _rotationX = 0f;
	# endregion
	
	# region Built-in Methods
	private void Awake()
	{
		_playerCamera = GetComponentInChildren<Camera>();
		_characterController = GetComponent<CharacterController>();
		_defaultYPos = _playerCamera.transform.localPosition.y;
		
	}

	private void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	private void Update()
	{
		if (CanMove)
		{
			HandleMovementInput();
			HandleMouseLook();

			if (canCrouch)
				HandleCrouch();

			if (canUseHeadbob)
				HandleHeadbob();

			if (useFootsteps)
				HandleFootsteps();
			
			ApplyFinalMovements();
		}
	}

    private void OnTriggerEnter(Collider trigger)
    {
        if(trigger.gameObject.CompareTag("Triggers/OOB"))
        {
			SceneManager.LoadScene("map_death_oob");
        }
    }

    #endregion

    #region Custom Methods
    private void HandleMovementInput()
	{
		_currentInput = new Vector2((IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxisRaw("Vertical"), (IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxisRaw("Horizontal"));

		float moveDirectionY = _moveDirection.y;
		_moveDirection = (transform.TransformDirection(Vector3.forward) * _currentInput.x) + (transform.TransformDirection(Vector3.right) * _currentInput.y);
		_moveDirection.y = moveDirectionY;
	}

	private void HandleMouseLook()
	{

		transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
	}

	private void HandleCrouch()
	{
		if (ShouldCrouch)
			StartCoroutine(CrouchStand());
	}

	private void HandleHeadbob()
	{
		if (!_characterController.isGrounded) return;

		if (Mathf.Abs(_moveDirection.x) > 0.1f || Mathf.Abs(_moveDirection.z) > 0.1f)
		{
			_timer += Time.deltaTime * (_isCrouching ? crouchBobSpeed : IsSprinting ? sprintBobSpeed : walkBobSpeed);
			_playerCamera.transform.localPosition = new Vector3(
				_playerCamera.transform.localPosition.x, 
				_defaultYPos + Mathf.Sin(_timer) * (_isCrouching ? crouchBobAmount : IsSprinting ? sprintBobAmount : walkBobAmount),
				_playerCamera.transform.localPosition.z);
		}
	}

	private void HandleFootsteps()
	{
		if (!_characterController.isGrounded) return;
		if (_currentInput == Vector2.zero) return;

		footstepTimer -= Time.deltaTime;

		if (footstepTimer <= 0)
		{
			if(Physics.Raycast(_characterController.transform.position, Vector3.down, out RaycastHit hit, 2))
			{
				switch (hit.collider.tag)
				{
					case "Footsteps/WOOD":
						footstepAudioSource.PlayOneShot(woodClips[Random.Range(0, woodClips.Length - 1)]);
						break;
					case "Footsteps/GRASS":
						footstepAudioSource.PlayOneShot(grassClips[Random.Range(0, grassClips.Length - 1)]);
						break;
					default:
						footstepAudioSource.PlayOneShot(grassClips[Random.Range(0, grassClips.Length - 1)]);
						break;
				}
			}

			footstepTimer = GetCurrentOffset;
		}
	}
	
	private void ApplyFinalMovements()
	{
		if (!_characterController.isGrounded)
			_moveDirection.y -= gravity * Time.deltaTime;

		_characterController.Move(_moveDirection * Time.deltaTime);
	}


	private IEnumerator CrouchStand()
	{ 
		_duringCrouchAnimation = true;

		float timeElapsed = 0;
		float targetHeight = _isCrouching ? standingHeight : crouchHeight;
		float currentHeight = _characterController.height;
		Vector3 targetCenter = _isCrouching ? standingCenter : crouchingCenter;
		Vector3 currentCenter = _characterController.center;

		while (timeElapsed < timeToCrouch)
		{
			_characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
			_characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
			timeElapsed += Time.deltaTime;
			yield return null;
		}

		_characterController.height = targetHeight;
		_characterController.center = targetCenter;

		_isCrouching = !_isCrouching;

		_duringCrouchAnimation = false;
	}
	#endregion
}
