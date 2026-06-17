using UnityEngine;
using UnityEngine.SceneManagement;

public class InventorySceneLoader : MonoBehaviour
{
    public void GoShopScene()
    {
        SceneManager.LoadScene("ShopScene");
    }
}