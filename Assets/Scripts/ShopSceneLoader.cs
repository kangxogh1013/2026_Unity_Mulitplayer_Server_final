using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopSceneLoader : MonoBehaviour
{
    public void GoInventoryScene()
    {
        SceneManager.LoadScene("InventoryScene");
    }
}