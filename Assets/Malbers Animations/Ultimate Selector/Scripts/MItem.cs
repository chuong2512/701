﻿using UnityEngine;
using MalbersAnimations.Utilities;
using MalbersAnimations.Events;
using UnityEngine.Events;

namespace MalbersAnimations.Selector
{
    public class MItem : MonoBehaviour
    {
        #region Variables
        [TextArea]
        [SerializeField] private string description = "Description of this Item";   //Description of the Item
        [SerializeField] private bool locked = false;
        [SerializeField] private int value = 0;
        [SerializeField] private int amount = 1;

        public GameObject originalItem;
        public string CustomAnimation;


        [SerializeField] private Vector3 localPosition;              //The Start Local Position of the Item
        [SerializeField] private Quaternion localRotation;           //The Start Local Rotation of the Item
        [SerializeField] private Vector3 localScale;                 //The Start Local Scale    of the Item


        [SerializeField] private Vector3 customPosition;             //If using Custom, store the Custom Position
        [SerializeField] private Quaternion customRotation;          //If using Custom, store the Custom Rotation
        [SerializeField] private Vector3 customScale = Vector3.one;  //If using Custom, store the Custom Scale 

        private bool restorePos;
        [SerializeField] private string All_MaterialChangerIndex;   //Store All Material Changer Indexes if the Items have Material Changers
        [SerializeField] private string All_ActiveMeshIndex;        //Store All Material Changer Indexes if the Items have Material Changers


        private MaterialChanger matChanger;                         //Store if this item has the Material Changer Component Attach to it
        private ActiveMeshes activeMeshes;                          //Store if this item has the Active Meshes Component Attach to it

        private Material[] SoloMaterial;                            //If the Item does not have a Material Changer use this intsead for the Locking material


        public UnityEvent OnSelected = new UnityEvent();
        public UnityEvent OnFocused = new UnityEvent();

        public UnityEvent OnLocked = new UnityEvent();
        public UnityEvent OnUnlocked = new UnityEvent();
        public IntEvent OnAmountChanged = new IntEvent();

        #endregion

        #region Properties

        /// <summary>
        /// Returns if the Item is Locked, also Invoke Lock and Unlock Events
        /// </summary>
        public bool Locked
        {
            get { return locked; }
            set
            {
                if (locked != value)
                {
                    locked = value;
                    if (locked)
                    {
                        OnLocked.Invoke();
                    }
                    else
                    {
                        OnUnlocked.Invoke();
                        RemoveLockMaterial();
                    }
                }
            }
        }

        /// <summary>
        /// The description of each item to show on the UI
        /// </summary>
        public string ItemData
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// The value of the Item
        /// </summary>
        public int Value
        {
            get { return value; }
            set { this.value = value; }
        }

        /// <summary>
        /// How many of this Items you have left
        /// </summary>
        public int Amount
        {
            get { return amount; }
            set
            {
                amount = value;
                OnAmountChanged.Invoke(amount);
            }
        }

        /// <summary>
        /// The original GameObject of the item
        /// </summary>
        public GameObject OriginalItem
        {
            get { return originalItem; }
            set { originalItem = value; }
        }


        /// <summary>
        /// Initial Rotation of the Item
        /// </summary>
        public Quaternion StartRotation
        {
            get { return localRotation; }
            set { localRotation = value; }
        }

        /// <summary>
        /// Initial Position of the Item
        /// </summary>
        public Vector3 StartPosition
        {
            get { return localPosition; }
            set { localPosition = value; }
        }


        /// <summary>
        /// Initial Local Scale of the Item
        /// </summary>
        public Vector3 StartScale
        {
            get { return localScale; }
            set { localScale = value; }
        }

        #region Custom Selector TransformData

        public Quaternion CustomRotation
        {
            get { return customRotation; }
            set { customRotation = value; }
        }

        public Vector3 CustomPosition
        {
            get { return customPosition; }
            set { customPosition = value; }
        }

        public Vector3 CustomScale
        {
            get { return customScale; }
            set { customScale = value; }
        }
        #endregion

        /// <summary>
        /// Returns the Size of the collider of the item
        /// </summary>
        public Vector3 BoundingBox
        {
            get
            {
                Collider ItemCollider = GetComponentInChildren<Collider>();
                if (ItemCollider)
                {
                    return ItemCollider.bounds.size;
                }
                return Vector3.one;
            }
        }
        
        /// <summary>
        /// Used to check if the Item is On the RestoreAnimation
        /// </summary>
        public bool IsRestoring
        {
            get { return restorePos; }
            set { restorePos = value; }
        }

        /// <summary>
        /// Calculates the Center of the Item base on the Collider
        /// </summary>
        public Vector3 Center
        {
            get
            {
                Collider ItemCollider = GetComponentInChildren<Collider>();
                if (ItemCollider)
                {
                    return transform.localPosition - ItemCollider.bounds.center;
                }
                Collider2D ItemCollider2D = GetComponentInChildren<Collider2D>();

                if (ItemCollider2D)
                {
                    return transform.localPosition - ItemCollider2D.bounds.center;
                }

                return transform.localPosition;
            }
        }

        /// <summary>
        /// Returns the MaterialChanger attacthed to this Item. if the itemo does not have a material changer it will return null
        /// </summary>
        public MaterialChanger MatChanger
        {
            get
            {
                if (matChanger == null)
                {
                    matChanger = GetComponentInChildren<MaterialChanger>();
                }
                return matChanger;
            }

            set
            {
                matChanger = value;
            }
        }

        /// <summary>
        /// Returns the ActiveMesh attacthed to this Item
        /// </summary>
        public ActiveMeshes ActiveMesh
        {
            get
            {
                if (activeMeshes == null)
                {
                    activeMeshes = GetComponentInChildren<ActiveMeshes>();
                }
                return activeMeshes;
            }

            set
            {
                activeMeshes = value;
            }
        }
        #endregion

        void Awake()
        {
            StartPosition = transform.localPosition;
            StartRotation = transform.localRotation;
            StartScale = transform.localScale;

            if (ActiveMesh) All_ActiveMeshIndex = ActiveMesh.AllIndex;          //Save all the Index Meshes 


            if (MatChanger) All_MaterialChangerIndex = MatChanger.AllIndex;     //Save all the Index Materials 
            else
            {
                Renderer isRenderer = GetComponentInChildren<Renderer>();
                if (isRenderer) SoloMaterial = isRenderer.materials;            //Save the firstMaterial
            }
        }

        /// <summary>
        /// Set a Lock Material to the current item
        /// </summary>
        /// <param name="mat">if mat == null Will unlock the item</param>
        public void SetLockMaterial(Material mat)
        {
            if (MatChanger)                                     //Check if is there a Material Changer component
            {
                if (mat)                                        //if they have a lock material set it to the item
                {
                    MatChanger.SetAllMaterials(mat);
                }
                else                                            //If not reset to the first material on the set and Unlock the material
                {
                    locked = false;
                    MatChanger.AllIndex = All_MaterialChangerIndex;
                }
            }
            else                                                       //If the Items does NOT have Material Changer
            {
                Renderer RItem = GetComponentInChildren<Renderer>();
                if (!RItem) return;

                if (mat)                                        //if they have a lock material set it to the item
                {
                    RItem.material = mat;
                }
                else                                            //If not reset to the first material on the set and Unlock the material
                {
                    locked = false;
                    RItem.materials = SoloMaterial;
                }
            }

            if (ActiveMesh)
            {
                ActiveMesh.AllIndex = All_ActiveMeshIndex;
            }
        }

        /// <summary>
        /// Unlocks the Item Remove the Lock Material and Set Locked to false
        /// </summary>
        public virtual void RemoveLockMaterial()
        {
            SetLockMaterial(null);      //Unlock the Item
        }


        /// <summary>
        /// Restore the Initial Transform of the Item;
        /// </summary>
        public virtual void RestoreInitialTransform()
        {
            transform.localPosition = StartPosition;
            transform.localRotation = StartRotation;
            transform.localScale = StartScale;
        }

        #region Material Changer and Active mesh Methods


        /// <summary>
        /// Change all Material List on this game object the next/before material
        /// </summary>
        /// <param name="Next">true: next, false: before</param>
        public void SetAllMaterials(bool Next = true)
        {
            if (MatChanger && !Locked)
            {
                MatChanger.SetAllMaterials(Next);
                All_MaterialChangerIndex = MatChanger.AllIndex;     //Update the Indexes
            }
        }

        /// <summary>
        /// Change  Material Item by its index  to the next material
        /// </summary>
        public void SetMaterial(int Index)
        {
            SetMaterial(Index, true);
        }

        /// <summary>
        /// Change  Material Item by its index  to the next/before material
        /// </summary>
        public void SetMaterial(int Index, bool next)
        {
            if (MatChanger && !Locked)
            {
                MatChanger.SetMaterial(Index,next);
                All_MaterialChangerIndex = MatChanger.AllIndex;     //Update the Indexes
            }
        }

        /// <summary>
        ///  Set on the Material Changer the Next Material Item using a Name
        /// </summary>
        public void SetMaterial(string Name)
        {
            SetMaterial(Name, true);
        }

        /// <summary>
        ///  Set on the Material Changer the Next or Before Material Item using a Name
        /// </summary>
        public void SetMaterial(string Name, bool Next)
        {
            if (MatChanger && !Locked)
            {
                MatChanger.SetMaterial(Name, Next);
                All_MaterialChangerIndex = MatChanger.AllIndex;     //Update the Indexes
            }
        }

        /// <summary>
        /// Change the Active Mesh Slot by its Index to the next/before Mesh Item.
        /// </summary>
        /// <param name="index">Index of the Mesh List</param>
        /// <param name="next">true: next, false: before</param>
        public virtual void ChangeMesh(int index, bool next = true)
        {
            if (ActiveMesh && !Locked)
            {
                ActiveMesh.ChangeMesh(index, next);
                All_ActiveMeshIndex = ActiveMesh.AllIndex;
            }
        }

        /// <summary>
        /// Change All Active Meshes the next/before Mesh Item.
        /// </summary>
        /// <param name="next">true: next, false: before</param>
        public virtual void ChangeMesh(bool next)
        {
            if (ActiveMesh && !Locked)
            {
                ActiveMesh.ChangeMesh(next);
                All_ActiveMeshIndex = ActiveMesh.AllIndex;
            }
        }

        /// <summary>
        /// Change the Active Mesh Slot by its Name to the nextMesh Item.
        /// </summary>
        public virtual void ChangeMesh(string name)
        {
            ChangeMesh(name, true);
        }

        /// <summary>
        /// Change the Active Mesh Slot by its Name to the next/before Mesh Item.
        /// </summary>
        /// <param name="name">Name of the Active</param>
        /// <param name="next">true: next, false: before</param>
        public virtual void ChangeMesh(string name, bool next)
        {
            if (ActiveMesh && !Locked)
            {
                ActiveMesh.ChangeMesh(name, next);
                All_ActiveMeshIndex = ActiveMesh.AllIndex;
            }
        }
        #endregion

       [HideInInspector] public bool EditorShowEvents;
    }
}