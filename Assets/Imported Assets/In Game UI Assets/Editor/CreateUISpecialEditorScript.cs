/*
 * 
 * Developed by Olusola Olaoye, 2022
 * 
 * To only be used by those who purchased from the Unity asset store
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;


public class CreateUISpecialEditorScript
{

    [MenuItem("GameObject/In Game UI/Player 1 Fight Panel", false, -1)]
    private static void createPlayer1FightPanel()
    {
        createPrefabResource("Player 1 Fight Panel");

    }
    

    [MenuItem("GameObject/In Game UI/Player 2 Fight Panel", false, -1)]
    private static void createPlayer2FightPanel()
    {
        createPrefabResource("Player 2 Fight Panel");

    }
    
    [MenuItem("GameObject/In Game UI/Joystick 1", false, -1)]
    private static void createJoystick1()
    {
        createPrefabResource("Joystick 1");

    }
    
    [MenuItem("GameObject/In Game UI/Joystick 2", false, -1)]
    private static void createJoystick2()
    {
        createPrefabResource("Joystick 2");

    }
    
    [MenuItem("GameObject/In Game UI/HealthBar 1", false, -1)]
    private static void createHealthBar1()
    {
        createPrefabResource("HealthBar 1");

    }
    

    [MenuItem("GameObject/In Game UI/HealthBar 2", false, -1)]
    private static void createHealthBar2()
    {
        createPrefabResource("HealthBar 2");

    }

    [MenuItem("GameObject/In Game UI/HealthBar 3", false, -1)]
    private static void createHealthBar3()
    {
        createPrefabResource("HealthBar 3");

    }

    
    [MenuItem("GameObject/In Game UI/Speedometer 1", false, -1)]
    private static void createSpeedometer1()
    {
        createPrefabResource("Speedometer 1");

    }
    

    [MenuItem("GameObject/In Game UI/Speedometer 2", false, -1)]
    private static void createSpeedometer2()
    {
        createPrefabResource("Speedometer 2");

    }
    

    [MenuItem("GameObject/In Game UI/Speedometer 3", false, -1)]
    private static void createSpeedometer3()
    {
        createPrefabResource("Speedometer 3");

    }
    

    [MenuItem("GameObject/In Game UI/Curved Bar", false, -1)]
    private static void createCurvedBar()
    {
        createPrefabResource("Curved Bar");

    }
    


    private static void createPrefabResource(string object_name)
    {

        Object ui_object = Resources.Load("prefabs/" + object_name); // find prefab in resources

        GameObject ui_game_object = (GameObject)GameObject.Instantiate(ui_object, Vector3.zero, Quaternion.identity); // instantiate object

        ui_game_object.name = object_name; // name object



        GameObject selected_object = Selection.activeGameObject; // current selected game object


        if (selected_object) // if selected object is not null
        {

            if (selected_object.GetComponent<RectTransform>()) // if there is a current seleted game object and that game object has a RectTransform component
            {
                ui_game_object.transform.SetParent(selected_object.transform, false);
            }

            else// if there is a current seleted game object and that game object does not have a RectTransform component
            {
                GameObject canvas = createCustomCanvasObject(); // create a canvas object

                canvas.transform.SetParent(selected_object.transform); // set canvas object to child of selected game object

                ui_game_object.transform.SetParent(canvas.transform, false); // set the ui object to the child of the canvas
            }

        }
        else // if selected object is null
        {
            // create a canvas object if none exists
            Canvas existingCanvas = Object.FindAnyObjectByType<Canvas>();
            GameObject canvas = existingCanvas ? existingCanvas.gameObject : createCustomCanvasObject();

            // set the ui object to the child of the canvas
            ui_game_object.transform.SetParent(canvas.transform, false);
        }


        Selection.activeGameObject = ui_game_object;
    }


    private static GameObject createCustomCanvasObject()
    {
        // create a gameobject with all the canvas components attached 
        GameObject canvas_object = new GameObject("Canvas", new System.Type[] { typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster) });

        canvas_object.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; // set canvas to screen space mode

        canvas_object.layer = 5; // Ui layer

        return canvas_object;
    }
}