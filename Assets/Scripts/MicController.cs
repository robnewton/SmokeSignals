/**************
 * Based upon code from
 * http://forum.unity3d.com/threads/microphone-network-test.123776/
 **************/

using UnityEngine;
using System.Collections;

public class MicController : Photon.MonoBehaviour {
	
	public int Frequency = 44100;

	private int lastSample;
	private AudioClip c;
	private double timer;
	
	void Start () {

		Debug.Log("Microphone is started.");

		if (photonView.isMine) {
			c = Microphone.Start(null, true, 100, Frequency);
			while(Microphone.GetPosition(null) < 0) {}
		}
	}

	void Update()
	{
		timer += Time.deltaTime;
		if (photonView.isMine && timer > .1f)
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
	
	[RPC]
	public void Send(byte[] ba, int chan) {
		float[] f = ToFloatArray(ba);
		GetComponent<AudioSource>().clip = AudioClip.Create("test", f.Length, chan, Frequency,true,false);
		GetComponent<AudioSource>().clip.SetData(f, 0);
		if (!GetComponent<AudioSource>().isPlaying) GetComponent<AudioSource>().Play();
	}
	
	public byte[] ToByteArray(float[] floatArray) {
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
	
	public float[] ToFloatArray(byte[] byteArray) {
		int len = byteArray.Length / 4;
		float[] floatArray = new float[len];
		for (int i = 0; i < byteArray.Length; i+=4) {
			floatArray[i/4] = System.BitConverter.ToSingle(byteArray, i);
		}
		return floatArray;
	}
}