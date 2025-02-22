﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
using MalbersAnimations.Utilities;
using MalbersAnimations.Events;
using System;

namespace MalbersAnimations.Selector
{
    //──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    //VARIABLES AND PROPERTIES
    //──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    public partial class SelectorController
    {
        static Keyframe[] K = { new Keyframe(0, 0), new Keyframe(1, 1) };

        #region Public Variables
        
        /// <summary>
        /// Animate(Move|Rotate) the Selector setting the Focused Item on the center of the Selector Controller
        /// </summary>
        public bool AnimateSelection = true;

        public bool SoloSelection = false;                          //Select the items by click in on the item
        public bool Hover = false;                                  //if true, selection by hovering on the item is activated
        public float SelectionTime = 0.3f;
        public float RestoreTime = 0.15f;                            //Restore Time of an Item to its original position.
        public AnimationCurve SelectionCurve = new AnimationCurve(K);

        public int focusedItemIndex = 0;                            //This is for set the Selection everytime is called to this new Index
        public float DragSpeed = 20;                                //Drag/Swipe Speed        
        public bool dragHorizontal = true;

        public float Threshold = 1f;                                //Range to identify the hold and swap/drag as a click

        /// <summary>
        /// Click over an item to focus and centered it if is not the current focused item 
        /// </summary>
        public bool ClickToFocus = true;
        public Material LockMaterial;                               //Lock Material 

        public bool frame_Camera;                                   //Frame the camera to the bound box of the Item
        public float frame_Multiplier = 1;                          //Multiplier for make

        public bool RotateIdle, MoveIdle, ScaleIdle;                //Set Animations Idles

        public float ItemRotationSpeed = 5;                         //TurnTable Speed        
        public Vector3 TurnTableVector = Vector3.up;                //TurnTable Vector 
        public bool ChangeOnEmptySpace = true;                      //If there's a Click/Touch on an empty space change to the next/previous item
        public TransformAnimation MoveIdleAnim, ScaleIdleAnim;      //Idle Animations
        #endregion

        #region Internal Variables
        protected DeltaTransform InitialTransform;                  //Initial transform of the Selector

        protected float angle                                      //The Angle between Items       (For Circle Selector)
        {get {return S_Editor.Angle;} }
    
        protected float distance                                    //The Distance between items    (For Linear Selector) 
        { get { return S_Editor.distance / 2; } }    
        

        protected bool isActive = true;

        protected Vector3 InitialPosCam;                            //Initial Camera Position where the CircleEditor edited it
        private MItem current;                                      //Current Selected Mitem

        protected int Rows                                          //Gets the total of the Rows for the grid 
        { get { return Items.Count / S_Editor.Grid + 1; } }

        protected int GridTotal                                     //Gets the total grid size
        { get { return Rows * S_Editor.Grid; } }
    
        protected Vector3 Linear                                    //The Linear vector storage.
        { get { return S_Editor.LinearVector; } }

        protected bool isChangingItem = false;                      //Check if is changin items
        protected bool isAnimating = false;                         //Check if is Animating                   
        protected bool isSwapping = false;                          //Check if is Swapping
        protected bool isRestoring = false;                         //Check if is Restoring

        protected DeltaTransform LastTCurrentItem;                  //Last current item position

        private int indexSelected = -1;                             //The selected Index
        private int previousIndex = -1;                             //The Previous selected Index

        internal float IdleTimeMove = 0, IdleTimeScale = 0;         //The global Idle Time for playing idle transform animations.
        internal float DragDistance = 0;                            //How much distance was traveled when is dragin/swapping
        internal int UILayer;

        public List<MItem> Items
        {
            get { return s_Editor.Items; }
        }

        #region MouseVariables
        protected DeltaTransform DeltaSelectorT;
        protected Vector3 MouseStartPosition;                       //the First mouse/touch position when the click/touch is down.
        protected Vector2 DeltaMousePos;

        private MItem LastAnimatedItem;                             //Last Animated Item
        private TransformAnimation LastAnimation;                   //Last animation of the last Animated Item

        private bool isEnabling = true;                             //Use for Skip the Animation and Restoring when the Controller is enabled
        private bool IsSelectTransformAnimation;                    //No se pa que lo puse pero no lo puedo quitar

        private PointerEventData CurrentEventData;                  //Store the current event data
        private MItem HitItem; //Hover Last Hitted Item
        #endregion
        #endregion

        #region Inputs
        public InputRow ChangeLeft = new InputRow("", KeyCode.LeftArrow, InputButton.Down);
        public InputRow ChangeRight = new InputRow("", KeyCode.RightArrow, InputButton.Down);
        public InputRow ChangeUp = new InputRow("", KeyCode.UpArrow, InputButton.Down);
        public InputRow ChangeDown = new InputRow("", KeyCode.DownArrow, InputButton.Down);

        public InputRow Submit = new InputRow("Submit", KeyCode.Return, InputButton.Down);
        #endregion

        #region Coroutines
        IEnumerator AnimateSelectionC;
        IEnumerator PlayTransformAnimationC;
        #endregion

        #region Events
        public GameObjectEvent OnClickOnItem = new GameObjectEvent();
        public GameObjectEvent OnItemFocused = new GameObjectEvent();
        public BoolEvent OnIsChangingItem = new BoolEvent();
        #endregion

        #region Properties
        /// <summary>
        /// Returns the Current Selected Item
        /// </summary>
        public MItem CurrentItem
        {
            get { return current; }
        }

        /// <summary>
        /// Return the LocalRotation of the Radial Axis of the Selector 
        /// </summary>
        protected Vector3 RadialVector
        {
            get
            {
                switch (S_Editor.RadialAxis)
                {
                    case RadialAxis.Up:
                        return transform.InverseTransformDirection(transform.up);
                    case RadialAxis.Right:
                        return transform.InverseTransformDirection(-transform.right);
                    case RadialAxis.Forward:
                        return transform.InverseTransformDirection(-transform.forward);
                }
                return transform.up;
            }
        }

        /// <summary>
        /// Return the Index of the Selected Item... also Invoke OnItemSelected
        /// </summary>
        public  int IndexSelected
        {
            get { return indexSelected; }
            private set
            {
                if (Items == null || Items.Count == 0) return;

                //if (indexSelected == value) return;             //Skip if is the same Index
                previousIndex = indexSelected;                  //Set the previous item to the las tiem selected

                indexSelected = value;


                Animate_to_Next_Item();                         //Activate all the Animations to change Item

                if (indexSelected == -1)                        //Clear Selection
                {
                    current = null;
                    OnItemFocused.Invoke(null);                //Let everybody knows that NO item was selected;
                }
                else
                {
                    indexSelected =
                        indexSelected < 0 ? Items.Count - 1 : indexSelected % Items.Count; //Just set the IndexSelect to the Limit Range

                    current = Items[indexSelected];
                    FocusedItemIndex = indexSelected;

                    if (S_Manager.Data) S_Manager.Data.Save.FocusedItem = FocusedItemIndex;         //UPDATE THE DATA WITH THE FOCUSED ITEM

                    LastTCurrentItem.LocalPosition = current.StartPosition;
                    LastTCurrentItem.LocalScale = current.StartScale;
                    LastTCurrentItem.LocalRotation = current.StartRotation;

                    IdleTimeMove = IdleTimeScale = 0;                               //Reset the Idle Time
                    IsSelectTransformAnimation = false;

                    OnItemFocused.Invoke(CurrentItem.gameObject);                   //Let everybody knows that  the item selected have been changed      
                    CurrentItem.OnFocused.Invoke();                                 //Invoke from the Item Event (On Focused) 
                    FrameCamera();
                }
            }
        }

        /// <summary>
        /// The Index of the last previous selected item
        /// </summary>
        public int PreviousIndex
        {
            get { return previousIndex; }
        }

        /// <summary>
        /// The LocalRotation of the World Equivalent
        /// </summary>
        protected Quaternion UseWorldRotation
        {
            get
            {
                return Quaternion.Inverse(transform.localRotation) * InitialTransform.LocalRotation;
            }
        }

        /// <summary>
        /// Reference for the MItem of the previous Item
        /// </summary>
        public MItem PreviousItem
        {
            get
            {
                if (previousIndex == -1 || previousIndex >= Items.Count)
                {
                    return null;
                }

                return Items[previousIndex];
            }
        }

        /// <summary>
        /// True if is on transition from one item to another. Also Invoke OnChanging Item
        /// </summary>
        protected bool IsChangingItem
        {
            get { return isChangingItem; }
            set
            {
                isChangingItem = value;
                OnIsChangingItem.Invoke(isChangingItem);
            }
        }

        /// <summary>
        ///True if is Swapping/Dragging from one item to another. Also Invoke OnChanging Item
        /// </summary>
        protected bool IsSwapping
        {
            get { return isSwapping; }
            set
            {
                isSwapping = value;
                OnIsChangingItem.Invoke(isSwapping);
            }
        }

        /// <summary>
        /// Main Input for the Selector True when the Input is Pressed
        /// </summary>
        protected bool MainInputPress
        {
            get
            {
                return Input.GetMouseButton(0);
            }
        }


        /// <summary>
        /// Main Input for the Selector True when the Input is Down
        /// </summary>
        protected bool MainInputDown
        {
            get
            {
                return Input.GetMouseButtonDown(0);
            }
        }

        /// <summary>
        /// Main Input for the Selector True when the Input is Released
        /// </summary>
        protected bool MainInputUp
        {
            get
            {
                return Input.GetMouseButtonUp(0);
            }
        }

        /// <summary>
        /// Set the Selector Controller Active or inactive
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
            set
            {
                isActive = value;

                //if (!isActive)                  //if by any chance there was an item restoring
                //{
                //    PreviousItem.IsRestoring = false;
                //    PreviousItem.RestoreTransform();
                //}
            }
        }

        /// <summary>
        /// Return true if is not Animating, Restoring or Swapping
        /// </summary>
        public bool ZeroMovement
        {
            get {
                return !isRestoring && !IsSwapping && !isAnimating;
            }
        }

        protected SelectorEditor S_Editor
        {
            get
            {
                if (s_Editor == null)
                {
                    s_Editor = GetComponent<SelectorEditor>();
                }
                return s_Editor;
            }
        }
        protected SelectorManager S_Manager
        {
            get
            {
                if (s_manager == null)
                {
                    s_manager = GetComponentInParent<SelectorManager>();
                }
                return s_manager;
            }
        }

        #endregion

        #region Reference Variables
        /// <summary>
        /// Editor Selector Reference
        /// </summary>
        private SelectorEditor s_Editor;
        private SelectorManager s_manager;

        /// <summary>
        /// The Camera for the Selector
        /// </summary>
        protected Camera S_Camera
        { get {return S_Editor.SelectorCamera; } }

        public int FocusedItemIndex
        {
            get
            {
                return focusedItemIndex;
            }

            set
            {
                focusedItemIndex = value;
            }
        }
        #endregion

        #region Editor Variables
        ////EDITOR VARIABLES
        [HideInInspector] public bool EditorShowEvents = true;
        [HideInInspector] public bool EditorIdleAnims = true;
        [HideInInspector] public bool EditorInput = true;
        [HideInInspector] public bool EditorAdvanced = true;
        #endregion
    }
}