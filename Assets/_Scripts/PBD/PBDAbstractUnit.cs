using UnityEngine;

namespace PBD
{
    public abstract class PBDAbstractUnit : MonoBehaviour
    {
        [SerializeField] private Transform _hips;
        [SerializeField] private Transform _neck;
        [SerializeField] private Rigidbody[] _rigidbodiesForTurningOff;
        private int _hipsIndex;
        private int _neckIndex;
        private bool _physicsEnabled = true;
        private PBDObject _pbdObject;
        
//        protected Vector3 hipsPosition => _pbdObject[_hipsIndex];
//        protected Vector3 neckPosition => _pbdObject[_neckIndex];

        public Transform Hips => _hips;
        public Transform Neck => _neck;

        public int HipsIndex
        {
            get => _hipsIndex;
            set => _hipsIndex = value;
        }

        public int NeckIndex
        {
            get => _neckIndex;
            set => _neckIndex = value;
        }

        public bool PhysicsEnabled
        {
            get => _physicsEnabled;
            set
            {
                _physicsEnabled = value;
                foreach (Rigidbody body in _rigidbodiesForTurningOff)
                {
                    body.isKinematic = !_physicsEnabled;
                }
            }
        }

        public PBDObject PbdObject
        {
            set => _pbdObject = value;
        }
    }
}