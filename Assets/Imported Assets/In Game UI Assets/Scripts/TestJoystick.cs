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


namespace InGameUI
{

    public class TestJoystick : MonoBehaviour
    {

        private TMPro.TMP_Text tmp_text;


        [SerializeField]
        private Joystick joystick;

        // Start is called before the first frame update
        void Start()
        {
            tmp_text = GetComponent<TMPro.TMP_Text>();
        }

        // Update is called once per frame
        void Update()
        {

            tmp_text.text = joystick.getDirection().x.ToString("n2") + " , " + joystick.getDirection().y.ToString("n2");
        }
    }

}
