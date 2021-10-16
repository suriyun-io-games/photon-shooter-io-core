using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(CharacterAction))]
[RequireComponent(typeof(SyncHpRpcComponent))]
[RequireComponent(typeof(SyncArmorRpcComponent))]
[RequireComponent(typeof(SyncExpRpcComponent))]
[RequireComponent(typeof(SyncLevelRpcComponent))]
[RequireComponent(typeof(SyncStatPointRpcComponent))]
[RequireComponent(typeof(SyncWatchAdsCountRpcComponent))]
[RequireComponent(typeof(SyncSelectCharacterRpcComponent))]
[RequireComponent(typeof(SyncSelectHeadRpcComponent))]
[RequireComponent(typeof(SyncSelectWeaponsRpcComponent))]
[RequireComponent(typeof(SyncSelectCustomEquipmentsRpcComponent))]
[RequireComponent(typeof(SyncSelectWeaponIndexRpcComponent))]
[RequireComponent(typeof(SyncIsInvincibleRpcComponent))]
[RequireComponent(typeof(SyncAttributeAmountsRpcComponent))]
[RequireComponent(typeof(SyncExtraRpcComponent))]
[DisallowMultipleComponent]
public class CharacterEntity : BaseNetworkGameCharacter
{
    public const float DISCONNECT_WHEN_NOT_RESPAWN_DURATION = 60;
    public const int MAX_EQUIPPABLE_WEAPON_AMOUNT = 10;

    public enum ViewMode
    {
        TopDown,
        ThirdPerson,
    }

    [System.Serializable]
    public class ViewModeSettings
    {
        public Vector3 targetOffsets = Vector3.zero;
        public float zoomDistance = 3f;
        public float minZoomDistance = 3f;
        public float maxZoomDistance = 3f;
        public float xRotation = 45f;
        public float minXRotation = 45f;
        public float maxXRotation = 45f;
        public float yRotation = 0f;
        public float fov = 60f;
        public float nearClipPlane = 0.3f;
        public float farClipPlane = 1000f;
    }

    public ViewMode viewMode;
    public ViewModeSettings topDownViewModeSettings;
    public ViewModeSettings thirdPersionViewModeSettings;
    public bool doNotLockCursor;
    public Transform damageLaunchTransform;
    public Transform effectTransform;
    public Transform characterModelTransform;
    public GameObject[] localPlayerObjects;
    public float dashDuration = 1.5f;
    public float dashMoveSpeedMultiplier = 1.5f;
    public float blockMoveSpeedMultiplier = 0.75f;
    public float returnToMoveDirectionDelay = 1f;
    public float endActionDelay = 0.75f;
    [Header("UI")]
    public Transform hpBarContainer;
    public Image hpFillImage;
    public Text hpText;
    public Image armorFillImage;
    public Text armorText;
    public Text nameText;
    public Text levelText;
    public GameObject attackSignalObject;
    public GameObject attackSignalObjectForTeamA;
    public GameObject attackSignalObjectForTeamB;
    [Header("Effect")]
    public GameObject invincibleEffect;

    #region Sync Vars
    private SyncHpRpcComponent syncHp = null;
    public int Hp
    {
        get { return syncHp.Value; }
        set
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (value <= 0)
            {
                value = 0;
                if (!IsDeadMarked)
                {
                    photonView.TargetRPC(RpcTargetDead, photonView.Owner);
                    DeathTime = Time.unscaledTime;
                    ++syncDieCount.Value;
                    IsDeadMarked = true;
                }
            }

            if (value > TotalHp)
                value = TotalHp;

            syncHp.Value = value;
        }
    }

    private SyncArmorRpcComponent syncArmor = null;
    public int Armor
    {
        get { return syncArmor.Value; }
        set
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (value <= 0)
                value = 0;

            if (value > TotalArmor)
                value = TotalArmor;

            syncArmor.Value = value;
        }
    }

    private SyncExpRpcComponent syncExp = null;
    public virtual int Exp
    {
        get { return syncExp.Value; }
        set
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            var gameplayManager = GameplayManager.Singleton;
            while (true)
            {
                if (Level == gameplayManager.maxLevel)
                    break;

                var currentExp = gameplayManager.GetExp(Level);
                if (value < currentExp)
                    break;
                var remainExp = value - currentExp;
                value = remainExp;
                ++Level;
                StatPoint += gameplayManager.addingStatPoint;
            }

            syncExp.Value = value;
        }
    }

    private SyncLevelRpcComponent syncLevel = null;
    public int Level { get { return syncLevel.Value; } set { syncLevel.Value = value; } }

    private SyncStatPointRpcComponent syncStatPoint = null;
    public int StatPoint { get { return syncStatPoint.Value; } set { syncStatPoint.Value = value; } }

    private SyncWatchAdsCountRpcComponent syncWatchAdsCount = null;
    public byte WatchAdsCount { get { return syncWatchAdsCount.Value; } set { syncWatchAdsCount.Value = value; } }

    private SyncSelectCharacterRpcComponent syncSelectCharacter = null;
    public int SelectCharacter { get { return syncSelectCharacter.Value; } set { syncSelectCharacter.Value = value; } }

    private SyncSelectHeadRpcComponent syncSelectHead = null;
    public int SelectHead { get { return syncSelectHead.Value; } set { syncSelectHead.Value = value; } }

    private SyncSelectWeaponsRpcComponent syncSelectWeapons = null;
    public int[] SelectWeapons { get { return syncSelectWeapons.Value; } set { syncSelectWeapons.Value = value; } }

    private SyncSelectCustomEquipmentsRpcComponent syncSelectCustomEquipments = null;
    public int[] SelectCustomEquipments { get { return syncSelectCustomEquipments.Value; } set { syncSelectCustomEquipments.Value = value; } }

    private SyncSelectWeaponIndexRpcComponent syncSelectWeaponIndex = null;
    public int SelectWeaponIndex { get { return syncSelectWeaponIndex.Value; } set { syncSelectWeaponIndex.Value = value; } }

    private SyncIsInvincibleRpcComponent syncIsInvincible = null;
    public bool IsInvincible { get { return syncIsInvincible.Value; } set { syncIsInvincible.Value = value; } }

    private SyncAttributeAmountsRpcComponent syncAttributeAmounts = null;
    public AttributeAmounts AttributeAmounts { get { return syncAttributeAmounts.Value; } set { syncAttributeAmounts.Value = value; } }

    private SyncExtraRpcComponent syncExtra = null;
    public string Extra { get { return syncExtra.Value; } set { syncExtra.Value = value; } }

    public virtual bool IsBlocking
    {
        get { return CacheCharacterAction.IsBlocking; }
        set { CacheCharacterAction.IsBlocking = value; }
    }
    public virtual int AttackingActionId
    {
        get { return CacheCharacterAction.AttackingActionId; }
        set { CacheCharacterAction.AttackingActionId = value; }
    }
    public virtual Vector3 AimPosition
    {
        get { return CacheCharacterAction.AimPosition; }
        set { CacheCharacterAction.AimPosition = value; }
    }
    #endregion

    public override bool IsDead
    {
        get { return Hp <= 0; }
    }

    public override bool IsBot
    {
        get { return false; }
    }

    public System.Action onDead;
    public readonly HashSet<PickupEntity> PickableEntities = new HashSet<PickupEntity>();
    public readonly EquippedWeapon[] equippedWeapons = new EquippedWeapon[MAX_EQUIPPABLE_WEAPON_AMOUNT];

    protected ViewMode dirtyViewMode;
    protected Camera targetCamera;
    protected Vector3 cameraForward;
    protected Vector3 cameraRight;
    protected FollowCameraControls followCameraControls;
    protected Coroutine attackRoutine;
    protected Coroutine reloadRoutine;
    protected CharacterModel characterModel;
    protected CharacterData characterData;
    protected HeadData headData;
    protected Dictionary<int, CustomEquipmentData> customEquipmentDict = new Dictionary<int, CustomEquipmentData>();
    protected int defaultWeaponIndex = -1;
    protected bool isMobileInput;
    protected Vector3 inputMove;
    protected Vector3 inputDirection;
    protected bool inputAttack;
    protected bool inputJump;
    protected Vector3 dashInputMove;
    protected float dashingTime;
    protected Vector3? previousPosition;
    protected Vector3 currentVelocity;
    protected float lastActionTime;
    protected Coroutine endActionDelayCoroutine;

    public float StartReloadTime { get; protected set; }
    public float ReloadDuration { get; protected set; }
    public bool IsReady { get; protected set; }
    public bool IsDeadMarked { get; protected set; }
    public bool IsGrounded { get { return CacheCharacterMovement.IsGrounded; } }
    public bool IsPlayingAttackAnim { get; protected set; }
    public bool IsReloading { get; protected set; }
    public bool IsDashing { get; protected set; }
    public bool HasAttackInterruptReload { get; protected set; }
    public float DeathTime { get; protected set; }
    public float InvincibleTime { get; protected set; }

    public float FinishReloadTimeRate
    {
        get { return (Time.unscaledTime - StartReloadTime) / ReloadDuration; }
    }

    public EquippedWeapon CurrentEquippedWeapon
    {
        get
        {
            try
            { return equippedWeapons[SelectWeaponIndex]; }
            catch
            { return EquippedWeapon.Empty; }
        }
    }

    public WeaponData WeaponData
    {
        get
        {
            try
            { return CurrentEquippedWeapon.WeaponData; }
            catch
            { return null; }
        }
    }

    private bool isHidding;
    public bool IsHidding
    {
        get { return isHidding; }
        set
        {
            isHidding = value;
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                renderer.enabled = !isHidding;
            var canvases = GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
                canvas.enabled = !isHidding;
            var projectors = GetComponentsInChildren<Projector>();
            foreach (var projector in projectors)
                projector.enabled = !isHidding;
        }
    }

    public Transform CacheTransform { get; private set; }
    public CharacterMovement CacheCharacterMovement { get; private set; }
    public CharacterAction CacheCharacterAction { get; private set; }

    protected bool refreshingSumAddStats = true;
    protected CharacterStats sumAddStats = new CharacterStats();
    public virtual CharacterStats SumAddStats
    {
        get
        {
            if (refreshingSumAddStats)
            {
                var addStats = new CharacterStats();
                if (headData != null)
                    addStats += headData.stats;
                if (characterData != null)
                    addStats += characterData.stats;
                if (WeaponData != null)
                    addStats += WeaponData.stats;
                if (customEquipmentDict != null)
                {
                    foreach (var value in customEquipmentDict.Values)
                    {
                        addStats += value.stats;
                    }
                }
                if (AttributeAmounts.Dict != null)
                {
                    foreach (var kv in AttributeAmounts.Dict)
                    {
                        CharacterAttributes attribute;
                        if (GameplayManager.Singleton.Attributes.TryGetValue(kv.Key, out attribute))
                            addStats += attribute.stats * kv.Value;
                    }
                }
                sumAddStats = addStats;
                refreshingSumAddStats = false;
            }
            return sumAddStats;
        }
    }

    public int TotalHp
    {
        get
        {
            var total = GameplayManager.Singleton.baseMaxHp + SumAddStats.addMaxHp;
            return total;
        }
    }

    public int TotalArmor
    {
        get
        {
            var total = GameplayManager.Singleton.baseMaxArmor + SumAddStats.addMaxArmor;
            return total;
        }
    }

    public int TotalMoveSpeed
    {
        get
        {
            var total = GameplayManager.Singleton.baseMoveSpeed + SumAddStats.addMoveSpeed;
            return total;
        }
    }

    public float TotalWeaponDamageRate
    {
        get
        {
            var total = GameplayManager.Singleton.baseWeaponDamageRate + SumAddStats.addWeaponDamageRate;

            var maxValue = GameplayManager.Singleton.maxWeaponDamageRate;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public float TotalReduceDamageRate
    {
        get
        {
            var total = GameplayManager.Singleton.baseReduceDamageRate + SumAddStats.addReduceDamageRate;

            var maxValue = GameplayManager.Singleton.maxReduceDamageRate;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public float TotalBlockReduceDamageRate
    {
        get
        {
            var total = GameplayManager.Singleton.baseBlockReduceDamageRate + SumAddStats.addBlockReduceDamageRate;

            var maxValue = GameplayManager.Singleton.maxBlockReduceDamageRate;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public float TotalArmorReduceDamage
    {
        get
        {
            var total = GameplayManager.Singleton.baseArmorReduceDamage + SumAddStats.addArmorReduceDamage;

            var maxValue = GameplayManager.Singleton.maxArmorReduceDamage;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public float TotalExpRate
    {
        get
        {
            var total = 1 + SumAddStats.addExpRate;
            return total;
        }
    }

    public float TotalScoreRate
    {
        get
        {
            var total = 1 + SumAddStats.addScoreRate;
            return total;
        }
    }

    public float TotalHpRecoveryRate
    {
        get
        {
            var total = 1 + SumAddStats.addHpRecoveryRate;
            return total;
        }
    }

    public float TotalArmorRecoveryRate
    {
        get
        {
            var total = 1 + SumAddStats.addArmorRecoveryRate;
            return total;
        }
    }

    public float TotalDamageRateLeechHp
    {
        get
        {
            var total = SumAddStats.addDamageRateLeechHp;
            return total;
        }
    }

    protected override void Init()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        base.Init();
        Hp = 0;
        Armor = 0;
        Exp = 0;
        Level = 1;
        StatPoint = 0;
        WatchAdsCount = 0;
        SelectCharacter = 0;
        SelectHead = 0;
        SelectWeapons = new int[0];
        SelectCustomEquipments = new int[0];
        SelectWeaponIndex = -1;
        IsInvincible = false;
        AttributeAmounts = new AttributeAmounts(0);
        Extra = string.Empty;
    }

    protected override void Awake()
    {
        base.Awake();
        gameObject.layer = GameInstance.Singleton.characterLayer;
        CacheTransform = transform;
        CacheCharacterMovement = gameObject.GetOrAddComponent<CharacterMovement>();
        CacheCharacterAction = gameObject.GetOrAddComponent<CharacterAction>();
        if (!photonView.ObservedComponents.Contains(CacheCharacterAction))
            photonView.ObservedComponents.Add(CacheCharacterAction);
        if (damageLaunchTransform == null)
            damageLaunchTransform = CacheTransform;
        if (effectTransform == null)
            effectTransform = CacheTransform;
        if (characterModelTransform == null)
            characterModelTransform = CacheTransform;
        foreach (var localPlayerObject in localPlayerObjects)
        {
            localPlayerObject.SetActive(false);
        }
        DeathTime = Time.unscaledTime;
    }

    protected override void OnStartLocalPlayer()
    {
        if (photonView.IsMine)
        {
            followCameraControls = FindObjectOfType<FollowCameraControls>();
            followCameraControls.target = CacheTransform;
            targetCamera = followCameraControls.CacheCamera;

            foreach (var localPlayerObject in localPlayerObjects)
            {
                localPlayerObject.SetActive(true);
            }

            StartCoroutine(DelayReady());
        }
    }

    IEnumerator DelayReady()
    {
        yield return new WaitForSeconds(0.5f);
        // Add some delay before ready to make sure that it can receive team and game rule
        // TODO: Should improve this (Or remake team system, one which made by Photon is not work well)
        var uiGameplay = FindObjectOfType<UIGameplay>();
        if (uiGameplay != null)
            uiGameplay.FadeOut();
        CmdReady();
    }

    protected override void Update()
    {
        base.Update();
        if (NetworkManager != null && NetworkManager.IsMatchEnded)
            return;

        if (Hp <= 0)
        {
            if (!PhotonNetwork.IsMasterClient && photonView.IsMine && Time.unscaledTime - DeathTime >= DISCONNECT_WHEN_NOT_RESPAWN_DURATION)
                GameNetworkManager.Singleton.LeaveRoom();

            if (photonView.IsMine)
            {
                AttackingActionId = -1;
                IsBlocking = false;
            }
        }

        if (PhotonNetwork.IsMasterClient && IsInvincible && Time.unscaledTime - InvincibleTime >= GameplayManager.Singleton.invincibleDuration)
            IsInvincible = false;
        if (invincibleEffect != null)
            invincibleEffect.SetActive(IsInvincible);
        if (nameText != null)
            nameText.text = PlayerName;
        if (hpBarContainer != null)
            hpBarContainer.gameObject.SetActive(Hp > 0);
        if (hpFillImage != null)
            hpFillImage.fillAmount = (float)Hp / (float)TotalHp;
        if (hpText != null)
            hpText.text = Hp + "/" + TotalHp;
        if (levelText != null)
            levelText.text = Level.ToString("N0");
        UpdateViewMode();
        UpdateAimPosition();
        UpdateAnimation();
        UpdateInput();
        // Update dash state
        if (IsDashing && Time.unscaledTime - dashingTime > dashDuration)
            IsDashing = false;
        // Update attack signal
        if (attackSignalObject != null)
            attackSignalObject.SetActive(IsPlayingAttackAnim);
        // TODO: Improve team codes
        if (attackSignalObjectForTeamA != null)
            attackSignalObjectForTeamA.SetActive(IsPlayingAttackAnim && PlayerTeam == 1);
        if (attackSignalObjectForTeamB != null)
            attackSignalObjectForTeamB.SetActive(IsPlayingAttackAnim && PlayerTeam == 2);
    }

    private void FixedUpdate()
    {
        if (!previousPosition.HasValue)
            previousPosition = CacheTransform.position;
        var currentMove = CacheTransform.position - previousPosition.Value;
        currentVelocity = currentMove / Time.deltaTime;
        previousPosition = CacheTransform.position;

        if (NetworkManager != null && NetworkManager.IsMatchEnded)
            return;

        UpdateMovements();
    }

    protected virtual void UpdateInputDirection_TopDown(bool canAttack)
    {
        if (viewMode != ViewMode.TopDown)
            return;
        doNotLockCursor = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        followCameraControls.updateRotation = false;
        followCameraControls.updateZoom = true;
        if (isMobileInput)
        {
            inputDirection = Vector3.zero;
            inputDirection += InputManager.GetAxis("Mouse Y", false) * cameraForward;
            inputDirection += InputManager.GetAxis("Mouse X", false) * cameraRight;
            if (canAttack)
                inputAttack = inputDirection.magnitude != 0;
        }
        else
        {
            inputDirection = (InputManager.MousePosition() - targetCamera.WorldToScreenPoint(CacheTransform.position)).normalized;
            inputDirection = new Vector3(inputDirection.x, 0, inputDirection.y);
            if (canAttack)
                inputAttack = InputManager.GetButton("Fire1");
        }
    }

    protected virtual void UpdateInputDirection_ThirdPerson(bool canAttack)
    {
        if (viewMode != ViewMode.ThirdPerson)
            return;
        if (isMobileInput || doNotLockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (isMobileInput)
        {
            followCameraControls.updateRotation = InputManager.GetButton("CameraRotate");
            followCameraControls.updateZoom = true;
            inputDirection = Vector3.zero;
            inputDirection += InputManager.GetAxis("Mouse Y", false) * cameraForward;
            inputDirection += InputManager.GetAxis("Mouse X", false) * cameraRight;
            if (canAttack)
                inputAttack = InputManager.GetButton("Fire1");
        }
        else
        {
            followCameraControls.updateRotation = true;
            followCameraControls.updateZoom = true;
            inputDirection = (InputManager.MousePosition() - targetCamera.WorldToScreenPoint(CacheTransform.position)).normalized;
            inputDirection = new Vector3(inputDirection.x, 0, inputDirection.y);
            if (canAttack)
                inputAttack = InputManager.GetButton("Fire1");
        }
        if (inputAttack)
            lastActionTime = Time.unscaledTime;
    }

    protected virtual void UpdateViewMode(bool force = false)
    {
        if (!photonView.IsMine)
            return;

        if (force || dirtyViewMode != viewMode)
        {
            dirtyViewMode = viewMode;
            ViewModeSettings settings = viewMode == ViewMode.ThirdPerson ? thirdPersionViewModeSettings : topDownViewModeSettings;
            followCameraControls.limitXRotation = true;
            followCameraControls.limitYRotation = false;
            followCameraControls.limitZoomDistance = true;
            followCameraControls.targetOffset = settings.targetOffsets;
            followCameraControls.zoomDistance = settings.zoomDistance;
            followCameraControls.minZoomDistance = settings.minZoomDistance;
            followCameraControls.maxZoomDistance = settings.maxZoomDistance;
            followCameraControls.xRotation = settings.xRotation;
            followCameraControls.minXRotation = settings.minXRotation;
            followCameraControls.maxXRotation = settings.maxXRotation;
            followCameraControls.yRotation = settings.yRotation;
            targetCamera.fieldOfView = settings.fov;
            targetCamera.nearClipPlane = settings.nearClipPlane;
            targetCamera.farClipPlane = settings.farClipPlane;
        }
    }

    protected virtual void UpdateAimPosition()
    {
        if (!photonView.IsMine || !WeaponData)
            return;

        float attackDist = WeaponData.damagePrefab.GetAttackRange();
        switch (viewMode)
        {
            case ViewMode.TopDown:
                // Update aim position
                Transform launchTransform;
                GetDamageLaunchTransform(CurrentActionIsForLeftHand(), out launchTransform);
                AimPosition = launchTransform.position + (CacheTransform.forward * attackDist);
                break;
            case ViewMode.ThirdPerson:
                float distanceToCharacter = Vector3.Distance(CacheTransform.position, followCameraControls.CacheCameraTransform.position);
                float distanceToTarget = attackDist;
                Vector3 lookAtCharacterPosition = targetCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceToCharacter));
                Vector3 lookAtTargetPosition = targetCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceToTarget));
                AimPosition = lookAtTargetPosition;
                RaycastHit[] hits = Physics.RaycastAll(lookAtCharacterPosition, (lookAtTargetPosition - lookAtCharacterPosition).normalized, attackDist);
                for (int i = 0; i < hits.Length; ++i)
                {
                    if (hits[i].transform.root != transform.root)
                        AimPosition = hits[i].point;
                }
                break;
        }
    }

    protected virtual void UpdateAnimation()
    {
        if (characterModel == null)
            return;
        var animator = characterModel.CacheAnimator;
        if (animator == null)
            return;
        if (Hp <= 0)
        {
            animator.SetBool("IsDead", true);
            animator.SetFloat("JumpSpeed", 0);
            animator.SetFloat("MoveSpeed", 0);
            animator.SetBool("IsGround", true);
            animator.SetBool("IsDash", false);
            animator.SetBool("IsBlock", false);
        }
        else
        {
            var velocity = currentVelocity;
            var xzMagnitude = new Vector3(velocity.x, 0, velocity.z).magnitude;
            var ySpeed = velocity.y;
            animator.SetBool("IsDead", false);
            animator.SetFloat("JumpSpeed", ySpeed);
            animator.SetFloat("MoveSpeed", xzMagnitude);
            animator.SetBool("IsGround", Mathf.Abs(ySpeed) < 0.5f);
            animator.SetBool("IsDash", IsDashing);
            animator.SetBool("IsBlock", IsBlocking);
        }

        if (WeaponData != null)
            animator.SetInteger("WeaponAnimId", WeaponData.weaponAnimId);

        animator.SetBool("IsIdle", !animator.GetBool("IsDead") && !animator.GetBool("DoAction") && animator.GetBool("IsGround"));

        if (AttackingActionId >= 0 && !IsPlayingAttackAnim)
            StartCoroutine(AttackRoutine(AttackingActionId));
    }

    protected virtual void UpdateInput()
    {
        if (!photonView.IsMine)
            return;

        bool canControl = true;
        var fields = FindObjectsOfType<InputField>();
        foreach (var field in fields)
        {
            if (field.isFocused)
            {
                canControl = false;
                break;
            }
        }

        isMobileInput = Application.isMobilePlatform;
#if UNITY_EDITOR
        isMobileInput = GameInstance.Singleton.showJoystickInEditor;
#endif
        InputManager.useMobileInputOnNonMobile = isMobileInput;

        var canAttack = isMobileInput || !EventSystem.current.IsPointerOverGameObject();
        inputMove = Vector3.zero;
        inputDirection = Vector3.zero;
        inputAttack = false;
        if (canControl)
        {
            cameraForward = followCameraControls.CacheCameraTransform.forward;
            cameraForward.y = 0;
            cameraForward = cameraForward.normalized;
            cameraRight = followCameraControls.CacheCameraTransform.right;
            cameraRight.y = 0;
            cameraRight = cameraRight.normalized;
            inputMove = Vector3.zero;
            if (!IsDead)
            {
                inputMove += cameraForward * InputManager.GetAxis("Vertical", false);
                inputMove += cameraRight * InputManager.GetAxis("Horizontal", false);
            }

            // Bloacking
            IsBlocking = !IsDead && !IsReloading && !IsDashing && AttackingActionId < 0 && IsGrounded && InputManager.GetButton("Block");

            // Jump
            if (!IsDead && !IsBlocking && !inputJump)
                inputJump = InputManager.GetButtonDown("Jump") && IsGrounded && !IsDashing;
                
            if (!IsBlocking && !IsDashing)
            {
                UpdateInputDirection_TopDown(canAttack);
                UpdateInputDirection_ThirdPerson(canAttack);
                if (!IsDead)
                {
                    if (InputManager.GetButtonDown("Reload"))
                        Reload();
                    if (GameplayManager.Singleton.autoReload &&
                        CurrentEquippedWeapon.currentAmmo == 0 &&
                        CurrentEquippedWeapon.CanReload())
                        Reload();
                    IsDashing = InputManager.GetButtonDown("Dash") && IsGrounded;
                }
                if (IsDashing)
                {
                    if (isMobileInput)
                        dashInputMove = inputMove.normalized;
                    else
                        dashInputMove = new Vector3(CacheTransform.forward.x, 0f, CacheTransform.forward.z).normalized;
                    inputAttack = false;
                    dashingTime = Time.unscaledTime;
                    CmdDash();
                }
            }
        }
    }

    protected virtual float GetMoveSpeed()
    {
        return TotalMoveSpeed * GameplayManager.REAL_MOVE_SPEED_RATE;
    }

    protected virtual bool CurrentActionIsForLeftHand()
    {
        if (AttackingActionId >= 0)
        {
            AttackAnimation attackAnimation;
            if (WeaponData.AttackAnimations.TryGetValue(AttackingActionId, out attackAnimation))
                return attackAnimation.isAnimationForLeftHandWeapon;
        }
        return false;
    }

    protected virtual void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude > 1)
            direction = direction.normalized;
        direction.y = 0;

        var targetSpeed = GetMoveSpeed() * (IsBlocking ? blockMoveSpeedMultiplier : (IsDashing ? dashMoveSpeedMultiplier : 1f));
        CacheCharacterMovement.UpdateMovement(Time.deltaTime, targetSpeed, direction, inputJump);
    }

    protected virtual void UpdateMovements()
    {
        if (!photonView.IsMine)
            return;

        var moveDirection = inputMove;
        var dashDirection = dashInputMove;

        Move(IsDashing ? dashDirection : moveDirection);
        // Turn character to move direction
        if (inputDirection.magnitude <= 0 && inputMove.magnitude > 0 || viewMode == ViewMode.ThirdPerson)
            inputDirection = inputMove;
        if (characterModel && characterModel.CacheAnimator && (characterModel.CacheAnimator.GetBool("DoAction") || Time.unscaledTime - lastActionTime <= returnToMoveDirectionDelay) && viewMode == ViewMode.ThirdPerson)
            inputDirection = cameraForward;
        if (!IsDead)
            Rotate(IsDashing ? dashInputMove : inputDirection);

        if (!IsDead && !IsBlocking)
        {
            if (inputAttack && GameplayManager.Singleton.CanAttack(this))
                Attack();
            else
                StopAttack();
        }

        inputJump = false;
    }

    protected void Rotate(Vector3 direction)
    {
        if (direction.sqrMagnitude != 0)
            CacheTransform.rotation = Quaternion.LookRotation(direction);
    }

    public void GetDamageLaunchTransform(bool isLeftHandWeapon, out Transform launchTransform)
    {
        if (characterModel == null || !characterModel.TryGetDamageLaunchTransform(isLeftHandWeapon, out launchTransform))
            launchTransform = damageLaunchTransform;
    }

    protected void Attack()
    {
        if (photonView.IsMine)
        {
            // If attacking while reloading, determines that it is reload interrupting
            if (IsReloading && FinishReloadTimeRate > 0.8f)
                HasAttackInterruptReload = true;
        }

        if (IsPlayingAttackAnim || IsReloading || IsBlocking || !CurrentEquippedWeapon.CanShoot())
            return;

        if (AttackingActionId < 0 && photonView.IsMine)
        {
            if (WeaponData != null)
                AttackingActionId = WeaponData.GetRandomAttackAnimation().actionId;
            else
                AttackingActionId = -1;
        }
    }

    protected void StopAttack()
    {
        if (AttackingActionId >= 0 && photonView.IsMine)
            AttackingActionId = -1;
    }

    protected void Reload()
    {
        if (IsPlayingAttackAnim || IsReloading || !CurrentEquippedWeapon.CanReload())
            return;
        if (photonView.IsMine)
            CmdReload();
    }

    IEnumerator AttackRoutine(int actionId)
    {
        if (!IsPlayingAttackAnim &&
            !IsReloading &&
            CurrentEquippedWeapon.CanShoot() &&
            Hp > 0 &&
            characterModel != null &&
            characterModel.CacheAnimator != null)
        {
            IsPlayingAttackAnim = true;
            AttackAnimation attackAnimation;
            if (WeaponData != null &&
                WeaponData.AttackAnimations.TryGetValue(actionId, out attackAnimation))
            {
                if (endActionDelayCoroutine != null)
                    StopCoroutine(endActionDelayCoroutine);
                // Play attack animation
                characterModel.CacheAnimator.SetBool("DoAction", true);
                characterModel.CacheAnimator.SetInteger("ActionID", attackAnimation.actionId);
                characterModel.CacheAnimator.Play(0, 1, 0);

                // Wait to launch damage entity
                var speed = attackAnimation.speed;
                var animationDuration = attackAnimation.animationDuration;
                var launchDuration = attackAnimation.launchDuration;
                if (launchDuration > animationDuration)
                    launchDuration = animationDuration;
                yield return new WaitForSeconds(launchDuration / speed);

                WeaponData.Launch(this, attackAnimation.isAnimationForLeftHandWeapon, AimPosition);
                // Manage ammo at master client
                if (PhotonNetwork.IsMasterClient)
                {
                    var equippedWeapon = CurrentEquippedWeapon;
                    equippedWeapon.DecreaseAmmo();
                    equippedWeapons[SelectWeaponIndex] = equippedWeapon;
                    photonView.AllRPC(RpcUpdateEquippedWeaponsAmmo, SelectWeaponIndex, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
                }

                // Random play shoot sounds
                if (WeaponData.attackFx != null && WeaponData.attackFx.Length > 0 && AudioManager.Singleton != null)
                    AudioSource.PlayClipAtPoint(WeaponData.attackFx[Random.Range(0, WeaponData.attackFx.Length - 1)], CacheTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);

                // Wait till animation end
                yield return new WaitForSeconds((animationDuration - launchDuration) / speed);
            }
            // If player still attacking, random new attacking action id
            if (PhotonNetwork.IsMasterClient && AttackingActionId >= 0 && WeaponData != null)
                AttackingActionId = WeaponData.GetRandomAttackAnimation().actionId;

            // Attack animation ended
            endActionDelayCoroutine = StartCoroutine(DelayEndAction(endActionDelay));
            IsPlayingAttackAnim = false;
        }
    }

    IEnumerator DelayEndAction(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        characterModel.CacheAnimator.SetBool("DoAction", false);
    }

    IEnumerator ReloadRoutine()
    {
        HasAttackInterruptReload = false;
        if (!IsReloading && CurrentEquippedWeapon.CanReload())
        {
            IsReloading = true;
            if (WeaponData != null)
            {
                ReloadDuration = WeaponData.reloadDuration;
                StartReloadTime = Time.unscaledTime;
                if (WeaponData.clipOutFx != null && AudioManager.Singleton != null)
                    AudioSource.PlayClipAtPoint(WeaponData.clipOutFx, CacheTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);
                yield return new WaitForSeconds(ReloadDuration);
                if (PhotonNetwork.IsMasterClient)
                {
                    var equippedWeapon = CurrentEquippedWeapon;
                    equippedWeapon.Reload();
                    equippedWeapons[SelectWeaponIndex] = equippedWeapon;
                    photonView.AllRPC(RpcUpdateEquippedWeaponsAmmo, SelectWeaponIndex, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
                }
                if (WeaponData.clipInFx != null && AudioManager.Singleton != null)
                    AudioSource.PlayClipAtPoint(WeaponData.clipInFx, CacheTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);
            }
            // If player still attacking, random new attacking action id
            if (PhotonNetwork.IsMasterClient && AttackingActionId >= 0 && WeaponData != null)
                AttackingActionId = WeaponData.GetRandomAttackAnimation().actionId;
            yield return new WaitForEndOfFrame();
            IsReloading = false;
            if (photonView.IsMine)
            {
                // If weapon is reload one ammo at a time (like as shotgun), automatically reload more bullets
                // When there is no attack interrupt while reload
                if (WeaponData != null && WeaponData.reloadOneAmmoAtATime && CurrentEquippedWeapon.CanReload())
                {
                    if (!HasAttackInterruptReload)
                        Reload();
                    else
                        Attack();
                }
            }
        }
    }

    public virtual bool ReceiveDamage(CharacterEntity attacker, int damage)
    {
        if (Hp <= 0 || IsInvincible)
            return false;

        if (!GameplayManager.Singleton.CanReceiveDamage(this, attacker))
            return false;

        int reduceHp = damage;
        reduceHp -= Mathf.CeilToInt(damage * TotalReduceDamageRate);

        // Armor damage absorbing
        if (Armor > 0)
        {
            if (Armor - damage >= 0)
            {
                // Reduce damage, decrease armor
                reduceHp -= Mathf.CeilToInt(damage * TotalArmorReduceDamage);
                Armor -= damage;
            }
            else
            {
                // Armor remaining less than 0, Reduce HP by remain damage without armor absorb
                reduceHp -= Mathf.CeilToInt(Armor * TotalArmorReduceDamage);
                // Remain damage after armor broke
                reduceHp -= Mathf.Abs(Armor - damage);
                Armor = 0;
            }
        }

        // Blocking
        if (IsBlocking)
            reduceHp -= Mathf.CeilToInt(damage * TotalBlockReduceDamageRate);

        // Avoid increasing hp by damage
        if (reduceHp < 0)
            reduceHp = 0;

        Hp -= reduceHp;
        if (attacker != null)
        {
            var leechHpAmount = Mathf.CeilToInt(attacker.TotalDamageRateLeechHp * reduceHp);
            attacker.Hp += leechHpAmount;
            if (Hp == 0)
            {
                if (onDead != null)
                    onDead.Invoke();
                InterruptAttack();
                InterruptReload();
                photonView.OthersRPC(RpcInterruptAttack);
                photonView.OthersRPC(RpcInterruptReload);
                attacker.KilledTarget(this);
                ++syncDieCount.Value;
            }
        }
        return true;
    }

    public void KilledTarget(CharacterEntity target)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        var gameplayManager = GameplayManager.Singleton;
        var targetLevel = target.Level;
        var maxLevel = gameplayManager.maxLevel;
        Exp += Mathf.CeilToInt(gameplayManager.GetRewardExp(targetLevel) * TotalExpRate);
        syncScore.Value += Mathf.CeilToInt(gameplayManager.GetKillScore(targetLevel) * TotalScoreRate);
        foreach (var rewardCurrency in gameplayManager.rewardCurrencies)
        {
            var currencyId = rewardCurrency.currencyId;
            var amount = rewardCurrency.amount.Calculate(targetLevel, maxLevel);
            photonView.TargetRPC(RpcTargetRewardCurrency, photonView.Owner, currencyId, amount);
        }
        ++syncKillCount.Value;
        GameNetworkManager.Singleton.SendKillNotify(PlayerName, target.PlayerName, WeaponData == null ? string.Empty : WeaponData.GetId());
    }

    public void Heal(int amount)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (Hp <= 0)
            return;

        Hp += amount;
    }

    public virtual float GetAttackRange()
    {
        if (WeaponData == null || WeaponData.damagePrefab == null)
            return 0;
        return WeaponData.damagePrefab.GetAttackRange();
    }

    public virtual Vector3 GetSpawnPosition()
    {
        return GameplayManager.Singleton.GetCharacterSpawnPosition(this);
    }

    public void UpdateCharacterModelHiddingState()
    {
        if (characterModel == null)
            return;
        var renderers = characterModel.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
            renderer.enabled = !IsHidding;
    }

    protected void InterruptAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            IsPlayingAttackAnim = false;
        }
    }

    protected void InterruptReload()
    {
        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            IsReloading = false;
        }
    }

    public virtual void OnSpawn() { }

    public void OnUpdateSelectCharacter(int selectCharacter)
    {
        refreshingSumAddStats = true;
        if (characterModel != null)
            Destroy(characterModel.gameObject);
        characterData = GameInstance.GetCharacter(selectCharacter);
        if (characterData == null || characterData.modelObject == null)
            return;
        characterModel = Instantiate(characterData.modelObject, characterModelTransform);
        characterModel.transform.localPosition = Vector3.zero;
        characterModel.transform.localEulerAngles = Vector3.zero;
        characterModel.transform.localScale = Vector3.one;
        if (headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        if (WeaponData != null)
            characterModel.SetWeaponModel(WeaponData.rightHandObject, WeaponData.leftHandObject, WeaponData.shieldObject);
        if (customEquipmentDict != null)
        {
            characterModel.ClearCustomModels();
            foreach (var value in customEquipmentDict.Values)
            {
                characterModel.SetCustomModel(value.containerIndex, value.modelObject);
            }
        }
        characterModel.gameObject.SetActive(true);
        UpdateCharacterModelHiddingState();
    }

    public void OnUpdateSelectHead(int selectHead)
    {
        refreshingSumAddStats = true;
        headData = GameInstance.GetHead(selectHead);
        if (characterModel != null && headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        UpdateCharacterModelHiddingState();
    }

    public void OnUpdateSelectWeapons(int[] selectWeapons)
    {
        refreshingSumAddStats = true;
        // Changes weapon list, equip first weapon equipped position
        var minEquipPos = int.MaxValue;
        for (var i = 0; i < selectWeapons.Length; ++i)
        {
            var weaponData = GameInstance.GetWeapon(selectWeapons[i]);

            if (weaponData == null)
                continue;

            var equipPos = weaponData.equipPosition;
            if (minEquipPos > equipPos)
            {
                defaultWeaponIndex = equipPos;
                minEquipPos = equipPos;
            }

            var equippedWeapon = new EquippedWeapon();
            equippedWeapon.defaultId = weaponData.GetHashId();
            equippedWeapon.weaponId = weaponData.GetHashId();
            equippedWeapon.SetMaxAmmo();
            equippedWeapons[equipPos] = equippedWeapon;
            if (PhotonNetwork.IsMasterClient)
                photonView.AllRPC(RpcUpdateEquippedWeapons, equipPos, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
        }
        SelectWeaponIndex = defaultWeaponIndex;
    }

    public void OnUpdateSelectCustomEquipments(int[] selectCustomEquipments)
    {
        refreshingSumAddStats = true;
        if (characterModel != null)
            characterModel.ClearCustomModels();
        customEquipmentDict.Clear();
        if (selectCustomEquipments != null)
        {
            for (var i = 0; i < selectCustomEquipments.Length; ++i)
            {
                var customEquipmentData = GameInstance.GetCustomEquipment(selectCustomEquipments[i]);
                if (customEquipmentData != null &&
                    !customEquipmentDict.ContainsKey(customEquipmentData.containerIndex))
                {
                    customEquipmentDict[customEquipmentData.containerIndex] = customEquipmentData;
                    if (characterModel != null)
                        characterModel.SetCustomModel(customEquipmentData.containerIndex, customEquipmentData.modelObject);
                }
            }
        }
        UpdateCharacterModelHiddingState();
    }

    public void OnUpdateSelectWeaponIndex(int selectWeaponIndex)
    {
        refreshingSumAddStats = true;
        if (selectWeaponIndex < 0 || selectWeaponIndex >= equippedWeapons.Length)
            return;
        if (characterModel != null && WeaponData != null)
            characterModel.SetWeaponModel(WeaponData.rightHandObject, WeaponData.leftHandObject, WeaponData.shieldObject);
        UpdateCharacterModelHiddingState();
    }

    public void OnUpdateAttributeAmounts()
    {
        refreshingSumAddStats = true;
    }

    public void ServerInvincible()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        InvincibleTime = Time.unscaledTime;
        IsInvincible = true;
    }

    public void ServerSpawn(bool isWatchedAds)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (Respawn(isWatchedAds))
        {
            ServerInvincible();
            OnSpawn();
            var position = GetSpawnPosition();
            CacheTransform.position = position;
            photonView.TargetRPC(RpcTargetSpawn, photonView.Owner, position.x, position.y, position.z);
            ServerRevive();
        }
    }

    public void ServerRespawn(bool isWatchedAds)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (CanRespawn(isWatchedAds))
            ServerSpawn(isWatchedAds);
    }

    public void ServerRevive()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        for (var i = 0; i < equippedWeapons.Length; ++i)
        {
            var equippedWeapon = equippedWeapons[i];
            equippedWeapon.ChangeWeaponId(equippedWeapon.defaultId, 0);
            equippedWeapon.SetMaxAmmo();
            equippedWeapons[i] = equippedWeapon;
            photonView.AllRPC(RpcUpdateEquippedWeapons, i, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
        }
        SelectWeaponIndex = defaultWeaponIndex;

        IsPlayingAttackAnim = false;
        IsReloading = false;
        IsDeadMarked = false;
        Hp = TotalHp;
    }

    public void ServerReload()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (WeaponData != null)
        {
            // Start reload routine at server to reload ammo
            reloadRoutine = StartCoroutine(ReloadRoutine());
            // Call RpcReload() at clients to play reloading animation
            photonView.OthersRPC(RpcReload);
        }
    }

    public void ServerChangeWeapon(int index)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        var gameInstance = GameInstance.Singleton;
        if (index >= 0 && index < MAX_EQUIPPABLE_WEAPON_AMOUNT && !equippedWeapons[index].IsEmpty())
        {
            SelectWeaponIndex = index;
            InterruptAttack();
            InterruptReload();
            photonView.OthersRPC(RpcInterruptAttack);
            photonView.OthersRPC(RpcInterruptReload);
        }
    }

    public bool ServerChangeSelectWeapon(WeaponData weaponData, int ammoAmount)
    {
        if (!PhotonNetwork.IsMasterClient)
            return false;
        if (weaponData == null || weaponData.equipPosition < 0 || weaponData.equipPosition >= equippedWeapons.Length)
            return false;
        var equipPosition = weaponData.equipPosition;
        var equippedWeapon = equippedWeapons[equipPosition];
        var updated = equippedWeapon.ChangeWeaponId(weaponData.GetHashId(), ammoAmount);
        if (updated)
        {
            InterruptAttack();
            InterruptReload();
            photonView.OthersRPC(RpcInterruptAttack);
            photonView.OthersRPC(RpcInterruptReload);
            equippedWeapons[equipPosition] = equippedWeapon;
            if (SelectWeaponIndex == equipPosition)
                OnUpdateSelectWeaponIndex(SelectWeaponIndex);
            photonView.AllRPC(RpcUpdateEquippedWeapons, equipPosition, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
        }
        return updated;
    }

    public bool ServerFillWeaponAmmo(WeaponData weaponData, int ammoAmount)
    {
        if (!PhotonNetwork.IsMasterClient)
            return false;
        if (weaponData == null || weaponData.equipPosition < 0 || weaponData.equipPosition >= equippedWeapons.Length)
            return false;
        var equipPosition = weaponData.equipPosition;
        var equippedWeapon = equippedWeapons[equipPosition];
        var updated = false;
        if (equippedWeapon.weaponId == weaponData.GetHashId())
        {
            updated = equippedWeapon.AddReserveAmmo(ammoAmount);
            if (updated)
            {
                equippedWeapons[equipPosition] = equippedWeapon;
                photonView.AllRPC(RpcUpdateEquippedWeaponsAmmo, equipPosition, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
            }
        }
        return updated;
    }

    public void CmdInit(int selectHead, int selectCharacter, int[] selectWeapons, int[] selectCustomEquipments, string extra)
    {
        photonView.MasterRPC(RpcServerInit, selectHead, selectCharacter, selectWeapons, selectCustomEquipments, extra);
    }

    [PunRPC]
    protected void RpcServerInit(int selectHead, int selectCharacter, int[] selectWeapons, int[] selectCustomEquipments, string extra)
    {
        var alreadyInit = false;
        var networkManager = BaseNetworkGameManager.Singleton;
        if (networkManager != null)
        {
            networkManager.RegisterCharacter(this);
            var gameRule = networkManager.gameRule;
            if (gameRule != null && gameRule is IONetworkGameRule)
            {
                var ioGameRule = gameRule as IONetworkGameRule;
                ioGameRule.NewPlayer(this, selectHead, selectCharacter, selectWeapons, selectCustomEquipments, extra);
                alreadyInit = true;
            }
        }
        if (!alreadyInit)
        {
            SelectHead = selectHead;
            SelectCharacter = selectCharacter;
            SelectWeapons = selectWeapons;
            SelectCustomEquipments = selectCustomEquipments;
            Extra = extra;
        }
        Hp = TotalHp;
    }

    public void CmdReady()
    {
        photonView.MasterRPC(RpcServerReady);
    }

    [PunRPC]
    protected void RpcServerReady()
    {
        if (!IsReady)
        {
            ServerSpawn(false);
            IsReady = true;
        }
    }

    public void CmdRespawn(bool isWatchedAds)
    {
        photonView.MasterRPC(RpcServerRespawn, isWatchedAds);
    }

    [PunRPC]
    protected void RpcServerRespawn(bool isWatchedAds)
    {
        ServerRespawn(isWatchedAds);
    }

    public void CmdReload()
    {
        photonView.MasterRPC(RpcServerReload);
    }

    [PunRPC]
    protected void RpcServerReload()
    {
        ServerReload();
    }

    public void CmdAddAttribute(int id)
    {
        photonView.MasterRPC(RpcServerAddAttribute, id);
    }

    [PunRPC]
    protected void RpcServerAddAttribute(int id)
    {
        if (StatPoint > 0)
        {
            if (GameplayManager.Singleton.Attributes.ContainsKey(id))
            {
                AttributeAmounts = AttributeAmounts.Increase(id, 1);
                --StatPoint;
            }
        }
    }

    public void CmdChangeWeapon(int index)
    {
        photonView.MasterRPC(RpcServerChangeWeapon, index);
    }

    [PunRPC]
    protected void RpcServerChangeWeapon(int index)
    {
        ServerChangeWeapon(index);
    }

    public void CmdDash()
    {
        // Play dash animation on other clients
        photonView.OthersRPC(RpcDash);
    }

    [PunRPC]
    protected void RpcDash()
    {
        // Just play dash animation on another clients
        IsDashing = true;
        dashingTime = Time.unscaledTime;
    }

    public void CmdPickup(int viewId)
    {
        photonView.MasterRPC(RpcServerPickup, viewId);
    }

    [PunRPC]
    protected void RpcServerPickup(int viewId)
    {
        var go = PhotonView.Find(viewId);
        if (go == null)
            return;
        var pickup = go.GetComponent<PickupEntity>();
        if (pickup == null)
            return;
        pickup.Pickup(this);
    }

    [PunRPC]
    public void RpcReload()
    {
        if (!PhotonNetwork.IsMasterClient)
            reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    [PunRPC]
    public void RpcInterruptAttack()
    {
        if (!PhotonNetwork.IsMasterClient)
            InterruptAttack();
    }

    [PunRPC]
    public void RpcInterruptReload()
    {
        if (!PhotonNetwork.IsMasterClient)
            InterruptReload();
    }

    [PunRPC]
    protected void RpcTargetDead()
    {
        DeathTime = Time.unscaledTime;
    }

    [PunRPC]
    public void RpcTargetSpawn(float x, float y, float z)
    {
        transform.position = new Vector3(x, y, z);
    }

    [PunRPC]
    protected void RpcTargetRewardCurrency(string currencyId, int amount)
    {
        MonetizationManager.Save.AddCurrency(currencyId, amount);
    }

    [PunRPC]
    protected virtual void RpcUpdateEquippedWeapons(int index, int defaultId, int weaponId, int currentAmmo, int currentReserveAmmo)
    {
        if (index < 0 || index >= equippedWeapons.Length)
            return;
        var weapon = new EquippedWeapon();
        weapon.defaultId = defaultId;
        weapon.weaponId = weaponId;
        weapon.currentAmmo = currentAmmo;
        weapon.currentReserveAmmo = currentReserveAmmo;
        equippedWeapons[index] = weapon;
        if (index == SelectWeaponIndex)
            OnUpdateSelectWeaponIndex(SelectWeaponIndex);
    }

    [PunRPC]
    protected virtual void RpcUpdateEquippedWeaponsAmmo(int index, int currentAmmo, int currentReserveAmmo)
    {
        if (index < 0 || index >= equippedWeapons.Length)
            return;
        var weapon = equippedWeapons[index];
        weapon.currentAmmo = currentAmmo;
        weapon.currentReserveAmmo = currentReserveAmmo;
        equippedWeapons[index] = weapon;
    }
}
