using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

/*
 * To Subscribe to data from the MyndbandManager in your class, use fx :
 * MyndbandManager.UpdateRawdataEvent += OnUpdateRawDataEvent;
 * (for raw EEG data)
 */

//public struct MyndbandEvent {
//    public DateTime systemTime;
//    public string packet;
//}

public enum MotorImageryEvent {
    MotorImagery,
    Rest,
}

public class StateObject
{
    // Client  socket.
    public TcpClient workSocket = null;
    // Size of receive buffer.
    public const int BufferSize = 512;
    // Receive buffer.
    public byte[] buffer = new byte[BufferSize];
    // Received data string.
    public StringBuilder sb = new StringBuilder();
}

public class OpenBCIInput : MonoBehaviour
{

	private TcpClient mySocket; 
  	private NetworkStream theStream;
    private StreamWriter theWriter;
    private StreamReader theReader;
    public string Host = "localhost";
    public Int32 Port = 5677;

    private MotorImageryEvent classification = MotorImageryEvent.Rest;
    public float classificationThreshold = 0.7f;

    private int testSampleChannelSize;
    private int testSampleCount;
    private int testChannelCount;

    private double[,] lastMatrix;

    private RawOpenVibeSignal lastSignal;

    public class RawOpenVibeSignal {

        public int channels;
        public int samples;
        public double[,] signalMatrix;
    }

    public enum BCIState {
        Disconnected,
        Connecting,
        ReceivingHeader,
        ReceivingData
    }

    private BCIState bciState;
    public string motorImageryEvent;
    private int inputNumber = 0;

    [Serializable]
    public class OnBCIStateChanged : UnityEvent<string, string> { }

    [Serializable]
    public class OnBCIMotorImagery : UnityEvent<MotorImageryEvent> { }

    [Serializable]
    public class OnInputFinished : UnityEvent<InputData> { }

    [Serializable]
    public class OnBCIEvent : UnityEvent<float> { }

    private Dictionary<string, List<string>> BCILogs;

    public OnBCIStateChanged onBCIStateChanged;
    public OnBCIMotorImagery onBCIMotorImagery;
    public OnInputFinished onInputFinished;
    public OnBCIEvent onBCIEvent;

    private Socket clientSocket;

    private static OpenBCIInput instance;

    void Start()
    {
        if (instance == null) {
            instance = this;
        }
        DontDestroyOnLoad(this);
        bciState = BCIState.Disconnected;
        onBCIStateChanged.Invoke(Enum.GetName(typeof(BCIState), bciState), "");
        StartCoroutine("ConnectToBCI");
    }

    public void StartLogging() {
        BCILogs = new Dictionary<string, List<string>>();
        BCILogs["Date"] = new List<string>();
        BCILogs["Timestamp"] = new List<string>();
        BCILogs["Event"] = new List<string>();
        BCILogs["MIEvent"] = new List<string>(); // TRUE, FALSE
        BCILogs["MIConfidence"] = new List<string>(); // fx 0.43
        BCILogs["RestConfidence"] = new List<string>(); // fx 0.51
        BCILogs["BCIState"] = new List<string>(); // fx "Connected, Disconnected, ReadingData"
    }

    public void LogEvent(String myevent = "", MotorImageryEvent miEvent = MotorImageryEvent.Rest, float miConfidence = -1f, float restConfidence = -1f) {
        BCILogs["Date"].Add(System.DateTime.Now.ToString("yyyy-MM-dd"));
        BCILogs["Timestamp"].Add(System.DateTime.Now.ToString("HH:mm:ss.ffff"));
        BCILogs["Event"].Add(myevent);
        BCILogs["MIEvent"].Add(Enum.GetName(typeof(MotorImageryEvent), miEvent)); // TRUE, FALSE
        BCILogs["MIConfidence"].Add(miConfidence.ToString()); // fx 0.43
        BCILogs["RestConfidence"].Add(restConfidence.ToString()); // fx 0.51
        BCILogs["BCIState"].Add(Enum.GetName(typeof(BCIState), bciState));
    }

    void FixedUpdate()
    {
        if (bciState == BCIState.Disconnected) {
            return;
        }
       float confidence = ((float)ReadSocket());
       if (confidence == -1f) {
           // No Stream available.
           return;
       }

       InputData inputData = new InputData();
       inputData.confidence = 1 - confidence;
       inputData.type = InputType.MotorImagery;
       MotorImageryEvent newClassification = MotorImageryEvent.Rest;
       inputData.validity = InputValidity.Rejected;
       if (confidence > classificationThreshold) {
           newClassification = MotorImageryEvent.MotorImagery;
           inputData.validity = InputValidity.Accepted;
       }
       if (newClassification != classification) {
           inputNumber++;
           inputData.inputNumber = inputNumber;
           onBCIMotorImagery.Invoke(newClassification);
           onInputFinished.Invoke(inputData);
           classification = newClassification;
       }
       if (classification != 0f) { 
        onBCIEvent.Invoke(confidence);
       }
       
    }

    private IEnumerator ConnectToBCI() {
        while (mySocket == null || !mySocket.Connected) {
            bciState = BCIState.Connecting;
            onBCIStateChanged.Invoke(Enum.GetName(typeof(BCIState), bciState), "Establishing connection to BCI Socket..");     
            yield return new WaitForSeconds(0.5f);
            try {
                    mySocket = new TcpClient(Host, Port);
                    theStream = mySocket.GetStream();
                    theWriter = new StreamWriter(theStream);
                    theReader = new StreamReader(theStream);
                }
            catch(SocketException e) {
                UnityEngine.Debug.LogError(e.Message);
                bciState = BCIState.Disconnected;
                onBCIStateChanged.Invoke(Enum.GetName(typeof(BCIState), bciState), "Could not connect to the BCI Socket..");
            }
            if (bciState == BCIState.Disconnected) {
                yield return new WaitForSeconds(2f);
            }
        }
        bciState = BCIState.ReceivingHeader;
        onBCIStateChanged.Invoke(Enum.GetName(typeof(BCIState), bciState), "Waiting for data..Make sure that Acquisition is paired with PC.");
        StateObject state = new StateObject();
        state.workSocket = mySocket;
        int variableSize = sizeof(UInt32);
        int variableCount = 8;
        int headerSize = variableCount * variableSize;
        byte[] headerBuffer = new byte[headerSize];
        yield return null;
    }

    public double ReadSocket()
    {
        if (theStream == null) {
            return -1;
        }

        if (theStream.DataAvailable)
        {
            // read header once
            if (bciState == BCIState.ReceivingHeader)
            {
                ReadHeader();
                bciState = BCIState.ReceivingData;
                onBCIStateChanged.Invoke(Enum.GetName(typeof(BCIState), bciState), "");
            }

            if (bciState == BCIState.ReceivingData)
                {
                // raw signal data
                // [nSamples x nChannels]
                // all channels for one sample are sent in a sequence, then all channels of the next sample

                // create a signal object to send it to another
                RawOpenVibeSignal newSignal = new RawOpenVibeSignal();
                newSignal.samples = testSampleCount;
                newSignal.channels = testChannelCount;

                double[,] newMatrix = new double[testSampleCount,testChannelCount];
                //Debug.Log("SampleCount: " + testSampleCount);
                //Debug.Log("ChannelCount: " + testChannelCount);

                byte[] buffer = new byte[testSampleChannelSize];

                theStream.Read(buffer, 0, testSampleChannelSize);


                int row = 0;
                int col = 0;
                    for (int i = 0; i < testSampleCount * testChannelCount * (sizeof(double)); i = i + (sizeof(double) * testChannelCount))
                    {
                        for (int j = 0; j < testChannelCount * sizeof(double); j = j + sizeof(double))
                        {

                            byte[] temp = new byte[8];

                            for(int k = 0; k < 8; k++)
                            {
                                temp[k] = buffer[i + j + k];
                            }

                        if (BitConverter.IsLittleEndian)
                        {
                           // Array.Reverse(temp);
                            double test = BitConverter.ToDouble(temp, 0);
                           
                            // TODO TEST THIS
                            //newMatrix[i / (8 * testChannelCount), j / 8] = test;
                            newMatrix[row, col] = test;
                        }
                        col++;

                        }
                    row++;
                    col = 0;
                    }

                newSignal.signalMatrix = newMatrix;
                lastSignal = newSignal;
                lastMatrix = newMatrix;

               return newMatrix[0, 0];
            }

        }
        return -1;

    }

    private void ReadHeader()
    {
        // size of header is 8 * size of unit = 32 byte

        int variableSize = sizeof(UInt32);
        int variableCount = 8;

        int headerSize = variableCount * variableSize;

        byte[] buffer = new byte[headerSize];

        theStream.Read(buffer, 0, headerSize);

        // version number (in network byte order)
        // endianness of the stream (in network byte order, 0==unknown, 1==little, 2==big, 3==pdp)
        // sampling frequency of the signal,
        //  number of channels,
        // number of samples per chunk and
        // three variables of padding


        UInt32 version, endiannes, frequency, channels, samples;

        byte[] v = new byte[4] { buffer[0], buffer[1], buffer[2], buffer[3] };
        byte[] e = new byte[4] { buffer[4], buffer[5], buffer[6], buffer[7] };
        byte[] f = new byte[4] { buffer[8], buffer[9], buffer[10], buffer[11] };
        byte[] c = new byte[4] { buffer[12], buffer[13], buffer[14], buffer[15] };
        byte[] s = new byte[4] { buffer[16], buffer[17], buffer[18], buffer[19] };
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(e);
            Array.Reverse(v);
            version = BitConverter.ToUInt32(v, 0);
            endiannes = BitConverter.ToUInt32(e, 0);
            frequency = BitConverter.ToUInt32(f, 0);
            channels = BitConverter.ToUInt32(c, 0);
            samples = BitConverter.ToUInt32(s, 0);
        }
        else
        {
            version = BitConverter.ToUInt32(v, 0);
            endiannes = BitConverter.ToUInt32(e, 0);
            frequency = BitConverter.ToUInt32(f, 0);
            channels = BitConverter.ToUInt32(c, 0);
            samples = BitConverter.ToUInt32(s, 0);
        }

        Debug.Log("Version: " + version + "\n" + "Endiannes: " + endiannes + "\n" + "sampling frequency of the signal: " + frequency + "\n" + "number of channels: " + channels + "\n" + "number of samples per chunk: " + samples + "\n");

        testSampleCount = buffer[16];
        testChannelCount = buffer[12];
        testSampleChannelSize = buffer[12] * buffer[16] * sizeof(double);

    }

	public void Disconnect(){
        if (theStream != null) {
            theReader.Close();
            mySocket.Close();
        }
        bciState = BCIState.Disconnected;
        onBCIStateChanged.Invoke(Enum.GetName(typeof(BCIState), bciState), "");
	}
	void OnApplicationQuit(){
		Disconnect();
	}

}
