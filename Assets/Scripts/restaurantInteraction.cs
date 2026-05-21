using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


public class restaurantInteraction : MonoBehaviour
{
    public GameObject camPosChair;
    public GameObject camPosBathroom;
    public GameObject camPosFinder;


    public Button _camPosChair;
    public Button _camPosBathroom;
    public Button _camPosFinder;

    public GameObject _foodMenu;
    public GameObject _person;

    public GameObject _xrCam;

    // Start is called before the first frame update
    void Start()
    {
        _foodMenu.SetActive(false);
        _person.SetActive(false);
        _camPosChair.onClick.AddListener(menuScene);
        _camPosBathroom.onClick.AddListener(bathroomScene);
        _camPosFinder.onClick.AddListener(finderScene);
    }
    public void menuScene()
    {
        _xrCam.transform.position = camPosChair.transform.position;
        _xrCam.transform.rotation = camPosChair.transform.rotation;

        _foodMenu.SetActive(true);
        _person.SetActive(true);
    }

    public void bathroomScene()
    {
        _xrCam.transform.position = camPosBathroom.transform.position;
        _xrCam.transform.rotation = camPosBathroom.transform.rotation;

        _foodMenu.SetActive(false);
        _person.SetActive(true);
    }

    public void finderScene()
    {
        _xrCam.transform.position = camPosFinder.transform.position;
        _xrCam.transform.rotation = camPosFinder.transform.rotation;

        _foodMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
