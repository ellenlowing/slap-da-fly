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
        }
    }

    private void Purchase()
    {
        if (_selectedPowerUp != null)
        {
            var shopItemName = _selectedPowerUp.StoreItemData.Name;
            settings.Cash -= _selectedPowerUp.StoreItemData.Price;

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
        }
        BoughtItems = new List<GameObject>();
    }

    public void ShowStore()
    {
        StoreUI.SetActive(true);
        UIManager.Instance.FaceCamera(StoreUI);
        foreach (var item in ShopItems)
        {
            item.SetActive(true);
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
