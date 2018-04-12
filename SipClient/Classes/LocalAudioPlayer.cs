using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Media;
using System.Threading.Tasks;
using System.Threading;

namespace SipClient.Classes
{
    public class LocalAudioPlayer
    {
        public static Dictionary<string, System.IO.UnmanagedMemoryStream> DTFMS_DICTONARY
            = new Dictionary<string, System.IO.UnmanagedMemoryStream>()
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

        private static SoundPlayer soundPlayer = new SoundPlayer();

        public static void PlaySound(System.IO.UnmanagedMemoryStream stream, bool isLoop = false)
        {
            if (stream != null)
            {
                stream.Position = 0;
                soundPlayer.Stream = null;
                soundPlayer.Stream = stream;
                if (isLoop)
                    soundPlayer.PlayLooping();
                else
                    soundPlayer.Play();
            }
        }

        public static void StopSound()
        {
            if (soundPlayer.IsLoadCompleted)
            {
                soundPlayer.Stop();
                soundPlayer.Stream = null;
            }
        }
    }
}
