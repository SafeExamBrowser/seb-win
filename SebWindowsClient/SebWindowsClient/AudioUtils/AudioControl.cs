using System;
using NAudio.CoreAudioApi;

namespace SebWindowsClient.AudioUtils
{
    public class AudioControl
    {
        private readonly MMDeviceCollection DevCol;

        public AudioControl()
        {
            //Instantiate an Enumerator to find audio devices
            MMDeviceEnumerator mmde = new MMDeviceEnumerator();
            //Get all the devices, no matter what condition or status
            DevCol = mmde.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
        }

        public float GetVolumeScalar()
        {
            try
            {
                //Loop through all devices
                foreach (MMDevice dev in DevCol)
                {
                    try
                    {
                        //Mute it
                        return dev.AudioEndpointVolume.MasterVolumeLevelScalar;
                    }
                    catch (Exception ex)
                    {
                        //Do something with exception when an audio endpoint could not be muted
                    }
                }
            }
            catch (Exception ex)
            {
                //When something happend that prevent us to iterate through the devices
            }
            return 0;
        }

        public void Mute(bool mute)
        {
            try
            {
                //Loop through all devices
                foreach (MMDevice dev in DevCol)
                {
                    try
                    {
                        //Mute it
                        dev.AudioEndpointVolume.Mute = mute;
                    }
                    catch (Exception ex)
                    {
                        //Do something with exception when an audio endpoint could not be muted
                    }
                }
            }
            catch (Exception ex)
            {
                //When something happend that prevent us to iterate through the devices
            }
        }

        public bool GetMute()
        {
            try
            {
                //Loop through all devices
                foreach (MMDevice dev in DevCol)
                {
                    try
                    {
                        return dev.AudioEndpointVolume.Mute;
                    }
                    catch (Exception ex)
                    {
                        //Do something with exception when an audio endpoint could not be muted
                    }
                }
            }
            catch (Exception ex)
            {
                //When something happend that prevent us to iterate through the devices
            }
            return false;
        }

        public void SetVolumeScalar(float volume)
        {
            try
            {
                //Loop through all devices
                foreach (NAudio.CoreAudioApi.MMDevice dev in DevCol)
                {
                    try
                    {
                        //Mute it
                        dev.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
                    }
                    catch (Exception ex)
                    {
                        //Do something with exception when an audio endpoint could not be muted
                    }
                }
            }
            catch (Exception ex)
            {
                //When something happend that prevent us to iterate through the devices
            }
        }
    }
}