#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Net;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using NatNetML;
#endregion usings

namespace VVVV.Nodes
{

    #region PluginInfo
    [PluginInfo(
        Name = "Client",
        Category = "OptiTrack",
        Version = "NatNet",
        Help = "Send messages and requests to the OptiTrack NatNet server.",
        Author = "id144",
        Tags = "")]

    #endregion PluginInfo
    public class NatNetOptiTrackClientNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Init", DefaultValue = 0.0, IsBang = true, IsSingle = true)]
        public ISpread<bool> FInputInit;

        [Input("Server IP", IsSingle = true, DefaultString = "127.0.0.1")]
        public ISpread<string> FInputIPServer;

        [Input("Client IP", IsSingle = true, DefaultString = "127.0.0.1")]
        public ISpread<string> FInputIPClient;

        [Output("Frame data")]
        public ISpread<NatNetML.FrameOfMocapData> FOutputFrameData;

        [Output("Client")]
        public ISpread<NatNetML.NatNetClientML> FOutputClient;

        [Output("Timestamp")]
        public ISpread<double> FOutputTimestamp;

        [Output("Frame Count")]
        public ISpread<int> FOutputFrameCount;

        [Output("Status")]
        public ISpread<string> FOutputStatus;

        [Output("Client Version")]
        public ISpread<string> FOutputVersion;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins
        private NatNetML.NatNetClientML m_NatNet;
        private int returnCode = 0;
        NatNetML.ServerDescription desc = new NatNetML.ServerDescription();

        private Queue<NatNetML.FrameOfMocapData> m_FrameQueue = new Queue<NatNetML.FrameOfMocapData>();
        void m_NatNet_OnFrameReady(NatNetML.FrameOfMocapData data, NatNetML.NatNetClientML client)
        {
            m_FrameQueue.Clear();
            FrameOfMocapData deepCopy = new FrameOfMocapData(data);
            m_FrameQueue.Enqueue(deepCopy);

            FOutputFrameData.SliceCount = 1;
            FOutputFrameData[0] = deepCopy;

            FOutputTimestamp.SliceCount = 1;
            FOutputTimestamp[0] = deepCopy.fTimestamp;

            FOutputFrameCount.SliceCount = 1;
            FOutputFrameCount[0] = deepCopy.iFrame;
        }

        public void Evaluate(int SpreadMax)
        {
            if (FInputInit[0])
            {
                

                if (m_NatNet != null)
                {
                    m_NatNet.Disconnect();
                }

                m_NatNet = new NatNetML.NatNetClientML();
                
                NatNetClientML.ConnectParams _params = new NatNetClientML.ConnectParams();
                _params.ServerAddress = FInputIPServer[0];
                _params.LocalAddress = FInputIPClient[0];
                _params.ConnectionType = ConnectionType.Multicast;
                returnCode = m_NatNet.Connect(_params);
                returnCode = m_NatNet.GetServerDescription(desc);

                FOutputStatus.SliceCount = 1;
                FOutputStatus[0] = "";

                m_NatNet.OnFrameReady += new NatNetML.FrameReadyEventHandler(m_NatNet_OnFrameReady);

                //Version info
                int[] ver = new int[4];
                ver = m_NatNet.NatNetVersion();
                FOutputVersion.SliceCount = 1;
                FOutputVersion[0] = String.Format("{0}.{1}.{2}.{3}", ver[0], ver[1], ver[2], ver[3]);
                FOutputClient.SliceCount = 1;
                FOutputClient[0] = m_NatNet;
            }
        }
        public void Dispose()
        {
            if (m_NatNet != null)
            {
                m_NatNet.OnFrameReady -= m_NatNet_OnFrameReady;
                m_NatNet.Disconnect();
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Markers", Category = "OptiTrack", Version = "NatNet", Help = "Basic template with one value in/out", Tags = "")]
    #endregion PluginInfo
    public class NatNetOptiTrackLabeledMarkersNode : IPluginEvaluate
    {
        #region fields & pins

        [Input("Frame data", IsSingle = true)]
        public ISpread<NatNetML.FrameOfMocapData> FInputFrameData;

        [Output("Labeled Markers")]
        public ISpread<Vector3D> FOutputLabeledMarkerXYZ;

    	[Output("Labeled Markers")]
        public ISpread<NatNetML.Marker[]> FOutputLabeledMarkers;
    	
    	[Output("Other Markers")]
        public ISpread<NatNetML.Marker[]> FOutputOtherMarkers;

    	
        [Output("Other Markers")]
        public ISpread<Vector3D> FOutputOtherMarkerXYZ;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        private Queue<NatNetML.FrameOfMocapData> m_FrameQueue = new Queue<NatNetML.FrameOfMocapData>();
        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {


            if (FInputFrameData[0] != null)
            {

                FOutputLabeledMarkers.SliceCount = (FInputFrameData[0].nMarkers > 1) ? 1 : 0;
                FOutputOtherMarkers.SliceCount = (FInputFrameData[0].nOtherMarkers>1)? 1:0;

                if (FInputFrameData[0].LabeledMarkers[0] != null)
                {                   
                    FOutputLabeledMarkers[0] = FInputFrameData[0].LabeledMarkers;
                }

                if (FInputFrameData[0].OtherMarkers[0] != null)
                {
                    FOutputOtherMarkers[0] = FInputFrameData[0].OtherMarkers;
                }
            	           	
                FOutputLabeledMarkerXYZ.SliceCount = FInputFrameData[0].nMarkers;
            	           	            	            	
                for (int i = 0; i < FInputFrameData[0].nMarkers; i++)
                {
                    Vector3D _vect;
                    _vect.x = FInputFrameData[0].LabeledMarkers[i].x;
                    _vect.y = FInputFrameData[0].LabeledMarkers[i].y;
                    _vect.z = FInputFrameData[0].LabeledMarkers[i].z;
                    FOutputLabeledMarkerXYZ[i] = _vect;
                }

                FOutputOtherMarkerXYZ.SliceCount = FInputFrameData[0].nOtherMarkers;
                for (int i = 0; i < FInputFrameData[0].nOtherMarkers; i++)
                {
                    Vector3D _vect;
                    _vect.x = FInputFrameData[0].OtherMarkers[i].x;
                    _vect.y = FInputFrameData[0].OtherMarkers[i].y;
                    _vect.z = FInputFrameData[0].OtherMarkers[i].z;
                    FOutputOtherMarkerXYZ[i] = _vect;
                }
            }
            else
            {
                FOutputLabeledMarkerXYZ.SliceCount = 0;
                FOutputOtherMarkerXYZ.SliceCount = 0;
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "RigidBodies", Category = "OptiTrack", Version = "NatNet", Help = "Basic template with one value in/out", Tags = "")]
    #endregion PluginInfo
    public class NatNetOptiTrackRigidBodiesNode : IPluginEvaluate
    {
        #region fields & pins

        [Input("Frame data")]
        public ISpread<NatNetML.FrameOfMocapData> FInputFrameData;

        [Output("ID")]
        public ISpread<int> FOutputID;

        [Output("Name")]
        public ISpread<string> FOutputName;

        [Output("Transform Out")]
        public ISpread<Matrix4x4> FOutputTransform;

        [Output("IsTracked")]
        public ISpread<bool> FOutputTracked;

      //  [Output("Markers")]
      //  public ISpread<NatNetML.Marker[]> FOutputMarkers;

      //  [Output("N Markers")]
     //   public ISpread<int> FOutputNMarkers;

        [Output("MeanError")]
        public ISpread<double> FOutputMeanError;


        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        //called when data for any output pin is requested

        public void Evaluate(int SpreadMax)
        {
            if (FInputFrameData[0] != null)
            {
                int _rigidBodyCount = FInputFrameData[0].nRigidBodies;
                FOutputID.SliceCount = _rigidBodyCount;
                FOutputTransform.SliceCount = _rigidBodyCount;
                FOutputTracked.SliceCount = _rigidBodyCount;
                //FOutputMarkers.SliceCount = _rigidBodyCount;
                //FOutputNMarkers.SliceCount = _rigidBodyCount;
                FOutputMeanError.SliceCount = _rigidBodyCount;

                for (int i = 0; i < _rigidBodyCount; i++)
                {
                    NatNetML.RigidBodyData rb = FInputFrameData[0].RigidBodies[i];
                    List<NatNetML.DataDescriptor> descs = new List<NatNetML.DataDescriptor>();

;

                    FOutputID[i] = rb.ID;
                    //FOutputName[i] = rb.Name;	            	
                    FOutputTracked[i] = rb.Tracked;
                    FOutputMeanError[i] = rb.MeanError;

                    Matrix4x4 _rigidBody;

                    double qw = rb.qw;
                    double qx = rb.qx;
                    double qy = rb.qy;
                    double qz = rb.qz;
                    double n = 1.0f / Math.Sqrt(qx * qx + qy * qy + qz * qz + qw * qw);
                    qx *= n;
                    qy *= n;
                    qz *= n;
                    qw *= n;

                    _rigidBody.m11 = 1.0f - 2.0f * qy * qy - 2.0f * qz * qz;
                    _rigidBody.m12 = 2.0f * qx * qy - 2.0f * qz * qw;
                    _rigidBody.m13 = 2.0f * qx * qz + 2.0f * qy * qw;
                    _rigidBody.m14 = 0;

                    _rigidBody.m21 = 2.0f * qx * qy + 2.0f * qz * qw;
                    _rigidBody.m22 = 1.0f - 2.0f * qx * qx - 2.0f * qz * qz;
                    _rigidBody.m23 = 2.0f * qy * qz - 2.0f * qx * qw;
                    _rigidBody.m24 = 0;

                    _rigidBody.m31 = 2.0f * qx * qz - 2.0f * qy * qw;
                    _rigidBody.m32 = 2.0f * qy * qz + 2.0f * qx * qw;
                    _rigidBody.m33 = 1.0f - 2.0f * qx * qx - 2.0f * qy * qy;
                    _rigidBody.m34 = 0;

                    _rigidBody.m41 = rb.x;
                    _rigidBody.m42 = rb.y;
                    _rigidBody.m43 = rb.z;
                    _rigidBody.m44 = 1;

                    FOutputTransform[i] = _rigidBody;
                }

            }
            else
            {
                FOutputID.SliceCount = 0;
                FOutputTransform.SliceCount = 0;
                FOutputTracked.SliceCount = 0;
                //FOutputMarkers.SliceCount = 0;
                //FOutputNMarkers.SliceCount = 0;
                FOutputMeanError.SliceCount = 0;
            }
        }
    }
    //http://wiki.optitrack.com/index.php?title=NatNet:_Remote_Requests/Commands
    #region PluginInfo
    public enum NatNetCommandsEnum
    {
        UnitsToMillimeters,
        FrameRate,
        StartRecording,
        StopRecording,
        LiveMode,
        EditMode,
        CurrentMode,
        TimelinePlay,
        TimelineStop,
        SetPlaybackTakeName,
        SetRecordTakeName,
        SetCurrentSession,
        SetPlaybackStartFrame,
        SetPlaybackStopFrame,
        SetPlaybackCurrentFrame,
        CurrentTakeLength,
        AnalogSamplesPerMocapFrame
    }
	
    [PluginInfo(
		Name = "SendMessage", 
		Category = "OptiTrack", 
		Version = "NatNet", 
		Help = "Send messages and requests to the OptiTrack NatNet server.", 
		Tags = "OptiTrack")]
	
    #endregion PluginInfo
    public class NatNetOptiTrackSendMessageNode : IPluginEvaluate
    {
        #region fields & pins
   	    [Input("Client")]
        public ISpread<NatNetML.NatNetClientML> FInputClient;

        [Input("Command", DefaultEnumEntry = "FrameRate")]
        public ISpread<NatNetCommandsEnum> FInputCommandEnum;

        [Input("Send", DefaultValue = 0.0, IsBang = true, IsSingle = true)]
        public ISpread<bool> FInputSend;

        [Output("Status")]
        public ISpread<string> FOutputStatus;

        [Output("Response")]
        public ISpread<string> FOutputResponse;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (FInputSend[0] &&(FInputClient[0] != null))
            {
                string _command = FInputCommandEnum[0].ToString();

                int nBytes = 0;
                byte[] response = new byte[10000];
                int rc = FInputClient[0].SendMessageAndWait(_command, 3, 100, out response, out nBytes);
                if (rc != 0)
                {
                    FOutputStatus.SliceCount = 1;
                    FOutputStatus[0] = (_command + " not handled by server");
                    FOutputResponse.SliceCount = 1;
                    FOutputResponse[0] = "";
                }
                else
                {
                    string s = System.Text.Encoding.ASCII.GetString(response, 0, nBytes);

                    FOutputStatus.SliceCount = 1;
                    FOutputResponse.SliceCount = 1;
                    FOutputResponse[0] = s;

                    int opResult = System.BitConverter.ToInt32(response, 0);
                    if (opResult == 0)
                    {
                        FOutputStatus[0] = (_command + " handled and succeeded.");
                    }
                    else
                    {
                        FOutputStatus[0] = (_command + " handled but failed.");
                    }
                }
            }
        }
    }

	 #region PluginInfo
    [PluginInfo(
		Name = "Marker", 
		Category = "OptiTrack", 
		Version = "NatNet", 
		Help = "Get marker info.", 
		Tags = "OptiTrack")]
	
    #endregion PluginInfo
    public class NatNetOptiTrackMarkerNode : IPluginEvaluate
    {
        #region fields & pins

        [Input("Markers")]
        public ISpread<NatNetML.Marker[]> FInputMarkers;

        [Output("")]
        public ISpread<Vector3D> FOutputOtherMarkerXYZ;

        [Output("ID")]
        public ISpread<int> FOutputID;

        [Output("Size")]
        public ISpread<double> FOutputSize;

        #endregion fields & pins
        Vector3D _vect;

        public void Evaluate(int SpreadMax)
        {
            int markerCount = 0;
            FOutputOtherMarkerXYZ.SliceCount = 0;
            FOutputID.SliceCount = 0;
            FOutputSize.SliceCount = 0;
            for (int j = 0; j < FInputMarkers.SliceCount; j++)
            {
                Marker[] mrkr = FInputMarkers[j];
                if (mrkr != null)
                {
                    for (int i = 0; i < mrkr.Length; i++)
                    {
                        if (mrkr[i] != null)
                        {
                            markerCount++;
                            FOutputOtherMarkerXYZ.SliceCount = markerCount;
                            FOutputID.SliceCount = markerCount;
                            FOutputSize.SliceCount = markerCount;

                            _vect.x = mrkr[i].x;
                            _vect.y = mrkr[i].y;
                            _vect.z = mrkr[i].z;

                            FOutputSize[i] = mrkr[i].size;
                            FOutputID[i] = mrkr[i].ID;
                            FOutputOtherMarkerXYZ[i] = _vect;
                        }
                    }
                }
            }
        }
    }	
	 #region PluginInfo
    [PluginInfo(
		Name = "Descriptions", 
		Category = "OptiTrack", 
		Version = "NatNet", 
		Help = "Get marker info.", 
		Tags = "OptiTrack")]
	
    #endregion PluginInfo
    public class NatNetOptiTrackDescriptionsNode : IPluginEvaluate
    {
        #region fields & pins

   	    [Input("Client")]
        public ISpread<NatNetML.NatNetClientML> FInputClient;
    	
        [Input("Update", DefaultValue = 0.0, IsBang = true, IsSingle = true)]
        public ISpread<bool> FInputUpdate;
    	
        [Output("")]
        public ISpread<Vector3D> FOutputOtherMarkerXYZ;

        [Output("ID")]
        public ISpread<int> FOutputID;

        [Output("Size")]
        public ISpread<double> FOutputSize;

        #endregion fields & pins
        Vector3D _vect;

        public void Evaluate(int SpreadMax)
        {
        	if (FInputUpdate[0]) 
        	{
	            List<NatNetML.DataDescriptor> descs = new List<NatNetML.DataDescriptor>();

	            bool bSuccess = FInputClient[0].GetDataDescriptions(out descs);

	            if (bSuccess)
	            {
	            	FOutputID.SliceCount = 1;
	            	FOutputID[0] = 2;
	            }        		
        	}
        }
    }		
}

