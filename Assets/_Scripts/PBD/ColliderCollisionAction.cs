using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBD
{
    public class ColliderCollisionAction : MonoBehaviour
    {
        public event Action<Collision> OnStay;
        public event Action<Collision> OnEnter;
        public event Action<Collision> OnExit;

        private void OnCollisionStay(Collision other)
        {
            OnStay?.Invoke(other);
        }

        private void OnCollisionEnter(Collision other)
        {
            OnEnter?.Invoke(other);
        }

        private void OnCollisionExit(Collision other)
        {
            OnExit?.Invoke(other);
        }
    }
}