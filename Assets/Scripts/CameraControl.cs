using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Transform targetPoint;

    [Range(0f, 89.9f)] public float angle;

    public float distance;
    [Range(0.2f,5f)]
    public float speed;
    private Vector3 dir;
    private float newTime = 0;

    private Vector3 newPos;
    // Start is called before the first frame update
    void Start()
    {
       // this.transform.LookAt(targetPoint);
    }

    // Update is called once per frame
    void Update()
    {
        distance += Input.mouseScrollDelta.y;
        distance = Mathf.Clamp(distance, 5f, 20f);
        dir = Quaternion.AngleAxis(angle, targetPoint.right) * -targetPoint.forward;
        newPos =targetPoint.position + dir*distance;
        if (transform.position != newPos)
        {
            newTime = 0;
        }
        newTime += Time.deltaTime;
        this.transform.position = Vector3.Lerp(this.transform.position, newPos, newTime*speed);
        this.transform.rotation = Quaternion.Slerp(
            this.transform.rotation, Quaternion.LookRotation(targetPoint.position-newPos), newTime*speed);
    }

    public void SetTarget(Transform player)
    {
        targetPoint = player;
    }
}
