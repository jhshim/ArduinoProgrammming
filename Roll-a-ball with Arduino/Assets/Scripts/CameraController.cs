using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour

{
    private Vector3 offset;
    private float horizontal;

    public GameObject player;

    // Use this for initialization
    void Start()
    {
        offset = transform.position;
        horizontal = 0;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = player.transform.position + offset;

        if (PlayerController.cameraHorizontal == 'l')
            horizontal += 1.5f;  // 바라보는 시점을 시계 방향으로 움직임
        else if (PlayerController.cameraHorizontal == 'r')
            horizontal -= 1.5f;  // 바라보는 시점을 시계 반대 방향으로 움직임
        else if(PlayerController.cameraHorizontal == 'a')
            horizontal = 0;  // 바라보는 시점을 원래의 위치로 되돌림

        if (horizontal > 180)
        {  // arithmetic overflow를 막기 위해 180을 넘으면 음수로 변환함 (180도)
           // ex) 190 -> -170
            float overflow = horizontal - 180;
            horizontal = 180 - overflow;
            horizontal *= -1;
        }
        else if(horizontal < -180)
        {  // arithmetic overflow를 막기 위해 -180보다 작으면 양수로 변환함
           // ex) -170 -> 190
            float overflow = horizontal + 180;
            overflow *= -1;
            horizontal = 180 - overflow;
        }

        if (horizontal != 0)
            transform.rotation = Quaternion.Euler(0f, horizontal, 0f);  // 바라보는 시점을 본격적으로 움직임
        else
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }
}
