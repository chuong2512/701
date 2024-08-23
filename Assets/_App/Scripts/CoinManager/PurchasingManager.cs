using System.Collections;
using System.Collections.Generic;
using MalbersAnimations.Selector;
using UnityEngine;

public class PurchasingManager : MonoBehaviour
{
   public void OnPressDown(int i)
   {
      switch (i)
      {
         case 1:
            SelectorUI.Instance.Manager.Data.Save.Coins += 1;
             IAPManager.Instance.BuyProductID(IAPKey.PACK1);
            break;
         case 2:
            SelectorUI.Instance.Manager.Data.Save.Coins += 3;
            IAPManager.Instance.BuyProductID(IAPKey.PACK2);
            break;
         case 3:
            SelectorUI.Instance.Manager.Data.Save.Coins += 5;
            IAPManager.Instance.BuyProductID(IAPKey.PACK3);
            break;
         case 4:
            SelectorUI.Instance.Manager.Data.Save.Coins += 10;
            IAPManager.Instance.BuyProductID(IAPKey.PACK4);
            break;
      }
   }

   public void Sub(int i)
   {
      GameDataManager.Instance.playerData.SubDiamond(i);
   }
}
