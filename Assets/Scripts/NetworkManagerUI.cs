using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton, joinButton;
    [SerializeField] private TMP_Text joinInput;
    [SerializeField] private GameObject canvas;
    public TestRelay relay;
    
    // Start is called before the first frame update
    private void Awake() {

        hostButton.onClick.AddListener(async () => {
            bool success = await relay.CreateRelay();
            if(!success) Debug.Log("something went wrong");
            else canvas.SetActive(false);
        });
        joinButton.onClick.AddListener(async () => {
            bool success = await relay.JoinRelay(joinInput.text.Substring(0,joinInput.text.Length-1));
            if(!success) Debug.Log("something went wrong");
            else canvas.SetActive(false);
        });
    }

}
