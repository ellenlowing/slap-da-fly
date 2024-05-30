using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.XR;

public class FroggyController : MonoBehaviour
{
    public class HandData
    {
        public OVRHand Hand;
        public OVRSkeleton Skeleton;
        public Transform ThumbTipTransform;
        public Transform IndexFingerTipTransform;
        public bool IsIndexFingerPinching = false;

        public HandData(OVRHand hand, OVRSkeleton skeleton)
        {
            Hand = hand;
            Skeleton = skeleton;
            foreach (var b in Skeleton.Bones)
            {
                if (b.Id == OVRSkeleton.BoneId.Hand_ThumbTip)
                {
                    ThumbTipTransform = b.Transform;
                }
                else if (b.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                {
                    IndexFingerTipTransform = b.Transform;
                }
            }
        }
    }

    public static FroggyController Instance;

    [Header("Tongue")]
    public Transform FroggyParentTransform;
    public Transform FrogTongueTransform;
    public float GrabSpeed = 2;
    public float ReturnSpeed = 1;
    public float MinScaleY = 0.1f;
    public float MaxScaleY = 5f;
    public bool FroggyActive = false;
    public Transform TongueTipObjectTransform;
    public Transform TongueTipTargetTransform;
    public float MaxDistanceToActivateFroggy = 0.1f;
    public float MaxDistanceToGrab = 0.02f;

    [Header("Hands")]
    public OVRHand FroggyActiveHand = null;
    public OVRHand LeftHand;
    public OVRHand RightHand;
    public OVRSkeleton LeftHandSkeleton;
    public OVRSkeleton RightHandSkeleton;
    public Vector3 FroggyPositionOffset;
    public Vector3 FroggyRotationOffset;

    [Header("SphereCast")]
    public float SphereCastRadius = 0.2f;
    public float SphereCastDistance = 4f;
    public LayerMask FlyLayerMask;

    [Header("Cooldown")]
    public float CooldownTime = 3f;
    public float FroggyLastTriggeredTime = 0;

    private HandData _leftHandData;
    private HandData _rightHandData;
    private Vector3 _originalFrogTongueScale;
    private Collider _hitFlyCollider = null;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip munchClip;
    public AudioClip croakClip;
    public AudioClip slurpClip;
    private bool _successFly = false;

    private bool _initialized = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        // Get the hand data
        UpdateHandData(_leftHandData);
        UpdateHandData(_rightHandData);

        // Check if there's a fly in front
        if (Physics.SphereCast(FroggyParentTransform.position, SphereCastRadius, -FroggyParentTransform.right, out RaycastHit hit, SphereCastDistance, FlyLayerMask))
        {
            if (_hitFlyCollider == null || hit.collider != _hitFlyCollider)
            {
                _hitFlyCollider = hit.collider;
            }
        }
        else
        {
            _hitFlyCollider = null;
        }

        // Sync tongue tip gameobject with extended tongue
        TongueTipObjectTransform.position = TongueTipTargetTransform.position;
        TongueTipObjectTransform.rotation = TongueTipTargetTransform.rotation;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerPress();
        }
    }

    public void Initialize()
    {
        if (!_initialized)
        {
            FrogTongueTransform.localScale = new Vector3(1, 0.1f, 1);
            _originalFrogTongueScale = FrogTongueTransform.localScale;
            _leftHandData = new HandData(LeftHand, LeftHandSkeleton);
            _rightHandData = new HandData(RightHand, RightHandSkeleton);
            audioSource = gameObject.GetComponent<AudioSource>();
            audioSource.clip = croakClip;
            audioSource.spatialBlend = 1f;
            audioSource.loop = false;
            audioSource.spatialize = true;
            audioSource.playOnAwake = false;
            var metaAudio = gameObject.AddComponent<MetaXRAudioSource>();
            metaAudio.EnableSpatialization = true;
            HideAllRenderers();
            _initialized = true;
        }
    }

    void UpdateHandData(HandData handData)
    {
        if (handData.Hand.IsTracked)
        {
            var distanceFromIndexFingerTipToThumbTip = Vector3.Distance(handData.IndexFingerTipTransform.position, handData.ThumbTipTransform.position);
            handData.IsIndexFingerPinching = distanceFromIndexFingerTipToThumbTip < MaxDistanceToGrab;

            if (FroggyActiveHand == null && handData.IsIndexFingerPinching && (Time.time - FroggyLastTriggeredTime) > CooldownTime)
            {
                FroggyActiveHand = handData.Hand;
                ShowAllRenderers();
                FroggyParentTransform.parent = FroggyActiveHand.transform;
                FroggyParentTransform.localPosition = FroggyPositionOffset;
                FroggyParentTransform.localEulerAngles = FroggyRotationOffset;

                if (FroggyActiveHand == LeftHand)
                {
                    FroggyParentTransform.localPosition = -FroggyPositionOffset;
                    FroggyParentTransform.localEulerAngles = FroggyRotationOffset + new Vector3(0, 0, 180);
                }

                TriggerPress();
                FroggyLastTriggeredTime = Time.time;
            }
        }
    }

    void ShowAllRenderers()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = true;
        }

        audioSource.mute = false;
    }

    void HideAllRenderers()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }
        //stop croaking while not in hand
        audioSource.mute = true;
    }

    void TriggerPress()
    {
        if (FroggyActiveHand != null && !FroggyActive)
        {
            if (_hitFlyCollider != null)
            {
                _successFly = true;
                float distance = Vector3.Distance(_hitFlyCollider.transform.position, TongueTipObjectTransform.position);
                float scaleY = Mathf.Clamp(map(distance, 0, SphereCastDistance, MinScaleY, MaxScaleY), MinScaleY, MaxScaleY);
                Debug.Log("Hitting " + _hitFlyCollider.name + ", extending tongue by " + scaleY);
                StartCoroutine(AnimateFrogTongueScale(_originalFrogTongueScale, new Vector3(1f, scaleY, 1f), GrabSpeed, ReturnSpeed));
            }
            else
            {
                _successFly = false;
                float scaleY = map(0.15f, 0, 1, MinScaleY, MaxScaleY);
                StartCoroutine(AnimateFrogTongueScale(_originalFrogTongueScale, new Vector3(1f, scaleY, 1f), GrabSpeed, ReturnSpeed));
            }
        }
    }

    IEnumerator AnimateFrogTongueScale(Vector3 inScale, Vector3 outScale, float grabSpeed, float returnSpeed)
    {
        //start slurping while lashing tongue
        audioSource.clip = slurpClip;
        audioSource.loop = false;
        audioSource.Play();

        float t = 0;
        FroggyActive = true;
        while (t <= 1)
        {
            FrogTongueTransform.localScale = Vector3.Lerp(inScale, outScale, t);
            t += Time.deltaTime * grabSpeed;
            yield return null;
        }
        yield return new WaitForSeconds(0.15f);

        Invoke(nameof(StartMunching), 0.2f);

        t = 0;
        while (Vector3.Distance(FrogTongueTransform.localScale, inScale) > 0.001f)
        {
            FrogTongueTransform.localScale = Vector3.Lerp(outScale, inScale, t);
            t += Time.deltaTime * returnSpeed;
            yield return null;
        }
        FroggyActive = false;
        FrogTongueTransform.localScale = inScale;
        FroggyActiveHand = null;
        FroggyParentTransform.parent = null;

        yield return new WaitForSeconds(0.2f);
        HideAllRenderers();
    }

    private void StartMunching()
    {
        audioSource.clip = munchClip;
        audioSource.Play();
        _successFly = false;
    }

    private void RestartCroaking()
    {
        audioSource.clip = croakClip;
        // audioSource.loop = true;
        audioSource.Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Fly")
        {
            Debug.Log("Fly caught!");
            // other.GetComponentInParent<FlyMovement>().isCaught = true;
            other.GetComponentInChildren<Animator>().speed = 0;
            other.transform.parent = TongueTipObjectTransform;
            other.transform.position = GetRandomPointWithinBounds(TongueTipObjectTransform.gameObject);
            other.transform.localScale = other.transform.localScale * 0.5f;

            Destroy(other.gameObject, 1f);
        }
    }

    private float map(float x, float in_min, float in_max, float out_min, float out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

    Vector3 GetRandomPointWithinBounds(GameObject obj)
    {
        // Get the Renderer component of the GameObject
        Renderer renderer = obj.GetComponent<Renderer>();

        Bounds bounds = renderer.bounds;

        // Generate random point within bounds
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, randomY, randomZ);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(FroggyParentTransform.position + SphereCastDistance * -FroggyParentTransform.right, SphereCastRadius);
        Gizmos.DrawRay(FroggyParentTransform.position, -FroggyParentTransform.right * SphereCastDistance);
    }
}
