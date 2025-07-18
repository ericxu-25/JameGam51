using UnityEngine;

namespace Globals 
{
    public class Follow : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Object to follow")]
        public Transform followTransform;

        [SerializeField]
        [Tooltip("Whether to follow the rotation or not")]
        public bool followRotation;

        [SerializeField]
        [Tooltip("Whether to follow the position or not")]
        public bool followPosition;

        private float _sqrFollowSharpness;

        [SerializeField]
        [Tooltip("How much to follow the target by at each timestep")]
        private float _followSharpness;

        [SerializeField]
        [Tooltip("How much to follow the target's rotation by at each timestep")]
        private float _followRotationSharpness;

        public float FollowSharpness
        {
            get
            {
                return _followSharpness;
            }
            set
            {
                _followSharpness = Mathf.Clamp(value, 0, Mathf.Infinity);
                _sqrFollowSharpness = value * value;
            }
        }

        [SerializeField]
        [Tooltip("Whether or not to use unscaled time or not when following")]
        public bool useUnscaledTime = false;

        private void OnValidate()
        {
            FollowSharpness = _followSharpness;
        }
        void Start()
        {
            gameObject.transform.position = followTransform.position;
            FollowSharpness = _followSharpness;
        }

        // Update is called once per frame
        void Update()
        {
            // follow position
            if (followPosition)
            {
                Vector3 difference = (followTransform.position - gameObject.transform.position);
                if (difference.sqrMagnitude > _sqrFollowSharpness)
                {
                    difference = difference.normalized * (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * _followSharpness;
                    gameObject.transform.Translate(difference);
                }
                else
                {
                    gameObject.transform.position = followTransform.position;
                }
            }
            // follow rotation
            if (followRotation)
            {
                gameObject.transform.rotation =  Quaternion.RotateTowards(gameObject.transform.rotation, followTransform.rotation, _followRotationSharpness * (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime));
            }
        }
    }
}

