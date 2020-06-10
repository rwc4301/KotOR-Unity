using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ThirdPersonPlayerController : MonoBehaviour
{
    public float moveSpeed = 5, rotateSpeed = 3;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        transform.position += transform.forward * v * Time.deltaTime * moveSpeed;
        transform.Rotate(transform.up, h * rotateSpeed);
    }
}
