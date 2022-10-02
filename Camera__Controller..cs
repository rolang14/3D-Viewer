using System;
using System.IO;
using UnityEngine;

public class Camera_Controller
{
    //Control 환경설정에서 멤버 접근을 위한 Singleton 객체 생성
    private static Camera_Controller camera_instance = null;
    public static Camera_Controller shared_instance
    {
        get
        {
            if (camera_instance == null)
                camera_instance = new Camera_Controller();

            return camera_instance;
        }
    }

    private Camera_Controller()
    {
        ReadINIFile();
    }

    private void Camera_Controller_Initialize(float mouse, float zoom, float rot, int accel, Vector3 camera_pos, Vector3 camera_pivot_pos, Vector3 camera_pivot_rot, Vector3 camera_aim_pos, Vector3 camera_aim_rot, float camera_ortho_size)
    {
        Mouse_Sensitivity = mouse;
        Zoom_Sensitivity = zoom;
        Rotate_Sensitivity = rot;
        Mouse_Acceleration = accel;
        Camera_Mode = 0;
        isPivotCreating = false;
        isOrthoCamera = false;
        Mouse_Axis_X = 0;
        Mouse_Axis_Y = 0;
        isTouchingGUI = false;
        isSettingGUIOpen = false;

        Camera_Home_Position = camera_pos;
        Camera_Pivot_Home_Position = camera_pivot_pos;
        Camera_Pivot_Home_Rotation = camera_pivot_rot;
        Camera_Aim_Home_Position = camera_aim_pos;
        Camera_Aim_Home_Rotation = camera_aim_rot;
        Camera_Ortho_Size = camera_ortho_size;
    }

    private void ReadINIFile()
    {
        try
        {
            INIFile inifile = new INIFile();
            //파일 없으면 하나 만들기
            FileInfo fileinfo = new FileInfo(inifile._ini_Path);
            if (!fileinfo.Exists)
            {
                fileinfo.Create().Close();
                Camera_Controller_Initialize(5, 5, 5, 1, new Vector3(-204.5f, 297.8f, -104.0f), new Vector3(-204.2f, 28.6f, -117.0f), 
                    Vector3.zero, new Vector3(-184.7f, 28.6f, -117.0f), new Vector3(270.0f, 180.0f, 0.0f), 160);
                return;
            }

            #region Vector3 Parse

            Vector3 cam_pos, cam_pivot_pos, cam_pivot_rot, cam_aim_pos, cam_aim_rot;

            try
            {
                //string -> Vector3 변환
                //Cam_Position
                string data_Vector3 = inifile.GetString(inifile._SecControlOptions, inifile._OpCamPos, "");
                string[] vector3_parser = data_Vector3.Split(',');

                cam_pos = new Vector3(float.Parse(vector3_parser[0]), float.Parse(vector3_parser[1]),
                    float.Parse(vector3_parser[2]));

                //Cam_Pivot_Position
                data_Vector3 = inifile.GetString(inifile._SecControlOptions, inifile._OpCamPivotPos, "");
                vector3_parser = data_Vector3.Split(',');

                cam_pivot_pos = new Vector3(float.Parse(vector3_parser[0]), float.Parse(vector3_parser[1]),
                    float.Parse(vector3_parser[2]));

                //Cam_Pivot_Rotation
                data_Vector3 = inifile.GetString(inifile._SecControlOptions, inifile._OpCamPivotRot, "");
                vector3_parser = data_Vector3.Split(',');

                cam_pivot_rot = new Vector3(float.Parse(vector3_parser[0]), float.Parse(vector3_parser[1]),
                    float.Parse(vector3_parser[2]));

                //Cam_Aim_Position
                data_Vector3 = inifile.GetString(inifile._SecControlOptions, inifile._OpCamAimPos, "");
                vector3_parser = data_Vector3.Split(',');

                cam_aim_pos = new Vector3(float.Parse(vector3_parser[0]), float.Parse(vector3_parser[1]),
                    float.Parse(vector3_parser[2]));

                //Cam_Aim_Position
                data_Vector3 = inifile.GetString(inifile._SecControlOptions, inifile._OpCamAimRot, "");
                vector3_parser = data_Vector3.Split(',');

                cam_aim_rot = new Vector3(float.Parse(vector3_parser[0]), float.Parse(vector3_parser[1]),
                    float.Parse(vector3_parser[2]));
            }
            catch
            {
                cam_pos = new Vector3(-204.5f, 297.8f, -104.0f);
                cam_pivot_pos = new Vector3(-204.2f, 28.6f, -117.0f);
                cam_pivot_rot = Vector3.zero;
                cam_aim_pos = new Vector3(-184.7f, 28.6f, -117.0f);
                cam_aim_rot = new Vector3(270.0f, 180.0f, 0.0f);
            }

            #endregion

            //INI 설정값 LOAD
            Camera_Controller_Initialize(inifile.GetFloat(inifile._SecControlOptions, inifile._OpPan, 5),
                inifile.GetFloat(inifile._SecControlOptions, inifile._OpZoom, 5),
                inifile.GetFloat(inifile._SecControlOptions, inifile._OpRotate, 5),
                inifile.GetInteger(inifile._SecControlOptions, inifile._OpAccel, 1), 
                cam_pos, cam_pivot_pos, cam_pivot_rot, cam_aim_pos, cam_aim_rot,
                inifile.GetFloat(inifile._SecControlOptions, inifile._OpCamOrthoSize, 160));
        }
        catch (TypeInitializationException)
        {
            return;
        }
    }

    //Control 환경설정에서 멤버 접근을 위한 Singleton 객체 멤버 변수
    //카메라 속도를 위함.

    //가속도 없음, 카메라 회전 속도
    public float Rotate_Sensitivity { get; set; }

    //고정/가속도 (1~10)
    //5(mid) - 1/0.02
    public float Mouse_Sensitivity { get; set; }

    //고정/가속도 (1~10)
    //5(mid) - 5/0.1
    public float Zoom_Sensitivity { get; set; }

    //카메라 거리에 따라 Mouse Acceleration On/Off
    public int Mouse_Acceleration { get; set; }

    //Home 기능 카메라 정보 추가, 순서대로 카메라, 카메라 피봇, 피봇 아이콘
    public Vector3 Camera_Home_Position { get; set; }
    //public Vector3 Camera_Home_Rotation { get; set; }
    public Vector3 Camera_Pivot_Home_Position { get; set; }
    public Vector3 Camera_Pivot_Home_Rotation { get; set; }
    public Vector3 Camera_Aim_Home_Position { get; set; }
    public Vector3 Camera_Aim_Home_Rotation { get; set; }
    public float Camera_Ortho_Size { get; set; }

    //카메라 거리
    public float Camera_Dist { get; set; }

    /// <summary>
    /// 카메라 모드
    /// 0: Move (Click to move)
    /// 1: Rotate (Click to rotate)
    /// </summary>
    public int Camera_Mode { get; set; }

    /// <summary>
    /// 카메라 피봇 생성용
    /// true : 생성 프로세스 진입
    /// false : 프로세스 종료 시 or 생성 프로세스 진입 안한 경우
    /// </summary>
    public bool isPivotCreating { get; set; }

    /// <summary>
    /// 카메라가 Ortho View 상태인지 Persp View 살태인지
    /// true : Ortho View Status
    /// false : Persp View Status
    /// </summary>
    public bool isOrthoCamera { get; set; }

    //220822 View 바뀌면 Mouse Axis 도 같이 초기화해줘야 부드럽게 움직일 것 같음
    public float Mouse_Axis_X { get; set; }
    public float Mouse_Axis_Y { get; set; }

    //GUI 위에 있는 경우 카메라 이동을 중지
    public bool isTouchingGUI { get; set; }

    //Setting 창이 Enable 상태인지
    public bool isSettingGUIOpen { get; set; }
}
