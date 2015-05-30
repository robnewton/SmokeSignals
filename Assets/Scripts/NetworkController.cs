using UnityEngine;
using System.Collections;

public class NetworkController : MonoBehaviour
{
	string _room = "SmokeSignals";
	
	void Start()
	{
		PhotonNetwork.ConnectUsingSettings("v0.1");
	}
	
	void OnJoinedLobby()
	{
		Debug.Log("Joined Smoke Signals lobby");

		RoomOptions roomOptions = new RoomOptions() { };
		PhotonNetwork.JoinOrCreateRoom(_room, roomOptions, TypedLobby.Default);
	}
	
	void OnJoinedRoom()
	{
		PhotonNetwork.Instantiate("NetworkedPlayer", Vector3.zero, Quaternion.identity, 0);
	}
}