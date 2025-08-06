using System.Collections;
using Unity.Cinemachine;
using Unity.Profiling;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ConsoleScript : MonoBehaviour
{

    [SerializeField] public RectTransform console_rect;
    [SerializeField] public Text display_text;

    [SerializeField] public InputField input_text;

    PlayerInput player_input;

    public bool is_console_open = false;

    public static ConsoleScript instance;

    [SerializeField] private string commandWord = string.Empty;

    [SerializeField] private RBPlayerHandler player;

    [SerializeField] private CinemachineCamera player_camera;


    void Start()
    {

        player_input = GetComponent<PlayerInput>();
        input_text.onSubmit.AddListener(ProcessCommand);
    }

    // Update is called once per frame
    void Update()
    {

        if (player_input.actions.FindAction("DebugConsole").triggered)
        {
            is_console_open = !is_console_open;
            Debug.Log("IS CONSOLE OPEN: " + is_console_open.ToString());
        }
        openConsole();

        if (player_input.actions.FindAction("Submit").triggered)
        {
            ProcessCommand(input_text.text);
        }
    }

    void openConsole()
    {
        if (is_console_open)
        {
            console_rect.localPosition = new Vector2(0, 157);
            input_text.enabled = true;
            input_text.IsActive();
            input_text.ActivateInputField();
        }
        else
        {
            console_rect.localPosition = new Vector2(0, 400);
            input_text.enabled = false;
        }
    }


    public void ProcessCommand(string input_value)
    {
        input_text.text = string.Empty;
        LogCommand(input_value);
        if (input_value.Contains("/camera_fov"))
        {
            int fov_value = int.Parse(input_value.Replace("/camera_fov", ""));
            player_camera.Lens.FieldOfView = fov_value;
        }
        if (input_value.Contains("/m_speed"))
        {
            float sens_value = float.Parse(input_value.Replace("/m_speed", ""));
            var axisController = player_camera.GetComponent<CinemachineInputAxisController>();
            Debug.Log("HAIII :3");
            foreach (var c in axisController.Controllers)
            {

                if (c.Name == "Look X (Pan)")
                {
                    c.Input.Gain = sens_value;

                }
                if (c.Name == "Look Y (Tilt)")
                {
                    c.Input.Gain = -sens_value;
                }
            }

        }
        if (input_value.Contains("/movement_type"))
        {
            int movement_value = int.Parse(input_value.Replace("/movement_type", ""));
            player.movement_type = movement_value;
        }
    }


    public void LogCommand(string new_log)
    {
        display_text.text = new_log + "\n" + display_text.text;
    }

}
