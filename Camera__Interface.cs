using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Interface : MonoBehaviour
{
    //카메라 설정값.
    private float m_rotationSpeed;
    private float m_translationSpeed;
    private float m_zoomSpeed;

    private float camera_dist_check;

    //마우스 현재 상태.
    //220822 싱글톤 변수로 변경
    //private float m_mouse_axis_X;
    //private float m_mouse_axis_Y;
    private float m_mouse_pos_X;
    private float m_mouse_pos_Y;

    //220816 피봇 생성 boolean 을 Camera Controller 싱글톤 멤버 변수 제어로 변경
    //private bool isPivotCreating = false;
    //Ortho 판단 boolean 도 마찬가지
    //private bool isOrthoCamera = false;

    //public Camera camera;

    public Transform m_centralAxis;
    public GameObject camera_pivot;
    public GameObject camera_dist_checker;

    public Camera main_camera;
    public Camera ui_Camera;

    private CursorManager cursorManager;
    private Main_Button_Event_Controller mbec;

    private void Start()
    {
        //220822
        //m_mouse_axis_X = 0;
        //m_mouse_axis_Y = 0;

        m_mouse_pos_X = 0;
        m_mouse_pos_Y = 0;
        main_camera.orthographicSize = 160;

        SetCameraSpeeds();

        cursorManager = GameObject.FindGameObjectWithTag("CursorManager").GetComponent<CursorManager>();
        mbec = GameObject.FindGameObjectWithTag("Main Button Controller").GetComponent<Main_Button_Event_Controller>();
    }

    void Update()
    {
        //카메라 거리를 계속 업데이트하여 싱글톤 멤버에 반영
        GetDist();

        //카메라 거리 체크에 따른 PAN, ZOOM 속도 유동적 변화
        SetCameraSpeeds();

        // 카메라 회전 피봇 생성
        if (Input.GetKeyDown(KeyCode.V))
        {
            cursorManager.SetCursor(CursorManager.CursorList.PIVOT);
            WaitforPivot();
        }

        // Projection 변경
        if (Input.GetKeyDown(KeyCode.T))
        {
            mbec.persp_ortho_Btn_Clicked();
            //if (!Camera_Controller.shared_instance.isOrthoCamera)
            //{
            //    Camera_Controller.shared_instance.isOrthoCamera = true;
            //    main_camera.orthographic = true;
            //    //Camera.main.orthographicSize = Vector3.Dot(Vector3.zero, Camera.main.transform.position);
            //}
            //else
            //{
            //    Camera_Controller.shared_instance.isOrthoCamera = false;
            //    main_camera.orthographic = false;
            //}

        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelEverything();
        }

        //Debug.Log("PAN SPEED : " + m_translationSpeed.ToString() + "\nZOOM SPEED : " + m_zoomSpeed.ToString());

        CameraControl();

        //Debug.Log(main_camera.transform.position);
        //Debug.Log(m_centralAxis.position);
        //Debug.Log(m_centralAxis.eulerAngles);
        //Debug.Log(camera_pivot.transform.position);
        //Debug.Log(camera_pivot.transform.eulerAngles);
    }

    private void CameraControl()
    {
        if (!Camera_Controller.shared_instance.isTouchingGUI)
        {
            Camera_Move();
            Camera_Rotate();
            Camera_Zoom();
        }
    }

    //휠만 누르는 경우 -> 이동
    private void Camera_Move()
    {
        if (Input.GetMouseButton(2) && !Input.GetKey(KeyCode.LeftControl))
        {
            m_mouse_pos_X = Input.GetAxis("Mouse X");
            m_mouse_pos_Y = Input.GetAxis("Mouse Y");

            m_centralAxis.transform.Translate(Vector3.right * -m_mouse_pos_X * m_translationSpeed * 0.5f);
            m_centralAxis.transform.Translate(transform.up * -m_mouse_pos_Y * m_translationSpeed * 0.5f, Space.World);
        }

        //Camera Mode 가 0 일때 (Move 모드일 때) 왼쪽 클릭으로 이동
        if (Camera_Controller.shared_instance.Camera_Mode == 0 && Camera_Controller.shared_instance.isPivotCreating == false)
        {
            if (Input.GetMouseButton(0))
            {
                m_mouse_pos_X = Input.GetAxis("Mouse X");
                m_mouse_pos_Y = Input.GetAxis("Mouse Y");

                m_centralAxis.transform.Translate(Vector3.right * -m_mouse_pos_X * m_translationSpeed * 0.5f);
                m_centralAxis.transform.Translate(transform.up * -m_mouse_pos_Y * m_translationSpeed * 0.5f, Space.World);
            }
        }
    }

    //휠+컨트롤 누르는 경우 -> 화면 전체 회전
    private void Camera_Rotate()
    {
        if (Input.GetMouseButton(2) && Input.GetKey(KeyCode.LeftControl))
        {
            //Vector2 mouse_pos = Input.mousePosition;
            //220822 변경
            //m_mouse_axis_X += Input.GetAxis("Mouse X");
            //m_mouse_axis_Y += Input.GetAxis("Mouse Y");
            Camera_Controller.shared_instance.Mouse_Axis_X += Input.GetAxis("Mouse X");
            Camera_Controller.shared_instance.Mouse_Axis_Y += Input.GetAxis("Mouse Y");

            //실제로 해보니 마우스 X랑 Y가 반대임
            //마우스가 아니고 Transform의 Rotation (centralAxis)가 반대임..
            //그리고 이건 1인칭 회전임

            //220822
            //Inspector 에 보이는 Rotation (Euler Angle) 을 여기서 아무리 transform.Euler, localEuler 갖고와도 다르다.
            //아무래도 각도가 0~360 사이의 각도를 -360해서 보여주는것같은데 이유는 모르겠다 (아마 내부 매트릭스 계산 때문인듯)
            //UnityEditor.TransformUtils.GetInspectorRotation(m_centralAxis.transform).x 이렇게 하면 Inspector의 Rotation 값을 가져올 수 있다...
            //근데 이렇게 하니까 말그대로 90 -> -270이던게 아예 값이 90이 되어서 그런지 Quaternion 값도 이상하게 나온다.. (당연히 -270을 변환해야 제대로 된 값이 나오는데
            //강제적으로 Inspector 값을 나오게 하니까 문제가 되는듯...
            m_centralAxis.rotation = Quaternion.Euler(new Vector3(m_centralAxis.transform.rotation.x - Camera_Controller.shared_instance.Mouse_Axis_Y,
                m_centralAxis.transform.rotation.y + Camera_Controller.shared_instance.Mouse_Axis_X, 0) * m_rotationSpeed);
            //m_centralAxis.rotation = Quaternion.Euler(new Vector3(m_centralAxis.rotation.x + m_mouse_axis_Y, m_centralAxis.rotation.y - m_mouse_axis_X, 0) * m_rotationSpeed);
        }

        //Camera Mode 가 1 일때 (Rotate 모드일 때) 왼쪽 클릭으로 회전
        if (Camera_Controller.shared_instance.Camera_Mode == 1 && Camera_Controller.shared_instance.isPivotCreating == false)
        {
            if (Input.GetMouseButton(0))
            {
                //220822 변경
                //m_mouse_axis_X += Input.GetAxis("Mouse X");
                //m_mouse_axis_Y += Input.GetAxis("Mouse Y");
                Camera_Controller.shared_instance.Mouse_Axis_X += Input.GetAxis("Mouse X");
                Camera_Controller.shared_instance.Mouse_Axis_Y += Input.GetAxis("Mouse Y");

                m_centralAxis.rotation = Quaternion.Euler(new Vector3(m_centralAxis.transform.rotation.x - Camera_Controller.shared_instance.Mouse_Axis_Y,
                    m_centralAxis.transform.rotation.y + Camera_Controller.shared_instance.Mouse_Axis_X, 0) * m_rotationSpeed);

            }
        }
    }

    //마우스 휠로 줌 인/아웃
    private void Camera_Zoom()
    {
        //Zoom
        //if (!isOrthoCamera)
        //    gameObject.transform.Translate(0, 0, Input.GetAxis("Mouse ScrollWheel") * m_zoomSpeed);
        //else
        //{
        //    Camera.main.orthographicSize -= Input.GetAxis("Mouse ScrollWheel") * m_zoomSpeed;

        //    if (Camera.main.orthographicSize < 0)
        //        Camera.main.orthographicSize = 0;
        //}

        //휠로 줌
        m_centralAxis.transform.Translate(0, - Input.GetAxis("Mouse ScrollWheel") * m_zoomSpeed, 0);
        //main_camera.orthographicSize -= Input.GetAxis("Mouse ScrollWheel") * m_zoomSpeed;
        ui_Camera.orthographicSize = main_camera.orthographicSize;

        //Ortho 카메라인경우 휠로 orthoSize 지정, 0 이하이면 0으로 고정
        //Camera.main.orthographicSize = Camera.main.transform.localPosition.y - Camera.main.fieldOfView;
        //main_camera.orthographicSize = m_centralAxis.transform.position.y + 200 - Camera.main.fieldOfView;
        main_camera.orthographicSize -= Input.GetAxis("Mouse ScrollWheel") * m_zoomSpeed;

        if (main_camera.orthographicSize < 1)
            main_camera.orthographicSize = 1;
    }

    //카메라 피봇을 만들기 위한 (이전 LookAt 삭제 -> 찍은곳에 새로 LookAt 만들기 -> 카메라 LookAt 옮기기)
    private void WaitforPivot()
    {
        Camera_Controller.shared_instance.isPivotCreating = true;
        //isPivotCreating = true;
        //Debug.Log(isPivotCreating.ToString());
    }

    //피봇 (테클라로 치면 마우스 Pivot 포인트) 설정을 위한 내부 On/Off
    private void CancelEverything()
    {
        //cursorManager.RollbackCursor();
        TurnBoolsOff();
    }

    private void TurnBoolsOff()
    {
        Camera_Controller.shared_instance.isPivotCreating = false;
        //isPivotCreating = false;
    }

    private void FixedUpdate()
    {
        if (Camera_Controller.shared_instance.isPivotCreating && Input.GetMouseButton(0))
        {
            //Debug.Log("Clicked");
            Ray ray = main_camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();

            //Ray 쏴서 맞는지 확인 (카메라 -> 100f 거리까지)
            //이게 일단 Unity 내에서 생성한 물체라면 문제가 없는데,
            //본인이 Import 한 물체라면 Collider 가 무조건 있어야 맞는다.
            //때문에 Component 추가해서 Mesh Collider 넣어주던지
            //(비추천:게임일 경우 물체가 매우 많은데 그거 Mesh 일일히 계산해서 Collider 생성하는건 유니티가 힘들어한다)
            //애초에 Collider 같이 Import 하던지 (되는지는 모르겠지만) 해야한다.

            if (Physics.Raycast(ray, out hit, 500f))
            {
                //제외될 Collider
                if (hit.transform.name != "E_F_Volume" && hit.transform.name != "E_F_Volume_Room" && hit.transform.name != "E_F_Reflection Proxy Volume")
                {
                    //Debug.Log("Hitted : " + hit.transform.name);

                    Vector3 hitPoint = hit.point;

                    //일단 맞는 높이 고정시킴 -> 후처리 필요
                    //hitPoint.y = 0.3f;
                    Vector3 exCameraPosition = this.transform.position;
                    m_centralAxis.position = hitPoint;
                    this.transform.position = exCameraPosition;
                    camera_pivot.transform.position = hitPoint;
                }
            }

            cursorManager.RollbackCursor();
            TurnBoolsOff();
        }
    }

    //Dynamic 하게 카메라 Pan, Zoom 속도 조절을 위한 거리 체크
    private void GetDist()
    {
        Camera_Controller.shared_instance.Camera_Dist = Mathf.Sqrt(Mathf.Pow((this.transform.position.x - camera_dist_checker.transform.position.x), 2)
            + Mathf.Pow((this.transform.position.y - camera_dist_checker.transform.position.y), 2)
            + Mathf.Pow((this.transform.position.z - camera_dist_checker.transform.position.z), 2));
    }

    //카메라 스피드 초기화
    //환경설정 (Control) 반영하여 설정함.
    //카메라와 카메라 기준 (camera_dist_check) 반영하여 가속도 설정함.
    //기초 속도 + 가속도 계산 하는 형태로 진행
    //환경설정 -> 기초 속도도 선택, 가속도도 선택할 것인지? 둘중 하나만 할 것인지?
    private void SetCameraSpeeds()
    {
        camera_dist_check = Camera_Controller.shared_instance.Camera_Dist;
        m_translationSpeed = (Camera_Controller.shared_instance.Mouse_Sensitivity / 5) + (0.004f * Camera_Controller.shared_instance.Mouse_Sensitivity * Camera_Controller.shared_instance.Mouse_Acceleration * camera_dist_check);
        m_zoomSpeed = Camera_Controller.shared_instance.Zoom_Sensitivity + (0.02f * Camera_Controller.shared_instance.Zoom_Sensitivity * Camera_Controller.shared_instance.Mouse_Acceleration * camera_dist_check);
        m_rotationSpeed = Camera_Controller.shared_instance.Rotate_Sensitivity * 0.4f;
    }
}
