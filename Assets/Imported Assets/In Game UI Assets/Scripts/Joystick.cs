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
using UnityEngine.EventSystems;


namespace InGameUI
{

    public class Joystick : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {

        private RectTransform rect_transform;

        [SerializeField]
        private RectTransform joystick_knob; // this will control joystick value


        private bool is_mouse_over; // if mouse is over this UI object


        private void Start()
        {
            rect_transform = GetComponent<RectTransform>();

        }

        private void Update()
        {

            if (Application.isMobilePlatform)
            {
                mobileControl();
            }
            else
            {
                pcControl();
            }
        }


        private void mobileControl()
        {

            bool any_touch = false; // if any touch is over this rect_transform

            foreach (Touch touch in Input.touches) // for each touch on the screen
            {
                Vector2 local_point;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect_transform, touch.position, null, out local_point))
                {
                    if (rect_transform.rect.Contains(local_point))
                    {
                        // if there is any touch on the screen, this value will be true because false || true evaluates to true
                        any_touch = any_touch || true;

                        // follow touch position
                        joystick_knob.transform.position = touch.position;

                        // keep joystick knob within bounds of this object's rect transform
                        joystick_knob.anchoredPosition = new Vector2(Mathf.Clamp(joystick_knob.anchoredPosition.x, -rect_transform.rect.width / 2, rect_transform.rect.width / 2),
                                                                     Mathf.Clamp(joystick_knob.anchoredPosition.y, -rect_transform.rect.height / 2, rect_transform.rect.height / 2));
                    }

                }
            }

            if (!any_touch) // if there is no touch on this game object
            {
                joystick_knob.anchoredPosition = Vector2.zero;

            }
        }



        private void pcControl()
        {
            if (is_mouse_over && Input.GetMouseButton(0)) // if mouse cursor is over this object while player is holding down mouse left button
            {
                joystick_knob.transform.position = Input.mousePosition; // knob should follow mouse position


                // keep joystick knob within bounds of this object's rect transform
                joystick_knob.anchoredPosition = new Vector2(Mathf.Clamp(joystick_knob.anchoredPosition.x, -rect_transform.rect.width / 2, rect_transform.rect.width / 2),
                                                             Mathf.Clamp(joystick_knob.anchoredPosition.y, -rect_transform.rect.height / 2, rect_transform.rect.height / 2));

            }
            else
            {
                joystick_knob.anchoredPosition = Vector2.zero; // reset knob position
            }
        }

        // return X,Y value for joystick
        public Vector2 getDirection()
        {
            return new Vector2(joystick_knob.anchoredPosition.x / (rect_transform.rect.width / 2),
                               joystick_knob.anchoredPosition.y / (rect_transform.rect.height / 2));
        }



        public void OnPointerEnter(PointerEventData eventData)
        {
            is_mouse_over = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            is_mouse_over = false;
        }
    }

}
