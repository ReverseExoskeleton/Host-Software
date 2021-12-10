using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeCollision : MonoBehaviour
{
    [SerializeField]
    private NinjaArmController controller;

    private void OnTriggerEnter(Collider other)
    {
        controller.AddFruitHit(other.gameObject);
    }
}
