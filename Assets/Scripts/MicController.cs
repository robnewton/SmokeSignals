/**************
 * Based upon code from
 * http://forum.unity3d.com/threads/microphone-network-test.123776/
 * 
 * As well as some bits from the Mic Control asset in the store
 * https://www.assetstore.unity3d.com/en/#!/content/12518
 **************/

using UnityEngine;
using System.Collections;

public class MicController : Photon.MonoBehaviour {

	public enum MicActivation {
		HoldToSpeak,
		PushToSpeak,
		ConstantSpeak
	}
	
	public int Frequency = 44100;
	public float sensitivity = 100;
	[Range(0,100)]
	public float sourceVolume = 100;//Between 0 and 100
	public string selectedDevice { get; private set; }
	public float loudness { get; private set; }
	public MicActivation activationStyle;
	
	private bool focused = true;
	private int amountSamples = 256; //increase to get better average, but will decrease performance. Best to leave it
	private int minFreq, maxFreq;
	private int lastSample;
	private AudioClip c;
	private double timer;
	
	void Start() {

		Debug.Log("Starting the players microphone...");

		if (photonView.isMine)
		{
			if (Microphone.devices.Length == 0)
			{
				Debug.Log("No microphone found!");
			}
			else
			{
				if (Microphone.devices.Length < 2)
				{
					//If there is only one device, select it by default
					selectedDevice = Microphone.devices[0].ToString();
				}
				else
				{
					//TODO: Allow the user to choose a mic using a VR menu
					//For now, just use the first device again until a menu is in place
					selectedDevice = Microphone.devices[0].ToString();
				}
				GetMicCaps();

				//c = Microphone.Start(selectedDevice, true, 10, minFreq);
				//while(Microphone.GetPosition(null) < 0) {}
				StartMicrophone();
			}
		}
	}

	public void GetMicCaps()
	{
		Microphone.GetDeviceCaps(selectedDevice, out minFreq, out maxFreq);//Gets the frequency of the device
		if ((minFreq + maxFreq) == 0)//These 2 lines of code are mainly for windows computers
			maxFreq = 44100;
		Debug.Log("The microphone caps are from " + minFreq + " to " + maxFreq);
	}

	public void StartMicrophone()
	{
		c = Microphone.Start(selectedDevice, true, 10, maxFreq);//Starts recording
		while (!(Microphone.GetPosition(selectedDevice) > 0)){} // Wait until the recording has started
		Debug.Log("The microphone is now recording");
		GetComponent<AudioSource>().Play(); // Play the audio source!  //Disable this for network transmitted audio
	}

	public void StopMicrophone()
	{
		GetComponent<AudioSource>().Stop();//Stops the audio
		Microphone.End(selectedDevice);//Stops the recording of the device
		Debug.Log("The microphone has stopped recording");
	}   

	void Update()
	{
		if (!focused)
			StopMicrophone();
		
		if (!Application.isPlaying)
			StopMicrophone();

		//Calculate loudness
		GetComponent<AudioSource>().volume = (sourceVolume / 100);
		loudness = GetAveragedVolume() * sensitivity * (sourceVolume / 10);

		//Hold To Speak!!
		if (activationStyle == MicActivation.HoldToSpeak)
		{
			if (Microphone.IsRecording(selectedDevice) && Input.GetKey(KeyCode.T) == false)
				StopMicrophone();
		
			if (Input.GetKeyDown(KeyCode.T)) //Push to talk
				StartMicrophone();

			if (Input.GetKeyUp(KeyCode.T))
				StopMicrophone();
		}
		
		//Push To Talk!!
		if (activationStyle == MicActivation.PushToSpeak)
		{
			if (Input.GetKeyDown(KeyCode.T))
			{
				if (Microphone.IsRecording(selectedDevice))
					StopMicrophone();
				
				else if (!Microphone.IsRecording(selectedDevice))
					StartMicrophone();
			}
		}

		//Slightly delayed networking transmission for better quality
		timer += Time.deltaTime;
		if (photonView.isMine && timer > .1f && Microphone.IsRecording(selectedDevice))
		{
			timer = 0;
			int pos = Microphone.GetPosition(null);
			int diff = pos - lastSample;
			if (diff > 0)
			{
				float[] samples = new float[diff * c.channels];
				c.GetData(samples, lastSample);
				byte[] ba = ToByteArray(samples);
				photonView.RPC("Send", PhotonTargets.All, ba, c.channels);
			}
			lastSample = pos;
		}
	}
	
	float GetAveragedVolume()
	{
		float[] data = new float[amountSamples];
		float a = 0;
		GetComponent<AudioSource>().GetOutputData(data,0);
		foreach(float s in data) {
			a += Mathf.Abs(s);
		}
		return a/amountSamples;
	}
	
	[RPC]
	public void Send(byte[] ba, int chan)
	{
		float[] f = ToFloatArray(ba);
		GetComponent<AudioSource>().clip = AudioClip.Create("test", f.Length, chan, Frequency, true, false);
		GetComponent<AudioSource>().clip.SetData(f, 0);
		if (!GetComponent<AudioSource>().isPlaying) GetComponent<AudioSource>().Play();
	}
	
	public byte[] ToByteArray(float[] floatArray)
	{
		int len = floatArray.Length * 4;
		byte[] byteArray = new byte[len];
		int pos = 0;
		foreach (float f in floatArray) {
			byte[] data = System.BitConverter.GetBytes(f);
			System.Array.Copy(data, 0, byteArray, pos, 4);
			pos += 4;
		}
		return byteArray;
	}
	
	public float[] ToFloatArray(byte[] byteArray)
	{
		int len = byteArray.Length / 4;
		float[] floatArray = new float[len];
		for (int i = 0; i < byteArray.Length; i+=4) {
			floatArray[i/4] = System.BitConverter.ToSingle(byteArray, i);
		}
		return floatArray;
	}
	
	void OnApplicationFocus(bool focus)
	{
		focused = focus;
	}
	
	void OnApplicationPause(bool focus)
	{
		focused = focus;
	}
}