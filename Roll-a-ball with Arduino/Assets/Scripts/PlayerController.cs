using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.IO.Ports;

public class PlayerController : MonoBehaviour 
{	
    private int count;
    private int numberOfGameObjects;
    private float elapseTime;  // elapseTime: 시간 경과를 의미
    private float moveTime;  // moveTime: Cube 하나가 임의로 움직이도록 하기 위해 사용

    private SerialPort sp = new SerialPort("COM4", 9600);  //set Serial port
    private double[] val = new double[5];
    private double[] angle = new double[2];

    public float speed;
    public GUIText countText;
    public GUIText winText;
    public Slider timeBar;  // timeBar: 게임 화면 상단 우측의 Slider
    public GameObject randomCube;  // randomCube: 2초마다 임의로 움직이는 Cube
    public Text testText;

    public static char cameraHorizontal;  // cameraHorizontal: 3개의 버튼 중 어떤 버튼이 눌렸는지에 대한 신호

    void Start()
	{
        string args = "";
        string[] arguments = Environment.GetCommandLineArgs();

        for (int i = 0; i < arguments.Length; i++)
        {
            if (i == 0) continue;

            args += arguments[i] + " ";
        }

        count = 0;
        numberOfGameObjects = GameObject.FindGameObjectsWithTag("PickUp").Length;
        SetCountText();
		winText.text = "";
        elapseTime = 0;  // 초기 시간 경과는 0으로 세팅함
        timeBar = GameObject.Find("Timebar").GetComponent<Slider>();  // 게임 화면의 Slider를 연결시킴
        randomCube = GameObject.Find("RandomPickUp");  // 게임 화면의 Cube를 연결시킴

        testText = GameObject.Find("testText").GetComponent<Text>();
        testText.text = args;

        sp.Open();  //Serial port open
        sp.ReadTimeout = 1;  //set Serial timeout
    }

    void Update()
    {
        elapseTime += Time.deltaTime;
        timeBar.value = elapseTime;  // Slider의 값을 변경함

        moveTime += Time.deltaTime;

        if ((int)moveTime == 2)  // moveTime이 0 -> 1 -> 2 가 되는 순간에 Cube 하나의 위치가 임의로 바뀜
        {
            randomCube.transform.position = new Vector3(UnityEngine.Random.Range(-8f, 8f), 0.5f, UnityEngine.Random.Range(-8f, 8f));
            moveTime = 0;
        }

        if ((int)elapseTime >= 180)  // 시간 초과 시 텍스트 출력 후 게임 종료
        {
            winText.text = "TIME OVER...";
            //UnityEditor.EditorApplication.isPlaying = false;
        }
    }
	
	void FixedUpdate ()
	{
        if (sp.IsOpen)
        {
            try
            {
                sp.Write("s");  //send start data
                val[0] = sp.ReadByte();  //read a byte
                if (val[0] == 0xff)
                {  //check start byte
                    for (int i = 1; i < 5; i++)
                    {
                        val[i] = sp.ReadByte();
                    }

                    angle[0] = val[1] * (val[2] - 2);  //calculate value
                    angle[1] = val[3] * (val[4] - 2);

                    float moveHorizontal;
                    float moveVertical;

                    if (angle[0] >= -70 && angle[0] <= 70)
                    {
                        moveHorizontal = (float) convertPos(angle[0]);
                    }
                    else
                    {
                        if (angle[0] > 0)
                            moveHorizontal = 1;
                        else
                            moveHorizontal = -1;
                    }

                    if (angle[1] >= -70 && angle[1] <= 70)
                    {
                        moveVertical = (float)convertPos(angle[1]);
                    }
                    else
                    {
                        if (angle[1] > 0)
                            moveVertical = 1;
                        else
                            moveVertical = -1;
                    }

                    Vector3 movement = new Vector3(moveHorizontal * -1, 0.0f, moveVertical);

                    GetComponent<Rigidbody>().AddForce(movement * speed * Time.deltaTime);

                    /*
                     * angle[1]: 위쪽(+), 아래쪽(-) [-70 ~ 70]
                     * angle[0]: 왼쪽(-), 오른쪽(+) [-70 ~ 70]
                     */
                }

                String btnState = sp.ReadLine();  // btnState: 어떤 버튼이 눌렸는지에 대한 정보를 담음

                if (btnState.Equals("btn1On"))
                    cameraHorizontal = 'l';  // 바라보는 시점을 시계 방향으로 움직인다는 뜻
                else if (btnState.Equals("btn2On"))
                    cameraHorizontal = 'r';  // 바라보는 시점을 시계 반대 방향으로 움직인다는 뜻
                else if (btnState.Equals("btn3On"))
                    cameraHorizontal = 'a';  // 바라보는 시점을 원위치로 움직인다는 뜻
                else
                    cameraHorizontal = ' ';

            }
            catch (System.Exception) { }
        }
    }

    double convertPos(double n)  // -70 ~ 70 값을 -1 ~ 1 으로 변환해주는 함수
    {
        return n / 70 * 1;
    }

    void OnTriggerEnter(Collider other) 
	{
		if(other.gameObject.tag == "PickUp")
		{
			other.gameObject.SetActive(false);
			count = count + 1;
			SetCountText();
		}
	}
	
	void SetCountText ()
	{
		countText.text = "Count: " + count.ToString();
		if(count >= numberOfGameObjects)  // Cube 5개를 모두 먹었을 경우 텍스트 출력 후 게임 종료
        {
			winText.text = "YOU WIN!";
            //UnityEditor.EditorApplication.isPlaying = false;
        }
	}
}
