using UnityEngine;
using MalbersAnimations.Utilities;
using System.Collections.Generic;

namespace MalbersAnimations.Selector
{
    [System.Serializable]
    public class SelectorSave
    {
        public int Coins;
        public int RestoreCoins;
        public int FocusedItem;


        public List<bool> Locked;
        public List<bool> RestoreLocked;

        public List<int> ItemsAmount;
        public List<int> RestoreItemsAmount;

        public bool UseMaterialChanger;
        public List<string> MaterialIndex;
        public List<string> RestoreMaterialIndex;

      
        public bool UseActiveMesh;
        public List<string> ActiveMeshIndex;
        public List<string> RestoreActiveMeshIndex;
    }

    [CreateAssetMenu(menuName = "Malbers Animations/Ultimate Selector/Selector Data")]
    public class SelectorData : ScriptableObject
    {

        public bool usePlayerPref = true;
        public string PlayerPrefKey = "SaveSelectorData";

        [Header("Data to Save")]
        public SelectorSave Save;

        /// <summary>
        /// Saves the Initial Data of a Selector Manager
        /// </summary>
        public virtual void SetDefaultData(SelectorManager manager)
        {
            Save.RestoreCoins = Save.Coins;                                       //Save the RestoreCoins to the Current Coins

            Save.RestoreLocked = new List<bool>();
            Save.Locked = new List<bool>();

            Save.RestoreItemsAmount = new List<int>();
            Save.ItemsAmount = new List<int>();

            Save.UseMaterialChanger = Save.UseActiveMesh =  false;
            Save.FocusedItem = manager.Controller.FocusedItemIndex;

            Save.ActiveMeshIndex = null;
            Save.RestoreActiveMeshIndex = null;
            Save.MaterialIndex = null;
            Save.RestoreMaterialIndex = null;

            if (manager.Editor.Items != null && manager.Editor.Items.Count > 1)         //Check for Material Changer and Active Mesh
            {
                if (manager.Editor.Items[0].MatChanger != null)
                {
                    Save.UseMaterialChanger = true;
                    Save.RestoreMaterialIndex = new List<string>();
                    Save.MaterialIndex = new List<string>();
                }

                if (manager.Editor.Items[0].ActiveMesh != null)
                {
                    Save.UseActiveMesh = true;
                    Save.RestoreActiveMeshIndex = new List<string>();
                    Save.ActiveMeshIndex = new List<string>();
                }
            }

            for (int i = 0; i < manager.Editor.Items.Count; i++)                
            {
                Save.RestoreLocked.Add(manager.Editor.Items[i].Locked);
                Save.RestoreItemsAmount.Add(manager.Editor.Items[i].Amount);

                if (Save.UseMaterialChanger && manager.Editor.Items[i].MatChanger)
                    Save.RestoreMaterialIndex.Add(manager.Editor.Items[i].MatChanger.AllIndex);

                if (Save.UseActiveMesh && manager.Editor.Items[i].ActiveMesh)
                    Save.RestoreActiveMeshIndex.Add(manager.Editor.Items[i].ActiveMesh.AllIndex);

            }

            Update_Current_Data_from_Restore();


            if (usePlayerPref)
            {
                if (PlayerPrefs.HasKey(PlayerPrefKey)) PlayerPrefs.DeleteKey(PlayerPrefKey);

                PlayerPrefs.SetString(PlayerPrefKey, MalbersTools.Serialize<SelectorSave>(Save));
                Debug.Log("Default Data saved on PlayerPref using the key: '" + PlayerPrefKey + "'\n" + MalbersTools.Serialize<SelectorSave>(Save));
            }



#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Restore the items values from the Restore Data
        /// </summary>
        public virtual void RestoreData(SelectorManager manager)
        {
            if (usePlayerPref && PlayerPrefs.HasKey(PlayerPrefKey))
            {
                Save = MalbersTools.Deserialize<SelectorSave>(PlayerPrefs.GetString(PlayerPrefKey));
            }


            Update_Current_Data_from_Restore();

            for (int i = 0; i < manager.Editor.Items.Count; i++)            //Save the Locked Elements
            {
                manager.Editor.Items[i].Locked = Save.RestoreLocked[i];
                manager.Editor.Items[i].Amount = Save.RestoreItemsAmount[i];


                if (Save.UseMaterialChanger && manager.Editor.Items[i].MatChanger)
                    manager.Editor.Items[i].MatChanger.AllIndex = Save.RestoreMaterialIndex[i];

                if (Save.UseActiveMesh && manager.Editor.Items[i].ActiveMesh)
                    manager.Editor.Items[i].ActiveMesh.AllIndex = Save.RestoreActiveMeshIndex[i];
            }

           

            if (usePlayerPref)
            {
                if (PlayerPrefs.HasKey(PlayerPrefKey)) PlayerPrefs.DeleteKey(PlayerPrefKey);

                PlayerPrefs.SetString(PlayerPrefKey, MalbersTools.Serialize<SelectorSave>(Save));
            }

            UpdateItems(manager);                                       //Update the Items from the Data

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);

#endif
        }



        private void Update_Current_Data_from_Restore()
        {
            Save.Coins = Save.RestoreCoins;

            Save.Locked = new List<bool>(Save.RestoreLocked);
            Save.ItemsAmount = new List<int>(Save.RestoreItemsAmount);

            if (Save.UseMaterialChanger) Save.MaterialIndex = new List<string>(Save.RestoreMaterialIndex);
            if (Save.UseActiveMesh) Save.ActiveMeshIndex = new List<string>(Save.RestoreActiveMeshIndex);
        }


        private void UpdateData_from_Restore1(int i)
        {
            Save.Locked.Add(Save.RestoreLocked[i]);
            Save.ItemsAmount.Add(Save.RestoreItemsAmount[i]);

            if (Save.UseMaterialChanger) Save.MaterialIndex.Add(Save.RestoreMaterialIndex[i]);
            if (Save.UseActiveMesh) Save.ActiveMeshIndex.Add(Save.RestoreActiveMeshIndex[i]);
        }



        /// <summary>
        /// Update the Data values with the Items values
        /// </summary>
        public virtual void UpdateData(SelectorManager manager)
        {
            for (int i = 0; i < manager.Editor.Items.Count; i++)            //Save the Locked Elements
            {
                Save.Locked[i] = manager.Editor.Items[i].Locked;
                Save.ItemsAmount[i] = manager.Editor.Items[i].Amount;

                if (Save.UseMaterialChanger && manager.Editor.Items[i].MatChanger)
                    Save.MaterialIndex[i] = manager.Editor.Items[i].MatChanger.AllIndex;

                if (Save.UseActiveMesh && manager.Editor.Items[i].ActiveMesh)
                    Save.ActiveMeshIndex[i] = manager.Editor.Items[i].ActiveMesh.AllIndex;
            }

            Save.FocusedItem = manager.Controller.FocusedItemIndex;

            if (usePlayerPref)
            {
               PlayerPrefs.SetString(PlayerPrefKey, MalbersTools.Serialize<SelectorSave>(Save));
            }
            

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }



        public virtual void LoadDataPlayerPref()
        {
            if (usePlayerPref && PlayerPrefs.HasKey(PlayerPrefKey))
            {
                Save = MalbersTools.Deserialize<SelectorSave>(PlayerPrefs.GetString(PlayerPrefKey));            //Get the Data from the Player Preff.
            }
        }

        /// <summary>
        /// Update the Items values with the Data Values.
        /// </summary>
        public virtual void UpdateItems(SelectorManager manager)
        {
            if (!manager.Editor) return;

            //if (manager.Editor.Items.Count != Save.Locked.Length)
            if (manager.Editor.Items.Count != Save.Locked.Count)
            {
                Debug.LogWarning("Please, on the Selector Manager Press 'Save initial Data'\nYou have add or remove items and the current Items ammount and the Items amount in the Data File are not the same");
                return;
            }

            for (int i = 0; i < manager.Editor.Items.Count; i++)            //Save the Locked Elements
            {
                manager.Editor.Items[i].Locked = Save.Locked[i];
                manager.Editor.Items[i].Amount = Save.ItemsAmount[i];

                if (Save.UseMaterialChanger && manager.Editor.Items[i].MatChanger)
                    manager.Editor.Items[i].MatChanger.AllIndex = Save.MaterialIndex[i];

                if (Save.UseActiveMesh && manager.Editor.Items[i].ActiveMesh)
                    manager.Editor.Items[i].ActiveMesh.AllIndex = Save.ActiveMeshIndex[i];
            }

            manager.Controller.FocusedItemIndex = Save.FocusedItem;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
