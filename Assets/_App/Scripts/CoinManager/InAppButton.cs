using System;
using UnityEngine;
using UnityEngine.UI;

namespace _App.Scripts.CoinManager
{
    public class InAppButton : MonoBehaviour
    {
        public int price = 1;
        private Button _button;

        private void Start()
        {
            _button = GetComponent<Button>();

            _button.onClick.AddListener(OnClickButton);
        }

        private void OnClickButton()
        {
            if (GameDataManager.Instance.playerData.intDiamond >= price)
            {
                GameDataManager.Instance.playerData.SubDiamond(price);
                //todo:
            }
        }
    }
}