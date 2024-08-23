using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
using MalbersAnimations.Utilities;
using MalbersAnimations.Events;
using System;

namespace MalbersAnimations.Selector
{
    //──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // LOGIC
    //──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    public partial class SelectorController
    {
        void Awake()
        {
            if (S_Editor && S_Manager) { }                                                   //Store the Selector Editor
            S_Editor.enabled = false;                                           //Disable Circle EditorScript
            UILayer = LayerMask.GetMask("UI");
            InitialTransform.StoreTransform(transform);                         //Store the initial transform of the selector;
            if (S_Camera) InitialPosCam = S_Camera.transform.localPosition;     //Store the Camera Initial Local Position
            CurrentEventData = new PointerEventData(EventSystem.current);       //Store the Current Event Data

            if (S_Manager.Data) FocusedItemIndex = S_Manager.Data.Save.FocusedItem;          //UPDATE THE DATA WITH THE FOCUSED ITEM

            LastDrag = DragSpeed;                                               //StoretheDragValue

            ResetController();
        }

        //──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        void OnEnable()
        {
            isEnabling = true;                                              //Set Enable to true to avoid Restoring and Swapping

            IndexSelected = FocusedItemIndex;                             //Set the Index and current Item to the Focused Item
            CheckFirstItemFocus();
        }
        //──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        void OnDisable()
        {
            foreach (var item in Items)
                item.RestoreInitialTransform();                             //Restore all the Items to its original Location

            StopAllCoroutines();
            isRestoring = isSwapping = isAnimating = false;
        }

        //──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Initialize the Selector for the first time
        /// </summary>
        public virtual void ResetController()
        {
            StopAllCoroutines();
            AnimateSelectionC = IAnimateSelection();                            //Store the courutine SelectionAnimation

            IndexSelected = FocusedItemIndex;                                   //Set the Index and current Item to the Focused Item
            CheckFirstItemFocus();
            InitialTransform.RestoreLocalTransform(transform);

            CurrentEventData = new PointerEventData(EventSystem.current);       //Store the Current Event Data
        }
        //──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the position and Rotation the Selector to the Focused Item. No Animations Involved
        /// </summary>
        public virtual  void CheckFirstItemFocus()
        {
            //Move the selector to the first item you want to be focus On
            if (AnimateSelection)
            {
                if (S_Editor.SelectorType == SelectorType.Radial)
                {
                    Vector3 RotationVector = RadialVector * S_Editor.Angle * IndexSelected;
                    transform.localRotation = Quaternion.Euler(RotationVector) * InitialTransform.LocalRotation; //SelectorInitRot;
                    CheckForWorldRotation();
                }
                else
                {
                    if (CurrentItem) transform.localPosition = InitialTransform.LocalPosition - CurrentItem.transform.localPosition;
                }
            }
        }

        void Update()
        {
            if (!IsActive) return;

            SelectionAction();
            AnimateIdle();

            if (Submit.GetInput)
            {
                IsSelectTransformAnimation = true;
                OnClickOnItem.Invoke(CurrentItem.gameObject);           //Invoke OnClick Event on the Controller
                CurrentItem.OnSelected.Invoke();                        //Invoke OnSelected Event on the Item
            }

            if (S_Editor.SelectorType != SelectorType.Grid)
            {
                if (ChangeLeft.GetInput || ChangeDown.GetInput) SelectNextItem(false);      //Change to Previous item
                if (ChangeRight.GetInput || ChangeUp.GetInput) SelectNextItem(true);        //Change to Next item
            }
            else
            {
                #region GRID Selection
                int NextItemIndex = IndexSelected;

                int CurrentRow = IndexSelected / S_Editor.Grid;


                if (ChangeLeft.GetInput)                                        //Change Left
                {
                    NextItemIndex--;

                    NextItemIndex = (NextItemIndex % S_Editor.Grid) + (CurrentRow * S_Editor.Grid);

                    if (NextItemIndex >= Items.Count) NextItemIndex = Items.Count - 1;
                    if (NextItemIndex < 0) NextItemIndex = S_Editor.Grid - 1;

                }
                else if (ChangeRight.GetInput)                                  //Change Right
                {
                    NextItemIndex++;

                    if (NextItemIndex == Items.Count) NextItemIndex = GridTotal;

                    NextItemIndex = NextItemIndex % S_Editor.Grid + (CurrentRow * S_Editor.Grid);
                }
                else if (ChangeUp.GetInput)                                     //Change UP
                {
                    NextItemIndex += S_Editor.Grid;

                    if (NextItemIndex >= Items.Count) NextItemIndex = NextItemIndex % S_Editor.Grid;
                }
                else if (ChangeDown.GetInput)
                {
                    NextItemIndex -= S_Editor.Grid;

                    if (NextItemIndex < 0)
                    {
                        if (GridTotal + NextItemIndex >= Items.Count)
                        {
                            NextItemIndex -= S_Editor.Grid;
                        }
                        NextItemIndex = GridTotal + NextItemIndex;
                    }
                }

                if (ChangeLeft.GetInput || ChangeDown.GetInput ||
                ChangeRight.GetInput || ChangeUp.GetInput)
                {
                    SelectNextItem(NextItemIndex);        //Change to Previous item
                }
                #endregion
            }
        }

        /// <summary>
        /// Idle Animation when the item is selected
        /// </summary>
        protected virtual void AnimateIdle()
        {
            if (IndexSelected == -1) return;                    //Skip if the Selection is clear
            if (!MoveIdle && !RotateIdle && !ScaleIdle) return; //Skip if there's no Idle Animation
            if (CurrentItem == null) return;

            if (!IsChangingItem && !isAnimating && !IsSwapping) //
            {
                IdleTimeMove += Time.deltaTime;
                IsSelectTransformAnimation = true;
                if (MoveIdle && MoveIdleAnim)
                {
                    IdleTimeMove = IdleTimeMove % MoveIdleAnim.time;
                    CurrentItem.transform.localPosition =
                       Vector3.LerpUnclamped(LastTCurrentItem.LocalPosition,
                       LastTCurrentItem.LocalPosition + transform.InverseTransformDirection(CurrentItem.transform.TransformDirection(MoveIdleAnim.Position)),
                       MoveIdleAnim.PosCurve.Evaluate(IdleTimeMove / MoveIdleAnim.time));
                }

                if (RotateIdle)
                {
                    Renderer mesh = CurrentItem.GetComponentInChildren<Renderer>();

                    CurrentItem.transform.RotateAround
                        (mesh ? mesh.bounds.center : CurrentItem.transform.position, transform.TransformDirection(TurnTableVector), ItemRotationSpeed * Time.deltaTime * 10); //rotate by center
                }

                if (ScaleIdle && ScaleIdleAnim)
                {
                    IdleTimeScale += Time.deltaTime;
                    IdleTimeScale = IdleTimeScale % ScaleIdleAnim.time;
                    Vector3 FinalScale = Vector3.Scale(LastTCurrentItem.LocalScale, ScaleIdleAnim.Scale);
                    CurrentItem.transform.localScale = Vector3.LerpUnclamped(LastTCurrentItem.LocalScale, FinalScale, ScaleIdleAnim.ScaleCurve.Evaluate(IdleTimeScale / ScaleIdleAnim.time));
                }
            }
        }

        /// <summary>
        /// Move the ItemSelector to the Next Item
        /// </summary>
        protected void Animate_to_Next_Item()
        {
            if (isEnabling)
            {
                isEnabling = false;
                return;                     //Don't Animate Selection or Restore Item if is being enabled
            }


            if (AnimateSelection && IndexSelected != -1)
            {
                if (AnimateSelectionC != null) StopCoroutine(AnimateSelectionC);  //Stop if is in other transition
                AnimateSelectionC = IAnimateSelection();
                StartCoroutine(AnimateSelectionC);
            }
            if (PreviousItem && !PreviousItem.IsRestoring)
                StartCoroutine(RestoreTransformAnimation(PreviousItem));        //If is not restoring the Previous Item Restore it
        }

        /// <summary>
        /// Animate Selector Between items
        /// </summary>
        protected IEnumerator IAnimateSelection()
        {
            IsChangingItem = true;              //Set that is Changing  an item.

         
            yield return null;


            float elapsedTime = 0;
            float normalized = 0;

            DeltaTransform CurrentT = new DeltaTransform();

            CurrentT.StoreTransform(transform);

            Vector3 NextPosition =
                InitialTransform.LocalPosition - transform.InverseTransformDirection(transform.TransformDirection(CurrentItem.transform.localPosition));

            Vector3 RotationVector = RadialVector * S_Editor.Angle * IndexSelected;

            Quaternion NextRotation = Quaternion.Euler(RotationVector) * InitialTransform.LocalRotation;


            if (SelectionTime > 0)
            {
                while (elapsedTime <= SelectionTime)
                {
                    normalized = SelectionCurve.Evaluate(elapsedTime / SelectionTime);

                    if (S_Editor.SelectorType != SelectorType.Radial)
                        transform.localPosition = Vector3.LerpUnclamped(CurrentT.LocalPosition, NextPosition, normalized);

                    if (S_Editor.SelectorType == SelectorType.Radial)
                    {
                        transform.localRotation = Quaternion.SlerpUnclamped(CurrentT.LocalRotation, NextRotation, normalized);

                        CheckForWorldRotation();
                    }

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }

            if (S_Editor.SelectorType == SelectorType.Radial)
            {
                transform.localRotation = NextRotation;
                CheckForWorldRotation();

            }
            else
            {
                transform.localPosition = NextPosition;
            }
            IsChangingItem = false;
            IdleTimeMove = IdleTimeScale = 0;           //Make sure to reset the Idle counting animations to zero when finishing dismounting
        }

        /// <summary>
        /// Plays the Transform Animations for the Selected item 
        /// </summary>
        protected IEnumerator PlayTransformAnimation(MItem item, TransformAnimation animTransform)
        {
            if (item != null && animTransform != null)
            {
                isAnimating = true;

                if (animTransform.delay != 0) yield return new WaitForSeconds(animTransform.delay);         //Wait for the Delay     

                float elapsedTime = 0;

                Vector3 CurrentPos = item.transform.localPosition;                                          //Store the Current Position Rotation and Scale
                Quaternion CurrentRot = item.transform.localRotation;
                Vector3 CurrentScale = item.transform.localScale;

                while ((animTransform.time > 0) && (elapsedTime <= animTransform.time))
                {

                    float resultPos = animTransform.PosCurve.Evaluate(elapsedTime / animTransform.time);               //Evaluation of the Pos curve
                    float resultRot = animTransform.RotCurve.Evaluate(elapsedTime / animTransform.time);               //Evaluation of the Rot curve
                    float resultSca = animTransform.ScaleCurve.Evaluate(elapsedTime / animTransform.time);               //Evaluation of the Scale curve

                    if (animTransform.UsePosition)
                    {
                        item.transform.localRotation = !S_Editor.UseWorld ? item.StartRotation : UseWorldRotation * item.StartRotation;                    //Reset the Rotation for the rotation animations
                        item.transform.localPosition =
                        Vector3.LerpUnclamped(CurrentPos,
                        item.StartPosition + transform.InverseTransformDirection(item.transform.TransformDirection(animTransform.Position)),
                        resultPos);
                    }

                    if (animTransform.UseRotation)
                    {
                        item.transform.localRotation = item.StartRotation * Quaternion.Euler(animTransform.Rotation * resultRot);
                        if (S_Editor.UseWorld) item.transform.localRotation = UseWorldRotation * item.transform.localRotation;
                    }

                    if (animTransform.UseScale)
                        item.transform.localScale = Vector3.LerpUnclamped(CurrentScale, Vector3.Scale(item.StartScale, animTransform.Scale), resultSca);

                    elapsedTime += Time.deltaTime;

                    yield return null;
                }
                EndAnimationTransform(item, animTransform);
            }
            isAnimating = false;
            yield return null;
        }

        /// <summary>
        /// Set an item tranform values on the Final State of a Transform Animation
        /// </summary>
        protected virtual void EndAnimationTransform(MItem item, TransformAnimation animTransform)
        {
           //if (item == PreviousItem) return; ERROR OF THE ESA TALLA GLitch
             
            //Set to the Last Value on the Curve POS ROT and SCALE
            if (animTransform.UsePosition)
                item.transform.localPosition = item.StartPosition +
                    transform.InverseTransformDirection(item.transform.TransformDirection(animTransform.Position * animTransform.PosCurve.Evaluate(1)));


            if (animTransform.UseRotation)
            {
                if (S_Editor.UseWorld)
                    item.transform.localRotation = UseWorldRotation * item.StartRotation * Quaternion.Euler(animTransform.Rotation * animTransform.RotCurve.Evaluate(1));
                else
                    item.transform.localRotation = item.StartRotation * Quaternion.Euler(animTransform.Rotation * animTransform.RotCurve.Evaluate(1));
            }

            if (animTransform.UseScale) item.transform.localScale = Vector3.LerpUnclamped(item.StartScale, Vector3.Scale(item.StartScale, animTransform.Scale), animTransform.ScaleCurve.Evaluate(1)); ;

            isAnimating = false;

            if (!IsSelectTransformAnimation)                                               //Don't Store if is doing an Idle Animation //Para que no se escale pa abajo o siga subiendo foreva
            {
                LastTCurrentItem.LocalPosition = item.transform.localPosition; //Store the last local position after the animation 
                LastTCurrentItem.LocalScale = item.transform.localScale;       //Store the last local Scale after the animation
            }
        }

        /// <summary>
        /// Restore the Transform of the Item to original pos/rot/scale on the list
        /// </summary>
        protected IEnumerator RestoreTransformAnimation(MItem item)
        {
            if (item)
            {
                float elapsedTime = 0;
                item.IsRestoring = true;                                        //Set that this item is On the Restoring State

                Vector3 CurrentPos = item.transform.localPosition;              //Store the Current Position
                Quaternion CurrentRot = item.transform.localRotation;           //Store the Current Rotation
                Vector3 CurrentScale = item.transform.localScale;               //Store the Current Scale

                while (elapsedTime <= RestoreTime && RestoreTime > 0)
                {
                    if (item == null) goto NoItemFound_RestoreAnimation;              //In case the item is removed.

                    item.transform.localPosition = Vector3.Lerp(CurrentPos, item.StartPosition, elapsedTime / RestoreTime);

                    if (S_Editor.SelectorType != SelectorType.Radial)
                    {
                        item.transform.localRotation =
                            Quaternion.Slerp(CurrentRot, item.StartRotation, elapsedTime / RestoreTime);
                    }
                    else
                    {
                        item.transform.localRotation =
                            Quaternion.Slerp(CurrentRot,
                            S_Editor.UseWorld ? UseWorldRotation * item.StartRotation : item.StartRotation, (LastAnimation && !LastAnimation.UseRotation && S_Editor.UseWorld) ? 1 : elapsedTime / RestoreTime);
                    }

                    item.transform.localScale = Vector3.Lerp(CurrentScale, item.StartScale, elapsedTime / RestoreTime);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }


                item.transform.localPosition = item.StartPosition;
                item.transform.localRotation = S_Editor.UseWorld ? UseWorldRotation * item.StartRotation : item.StartRotation;
                item.transform.localScale = item.StartScale;
                item.IsRestoring = false;


                NoItemFound_RestoreAnimation:

                isRestoring = false;
            }
        }

      

        /// <summary>
        ///Drag/Swipe with Mouse/Touch and Action by Click/Tap
        /// </summary>
        protected virtual void SelectionAction()
        {
            CurrentEventData.position = Input.mousePosition;

            #region Hover && SoloSelection
            if (Hover && SoloSelection)
            {
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(CurrentEventData, results); //HERE IS THE MODIfICATION

                if (results.Count > 0)
                {
                    GameObject HitObject = results[0].gameObject;

                    if (HitObject.GetComponentInParent<SelectorManager>() != S_Manager) return; //Its not the same selector Skip All

                    MItem HitObjectItem = HitObject.GetComponentInParent<MItem>();

                    if (HitItem != HitObjectItem)
                    {
                        HitItem = HitObjectItem;

                        if (HitItem)
                        {
                            int Next = Items.FindIndex(item => item == HitItem);
                            if (IndexSelected != Next)
                            {
                                IndexSelected = Next;

                                if (PreviousItem && !PreviousItem.IsRestoring)
                                    StartCoroutine(RestoreTransformAnimation(PreviousItem)); //If is not restoring the Previous Item Restore it
                            }
                            if (MainInputDown)
                            {
                                IsSelectTransformAnimation = true;
                                OnClickOnItem.Invoke(CurrentItem.gameObject);           //Invoke OnClick Event on the Controller
                                CurrentItem.OnSelected.Invoke();                        //Invoke OnSelected Event on the Item
                            }
                        }
                    }
                }
                //return;  //Skip everything else
            }
            #endregion
            if (MainInputDown)                                          //The moment the mouse/touch start the click/touch
            {
                MouseStartPosition = Input.mousePosition;               //Store the mouse/touch start position      
                DeltaSelectorT.StoreTransform(transform);               //Store the Current Selector Transform Data

                IsSwapping = false;                                     //Set swapping to false     
            }
            if (SoloSelection)
            {
                DeltaMousePos = Vector2.zero;
                goto ReleaseMouse;
            }

            #region DRAG/SWIPE on MOUSE/TOUCH

            if (MainInputPress)                                                       //if we are still touching means that we are dragging/swiping
            {
                DeltaMousePos = MouseStartPosition - Input.mousePosition;             //Store the amount of distance travelled sice the first click/touch

                if (DeltaMousePos.magnitude > Threshold && !IsSwapping && DragSpeed != 0 && !SoloSelection)   //Find out if is a Swipe/Drag 
                {
                    IsSwapping = true;                                                  //Is a Drag/Swipe!!  

                    StartCoroutine(RestoreTransformAnimation(CurrentItem));               //Restore the Current Item to their last pos    
                }

                #region Dragin and Swapping if Animate Selection is Active

                if (AnimateSelection && IsSwapping && DragSpeed != 0)                                   //Just Drag if Animate Selection is active
                {
                    #region Drag Radial
                    if (S_Editor.SelectorType == SelectorType.Radial)                 //If is a Radial Selector-----------------------------------------------------------
                    {
                        DragDistance = dragHorizontal ? DeltaMousePos.x : DeltaMousePos.y;

                        DragDistance = DragDistance * Time.fixedDeltaTime * DragSpeed;

                        //Rotate while drag
                        switch (S_Editor.RadialAxis)
                        {
                            case RadialAxis.Up:
                                transform.localEulerAngles = DeltaSelectorT.LocalEulerAngles + new Vector3(0, DragDistance, 0);             //Rotate  on Y axis the Selector
                                break;

                            case RadialAxis.Right:
                                transform.localEulerAngles = DeltaSelectorT.LocalEulerAngles + new Vector3(DragDistance, 0, 0);             //Rotate on X axis the Selector This one has probems with gimbal
                                break;

                            case RadialAxis.Forward:
                                transform.localEulerAngles = DeltaSelectorT.LocalEulerAngles + new Vector3(0, 0, DragDistance);              //Rotate  on Z axis the Selector
                                break;
                            default:
                                break;
                        }

                        if (IsSwapping) CheckForWorldRotation();    //Align Items to the World if Use World is Active
                    }
                    #endregion
                    #region Drag Linear
                    else if (S_Editor.SelectorType == SelectorType.Linear)                                  //If is a Linear Selector-----------------------------------------------------------
                    {
                        float magnitude = dragHorizontal ? DeltaMousePos.x : DeltaMousePos.y;

                        DragDistance = -(DeltaSelectorT.LocalPosition - InitialTransform.LocalPosition).magnitude + (-magnitude * DragSpeed * 0.002f);

                        float maxDistance = -distance * (Items.Count - 1);
                        DragDistance = Mathf.Clamp(DragDistance, maxDistance, 0);

                        transform.localPosition = InitialTransform.LocalPosition + S_Editor.LinearVector * DragDistance;

                    }
                    #endregion
                    #region Drag Grid
                    else if (S_Editor.SelectorType == SelectorType.Grid)
                    {
                        transform.localPosition = GridVector();
                    }
                    #endregion
                }
                #endregion
            }
            #endregion

            #region Release Mouse Click/Touch

            ReleaseMouse:

            if (MainInputUp)            //if is released the Click/Touch───────────────────────────────────────────────────────────────────────────────────────
            {
                IsSwapping = false;                                                                                //Set Swapping to false

                if (DeltaMousePos.magnitude <= Threshold)                           //if it was a CLICK/TAP and it was not on top a IU Element
                {
                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(CurrentEventData, results);

                    if (results.Count > 0)
                    {
                        MItem HitItem = results[0].gameObject.GetComponentInParent<MItem>();

                        if (HitItem)
                        {
                            if (HitItem.GetComponentInParent<SelectorManager>() != S_Manager) return;       //Its not the same Selector

                            int Next = Items.FindIndex(item => item == HitItem);
                            
                            if (IndexSelected == Next)                                                      //If we Click/Touch the Selected item Invoke |ON CLICK|
                            {
                                IsSelectTransformAnimation = true;
                                OnClickOnItem.Invoke(current.gameObject);                   //Invoke OnClick Event on the Controller
                                current.OnSelected.Invoke();                                //Invoke OnSelected Event on the Item
                            }
                            else                                                            //If another visible item was touch change to that one;
                            {
                                if (ClickToFocus) IndexSelected = Next;                     //Focus on the Next Item if lcick to focus is Active
                            }
                        }

                    }
                    else if (ChangeOnEmptySpace)               // if we did not hit any item the it was a click/touch on Left or Right of the Screen
                    {
                        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);

                        if ((Input.mousePosition.x < center.x && dragHorizontal) || ((Input.mousePosition.y < center.y && !dragHorizontal)))
                        {
                            SelectNextItem(false);
                        }
                        else
                        {
                            SelectNextItem(true);
                        }
                    }
                }

                else if (DragSpeed != 0) //Else if it was a swipe and swipe is active, average to the nearest item on the camera view
                {
                    if (S_Editor.SelectorType == SelectorType.Radial)           //Circular Selector---------------------------
                    {
                        int Next = Mathf.RoundToInt(DragDistance / S_Editor.Angle);

                        if (S_Editor.RadialAxis != RadialAxis.Up) Next = -Next;

                        if (Next < 0) Next = Items.Count + (Next % Items.Count);

                        IndexSelected += Next; // ????????????????????
                    }
                    else if (S_Editor.SelectorType == SelectorType.Linear)      //Linear Selector---------------------------
                    {
                        IndexSelected = Mathf.RoundToInt(Mathf.Abs(DragDistance) / distance) % Items.Count;
                    }
                    else if (S_Editor.SelectorType == SelectorType.Grid)        //Grid Selector---------------------------
                    {
                        Vector3 DragMouse = GridVector() - InitialTransform.LocalPosition; //Get the Position on the Grid

                        int Next = 0;
                        float mag = float.MaxValue;

                        for (int i = 0; i < Items.Count; i++)
                        {
                            float currentmag = Mathf.Abs((DragMouse + Items[i].transform.localPosition).magnitude); //Find the nearest item.

                            if (currentmag < mag)
                            {
                                Next = i;
                                mag = currentmag;
                            }
                        }
                        IndexSelected = Next;       //Set the Focused Item to the next
                    }
                    DragDistance = 0;               //Reset Drag Distance
                }
            }
            #endregion
        }

        /// <summary>
        /// Used to Clamp the Grid Size
        /// </summary>
        internal Vector3 GridVector()
        {
            Vector3 DragMouse = (Vector3)(DeltaMousePos * (-DragSpeed * 0.002f));

            DragMouse = DeltaSelectorT.LocalPosition + DragMouse;

            DragMouse = new Vector3(Mathf.Clamp(DragMouse.x,
                InitialTransform.LocalPosition.x - (S_Editor.GridWidth * (S_Editor.Grid - 1)),
                InitialTransform.LocalPosition.x), Mathf.Clamp(DragMouse.y,
            InitialTransform.LocalPosition.y - (S_Editor.GridHeight * (Rows - 1)),
            InitialTransform.LocalPosition.y));

            return DragMouse;
        }


        /// <summary>
        /// Align the Items to the World Orientation if UseWorld is Active
        /// </summary>
        internal void CheckForWorldRotation()
        {
            if (S_Editor.UseWorld)
            {
                foreach (var item in Items)
                {
                    if (item.IsRestoring) continue;
                    item.transform.localRotation = UseWorldRotation * item.StartRotation;
                }
            }
        }

        /// <summary>
        /// Moves camera near or far depending the  size of the selected item.
        /// </summary>
        protected virtual void FrameCamera()
        {
            if (S_Camera == null || S_Editor.WorldCamera || IndexSelected == -1 || !frame_Camera) return; //Skip if these Settings happen
            StartCoroutine(FrameCameraAnim());
        }

        IEnumerator FrameCameraAnim()
        {
            Vector3 currentcampos = S_Camera.transform.localPosition;
            Vector3 FixPos = InitialPosCam + (S_Camera.transform.forward * (Items[0].BoundingBox.magnitude - CurrentItem.BoundingBox.magnitude) * frame_Multiplier);
            float elapsedTime = 0;
            float normalized = 0;
            while (elapsedTime <= SelectionTime && SelectionTime > 0)
            {
                normalized = SelectionCurve.Evaluate(elapsedTime / SelectionTime);
                S_Camera.transform.localPosition = Vector3.Lerp(currentcampos, FixPos, normalized);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            S_Camera.transform.localPosition = FixPos;
            yield return null;
        }
    }
}