using System.Collections.Generic;
using Firebase.Database;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class ShopManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Text CoinText;
    [SerializeField] Text MessageText;
    [SerializeField] Text MessageLogText;

    [Header("Log")]
    [SerializeField] int MaxLogLines = 8;

    [Header("Price")]
    [SerializeField] int DashBoosterPrice = 100;
    [SerializeField] int ShieldChipPrice = 150;
    [SerializeField] int PortalCorePrice = 200;

    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    string userKey;
    int coin;
    Dictionary<string, int> inventory = new Dictionary<string, int>();
    List<string> messageLogs = new List<string>();

    void Start()
    {
        reference = FirebaseDatabase.GetInstance("https://shingufinal-86da6-default-rtdb.asia-southeast1.firebasedatabase.app/").RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        userKey = PlayerPrefs.GetString("UserKey", "");
        if (string.IsNullOrEmpty(userKey))
        {
            AddMessageLog("UserKey가 없습니다.");
            return;
        }

        LoadUserData();
    }

    void LoadUserData()
    {
        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.Result == null || !task.Result.Exists)
            {
                dispatcher.Enqueue(() =>
                {
                    AddMessageLog("유저 데이터를 불러오지 못했습니다.");
                });
                return;
            }

            DataSnapshot snapshot = task.Result;

            string coinStr = snapshot.Child("Coin").Value.ToString();
            coin = int.Parse(coinStr);

            string inventoryJson = snapshot.Child("Inventory").Value != null
                ? snapshot.Child("Inventory").Value.ToString()
                : "{}";

            inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);
            if (inventory == null)
                inventory = new Dictionary<string, int>();

            dispatcher.Enqueue(() =>
            {
                UpdateCoinText();
                AddMessageLog("상점에 오신 것을 환영합니다.");
            });
        });
    }

    void UpdateCoinText()
    {
        CoinText.text = "Coin : " + coin.ToString();
    }

    void AddMessageLog(string message)
    {
        MessageText.text = message;
        messageLogs.Add(message);

        if (messageLogs.Count > MaxLogLines)
        {
            messageLogs.RemoveRange(0, messageLogs.Count - MaxLogLines);
        }

        if (MessageLogText != null)
        {
            MessageLogText.text = string.Join("\n", messageLogs);
        }
    }

    public void OnClickAddCoin()
    {
        coin += 100;

        Dictionary<string, object> updates = new Dictionary<string, object>();
        updates["Coin"] = coin;

        reference.Child("UserInfo").Child(userKey).UpdateChildrenAsync(updates).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    AddMessageLog("코인 증가 실패");
                });
                return;
            }

            dispatcher.Enqueue(() =>
            {
                UpdateCoinText();
                AddMessageLog("코인 +100");
            });
        });
    }

    public void OnClickBuyDashBooster()
    {
        BuyItem("DashBooster", DashBoosterPrice);
    }

    public void OnClickBuyShieldChip()
    {
        BuyItem("ShieldChip", ShieldChipPrice);
    }

    public void OnClickBuyPortalCore()
    {
        BuyItem("PortalCore", PortalCorePrice);
    }

    void BuyItem(string itemName, int price)
    {
        if (coin < price)
        {
            AddMessageLog("코인이 부족합니다.");
            return;
        }

        coin -= price;

        if (!inventory.ContainsKey(itemName))
            inventory[itemName] = 0;

        inventory[itemName] += 1;

        string inventoryJson = JsonConvert.SerializeObject(inventory);

        Dictionary<string, object> updates = new Dictionary<string, object>();
        updates["Coin"] = coin;
        updates["Inventory"] = inventoryJson;

        reference.Child("UserInfo").Child(userKey).UpdateChildrenAsync(updates).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    AddMessageLog("구매 실패");
                });
                return;
            }

            dispatcher.Enqueue(() =>
            {
                UpdateCoinText();
                AddMessageLog(itemName + " 구매 완료");
            });
        });
    }
}