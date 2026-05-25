using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotate : MonoBehaviour
{
    public float rotationSpeed = 180f; // degrees per second

    void Update()
    {
        transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
    }
}