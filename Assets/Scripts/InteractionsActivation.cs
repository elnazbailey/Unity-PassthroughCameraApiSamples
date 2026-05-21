using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InteractionsActivation : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject camPosKitchen;
    public GameObject camPosLaundry;
    public GameObject camPosChair;
    public GameObject camPosThermostat;

    public Button _MeatCut;
    public Button _SpillCleaning;

    public Button _LaundryMachine;
    public Button _LaundryFolding;
    public Button _phoneOperate;

    public GameObject _towelSpill;
    public GameObject _spillGO;

    public GameObject _cuttingBoard;
    public GameObject _knife;
    public GameObject _meat;

    public GameObject _laundryBasket;
    public GameObject _cleanClothes;

    public GameObject _xrCam;
    void Start()
    {
        _MeatCut.onClick.AddListener(kitchenSceneMeat);
        _SpillCleaning.onClick.AddListener(kitchenSceneSpill);
        _LaundryMachine.onClick.AddListener(laundrySceneMachine);
        _LaundryFolding.onClick.AddListener(laundrySceneFolding);
        _phoneOperate.onClick.AddListener(phoneActivate);
    }
    public void kitchenSceneMeat()
    {
        _xrCam.transform.position = camPosKitchen.transform.position;
        _xrCam.transform.rotation = camPosKitchen.transform.rotation;

        _towelSpill.SetActive(false);
        _spillGO.SetActive(false);

        _cuttingBoard.SetActive(true);
        _knife.SetActive(true);
        _meat.SetActive(true);
            /*
                public GameObject _towelSpill;
        public GameObject _spillGO;

        public GameObject _cuttingBoard;
        public GameObject _knife;
            */
}

    public void kitchenSceneSpill()
    {
        _xrCam.transform.position = camPosKitchen.transform.position;
        _xrCam.transform.rotation = camPosKitchen.transform.rotation;

        _towelSpill.SetActive(true);
        _spillGO.SetActive(true);

        _cuttingBoard.SetActive(false);
        _knife.SetActive(false);
        _meat.SetActive(false);
    }

    public void laundrySceneMachine()
    {
        _xrCam.transform.position = camPosLaundry.transform.position;
        _xrCam.transform.rotation = camPosLaundry.transform.rotation;
        _laundryBasket.SetActive(false);
        _cleanClothes.SetActive(false);
        /*
            public GameObject _laundryBasket;
    public GameObject _cleanClothes;
        */
}

    public void laundrySceneFolding()
    {

        _xrCam.transform.position = camPosLaundry.transform.position;
        _xrCam.transform.rotation = camPosLaundry.transform.rotation;
        _laundryBasket.SetActive(true);
        _cleanClothes.SetActive(true);
    }
    public void phoneActivate()
    {
        _xrCam.transform.position = camPosChair.transform.position;
        _xrCam.transform.rotation = camPosChair.transform.rotation;
        SceneManager.LoadScene(sceneBuildIndex:1);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
