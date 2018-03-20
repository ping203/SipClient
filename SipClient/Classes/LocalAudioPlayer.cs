using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Media;
using System.Threading.Tasks;

namespace SipClient.Classes
{
    public class LocalAudioPlayer
    {
        private static SoundPlayer soundPlayer = new SoundPlayer();

        public static Dictionary<string, System.IO.UnmanagedMemoryStream> DTFMS_DICTONARY = new Dictionary<string, System.IO.UnmanagedMemoryStream>()
        {
            { "0", Properties.Resources.dtmf_0 },
            { "1", Properties.Resources.dtmf_1 },
            { "2", Properties.Resources.dtmf_2 },
            { "3", Properties.Resources.dtmf_3 },
            { "4", Properties.Resources.dtmf_4 },
            { "5", Properties.Resources.dtmf_5 },
            { "6", Properties.Resources.dtmf_6 },
            { "7", Properties.Resources.dtmf_7 },
            { "8", Properties.Resources.dtmf_8 },
            { "9", Properties.Resources.dtmf_9 },
            { "*", Properties.Resources.dtmf_star },
            { "#", Properties.Resources.dtmf_hash },
        };

        public static void PlaySound(System.IO.UnmanagedMemoryStream dtfm_stream)
        {
            if (dtfm_stream != null)
            {
                Task.Factory.StartNew(() =>
                {
                    soundPlayer.Stream = dtfm_stream;
                    try
                    {
                        soundPlayer.Play();
                    }
                    catch (Exception)
                    {
                       
                    }
                   
                });
            }
        }

        public static void PlayIcnomingCallSound()
        {
            soundPlayer.Stream = Properties.Resources.signal;
            soundPlayer.PlayLooping();
        }

        public static void StopSound()
        {
            if (soundPlayer.IsLoadCompleted)
                soundPlayer.Stop();
        }
    }
}
