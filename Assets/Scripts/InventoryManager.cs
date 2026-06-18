using System.Collections.Generic;
using Firebase.Database;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class InventoryManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Text DashBoosterCountText;
    [SerializeField] Text ShieldChipCountText;
    [SerializeField] Text PortalCoreCountText;
    [SerializeField] Text MessageText;
    [SerializeField] Text MessageLogText;

    [Header("Log")]
    [SerializeField] int MaxLogLines = 8;

    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    string userKey;
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

        LoadInventory();
    }

    void LoadInventory()
    {
        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.Result == null || !task.Result.Exists)
            {
                dispatcher.Enqueue(() =>
                {
                    AddMessageLog("인벤토리를 불러오지 못했습니다.");
                });
                return;
            }

            DataSnapshot snapshot = task.Result;

            string inventoryJson = snapshot.Child("Inventory").Value != null
                ? snapshot.Child("Inventory").Value.ToString()
                : "{}";

            inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);
            if (inventory == null)
                inventory = new Dictionary<string, int>();

            dispatcher.Enqueue(() =>
            {
                UpdateInventoryText();
                AddMessageLog("인벤토리 로드 완료");
            });
        });
    }

    void UpdateInventoryText()
    {
        DashBoosterCountText.text = "DashBooster : " + GetCount("DashBooster");
        ShieldChipCountText.text = "ShieldChip : " + GetCount("ShieldChip");
        PortalCoreCountText.text = "PortalCore : " + GetCount("PortalCore");
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

    int GetCount(string itemName)
    {
        if (inventory.ContainsKey(itemName))
            return inventory[itemName];
        return 0;
    }

    public void OnClickUseDashBooster()
    {
        UseItem("DashBooster");
    }

    public void OnClickUseShieldChip()
    {
        UseItem("ShieldChip");
    }

    public void OnClickUsePortalCore()
    {
        UseItem("PortalCore");
    }

    void UseItem(string itemName)
    {
        if (!inventory.ContainsKey(itemName) || inventory[itemName] <= 0)
        {
            AddMessageLog(itemName + "이 없습니다.");
            return;
        }

        inventory[itemName] -= 1;

        string inventoryJson = JsonConvert.SerializeObject(inventory);

        Dictionary<string, object> updates = new Dictionary<string, object>();
        updates["Inventory"] = inventoryJson;

        reference.Child("UserInfo").Child(userKey).UpdateChildrenAsync(updates).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    AddMessageLog("사용 실패");
                });
                return;
            }

            dispatcher.Enqueue(() =>
            {
                UpdateInventoryText();
                AddMessageLog(itemName + " 사용 완료");
            });
        });
    }
}