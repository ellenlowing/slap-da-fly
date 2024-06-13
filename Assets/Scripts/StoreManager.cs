using Oculus.Interaction;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance;
    [SerializeField] private List<StoreItemSO> items;
    [SerializeField] private SettingSO settings;

    [Header("UI")]
    [SerializeField] private GameObject StoreUI;

    [Header("Shared UI")]
    [SerializeField] public TextMeshProUGUI GlobalName;
    [SerializeField] public TextMeshProUGUI GlobalDescription;
    [SerializeField] public TextMeshProUGUI GlobalCashAmount;

    [Header("Buttons")]
    [SerializeField] private InteractableUnityEventWrapper PurchaseBtn;
    [SerializeField] private InteractableUnityEventWrapper NextWaveBtn;

    [Header("Events")]
    public VoidEventChannelSO StartNextWaveEvent;

    [Header("Power Up")]
    public GameObject Froggy;
    public GameObject InsecticideSpray;
    public GameObject ElectricSwatter;
    public List<GameObject> BoughtItems = new List<GameObject>();

    public List<GameObject> ShopItems;
    public List<GameObject> PowerUpItems;

    private BasePowerUpBehavior _selectedPowerUp;

    private void OnEnable()
    {
        PurchaseBtn.WhenSelect.AddListener(Purchase);
        NextWaveBtn.WhenSelect.AddListener(NextWave);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        foreach (var item in ShopItems)
        {
            item.SetActive(false);
            item.GetComponentInChildren<BasePowerUpBehavior>().StoreItemData.LocalPosition = item.transform.localPosition;
            item.GetComponentInChildren<BasePowerUpBehavior>().StoreItemData.LocalRotation = item.transform.localRotation;
        }

        PowerUpItems.Add(Froggy);
        PowerUpItems.Add(InsecticideSpray);
        PowerUpItems.Add(ElectricSwatter);
    }

    private void Purchase()
    {
        if (_selectedPowerUp != null)
        {
            var shopItemName = _selectedPowerUp.StoreItemData.Name;
            settings.Cash -= _selectedPowerUp.StoreItemData.Price;
            UIManager.Instance.UpdateCashUI();

            switch (shopItemName)
            {
                case "Froggy":
                    BoughtItems.Add(Froggy);
                    break;
                case "Insecticide Spray":
                    BoughtItems.Add(InsecticideSpray);
                    break;
                case "Electric Swatter":
                    BoughtItems.Add(ElectricSwatter);
                    break;
            }

            GameObject powerupItem = _selectedPowerUp.GetComponentInParent<Grabbable>().gameObject;
            powerupItem.SetActive(false);

            UIManager.Instance.AddTextPopUp(shopItemName + " Purchased!", powerupItem.transform.position);

            _selectedPowerUp = null;
        }
    }

    private void NextWave()
    {
        Debug.Log("[Testing] Next Wave Triggered");
        StartNextWaveEvent.RaiseEvent();
        foreach (var item in BoughtItems)
        {
            item.SetActive(true);
            UIManager.Instance.FaceCamera(item);
            item.GetComponentInChildren<BasePowerUpBehavior>().ResetPowerUp();
        }
        BoughtItems = new List<GameObject>();
    }

    public void ShowStore()
    {
        // Disable all power up items at the end of each wave !! TODO NOT WORKING
        foreach (var item in PowerUpItems)
        {
            item.SetActive(false);
        }

        StoreUI.SetActive(true);
        UIManager.Instance.FaceCamera(StoreUI);
        foreach (var item in ShopItems)
        {
            item.SetActive(true);
            item.transform.localPosition = item.GetComponentInChildren<BasePowerUpBehavior>().StoreItemData.LocalPosition;
            item.transform.localRotation = item.GetComponentInChildren<BasePowerUpBehavior>().StoreItemData.LocalRotation;
        }
    }

    public void HideStore()
    {
        StoreUI.SetActive(false);
    }

    public void SetActivePowerUp(BasePowerUpBehavior powerUp)
    {
        _selectedPowerUp = powerUp;
    }
}
