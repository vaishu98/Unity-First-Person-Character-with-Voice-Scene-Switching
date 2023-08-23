using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HuggingFace.API;
using System.IO;


public class SceneSwitch : MonoBehaviour
{
    private int selectedMicrophoneIndex = 0; 
    private string[] microphoneDevices;
    private AudioSource audioSource;
    private string keyword = "change"; 
    private byte[] bytes = {};

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        microphoneDevices = Microphone.devices;

        if (selectedMicrophoneIndex < microphoneDevices.Length)
        { 
            string selectedMicrophone = microphoneDevices[selectedMicrophoneIndex];
            audioSource.clip = Microphone.Start(selectedMicrophone, true, 10, 44100);

            audioSource.loop = true;
            //audioSource.Play();
        }
        else
        {
            Debug.LogError("Selected microphone index is out of range.");
        }
    }

    private void Update()
    {
        if (Microphone.IsRecording(null) && audioSource.clip != null)
        {
            float[] samples = new float[audioSource.clip.samples];
            audioSource.clip.GetData(samples, 0);
            Debug.Log("update - "+ samples);
            bytes = EncodeAsWAV(samples, audioSource.clip.frequency, audioSource.clip.channels);
            Debug.Log("extracted  - "+ bytes+" - now sending to hgf");
            SendRecording();
        }
    }

    private void ChangeScene()
    {
        Debug.Log("In change scene function... keyword recognized correctly");
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }

    private byte[] EncodeAsWAV(float[] samples, int frequency, int channels) {
        using (var memoryStream = new MemoryStream(44 + samples.Length * 2)) {
            using (var writer = new BinaryWriter(memoryStream)) {
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * 2);

                foreach (var sample in samples) {
                    writer.Write((short)(sample * short.MaxValue));
                }
            }
            return memoryStream.ToArray();
        }
    }

    private void SendRecording(){
        Debug.Log("In send recording to hugging face api..."+bytes);
        HuggingFaceAPI.AutomaticSpeechRecognition(bytes, response => {
            Debug.Log("extracted from hgf api - "+response);
            if(response.Contains(keyword)){
                ChangeScene();
            }
        }, error => {
            Debug.Log("Error using HuggingFaceAPI");
        });

    }

}
