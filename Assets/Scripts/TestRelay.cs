using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class TestRelay : MonoBehaviour
{
    // Start is called before the first frame update
    private async void Start()
    {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in "+AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // Update is called once per frame
    public async Task<bool> CreateRelay() {
        try { 
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2); 
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            Debug.Log("join code: "+joinCode);
            return true;
        } catch(RelayServiceException e){ Debug.Log(e); return false;}
        
    }

    public async Task<bool> JoinRelay(string joinCode) {
        try{
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
            Debug.Log("joined "+joinCode + " at " + joinAllocation.Region);

            return true;
        } catch(RelayServiceException e){ Debug.Log(e); return false;}
    }

    
}
