using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Harmony;
using MelonLoader;
using OWOGame;
//using MyOWOSensations;

namespace MyBhapticsTactsuit
{
    public class TactsuitVR
    {
        /* A class that contains the basic functions for the bhaptics Tactsuit, like:
         * - A Heartbeat function that can be turned on/off
         * - A function to read in and register all .tact patterns in the bHaptics subfolder
         * - A logging hook to output to the Melonloader log
         * - 
         * */
        public bool suitDisabled = true;
        public bool systemInitialized = false;
        // Event to start and stop the heartbeat thread
        public Dictionary<String, Sensation> FeedbackMap = new Dictionary<String, Sensation>();

        public TactsuitVR()
        {
            RegisterAllTactFiles();
            InitializeOWO();
        }

        private async void InitializeOWO()
        {
            LOG("Initializing suit");

            // New auth.
            var gameAuth = GameAuth.Create(AllBakedSensations()).WithId("79724427");
            
            OWO.Configure(gameAuth);
            string[] myIPs = getIPsFromFile("OWO_Manual_IP.txt");
            if (myIPs.Length == 0) await OWO.AutoConnect();
            else
            {
                await OWO.Connect(myIPs);
            }

            if (OWO.ConnectionState == ConnectionState.Connected)
            {
                suitDisabled = false;
                LOG("OWO suit connected.");
            }
            if (suitDisabled) LOG("Owo is not enabled?!?!");
        }

        public string[] getIPsFromFile(string filename)
        {
            List<string> ips = new List<string>();
            string filePath = Directory.GetCurrentDirectory() + "\\Mods\\" + filename;
            if (File.Exists(filePath))
            {
                LOG("Manual IP file found: " + filePath);
                var lines = File.ReadLines(filePath);
                foreach (var line in lines)
                {
                    if (IPAddress.TryParse(line, out _)) ips.Add(line);
                    else LOG("IP not valid? ---" + line + "---");
                }
            }
            return ips.ToArray();
        }

        ~TactsuitVR()
        {
            LOG("Destructor called");
            DisconnectOwo();
        }

        private BakedSensation[] AllBakedSensations()
        {
            var result = new List<BakedSensation>();

            foreach (var sensation in FeedbackMap.Values)
            {
                if (sensation is BakedSensation baked)
                {
                    LOG("Registered baked sensation: " + baked.name);
                    result.Add(baked);
                }
                else
                {
                    LOG("Sensation not baked? " + sensation);
                    continue;
                }
            }
            return result.ToArray();
        }

        public void DisconnectOwo()
        {
            LOG("Disconnecting Owo skin.");
            OWO.Disconnect();
        }

        public void LOG(string logStr)
        {
            MelonLogger.Msg(logStr);
        }

        void RegisterAllTactFiles()
        {

            string configPath = Directory.GetCurrentDirectory() + "\\Mods\\OWO";
            DirectoryInfo d = new DirectoryInfo(configPath);
            FileInfo[] Files = d.GetFiles("*.owo", SearchOption.AllDirectories);
            for (int i = 0; i < Files.Length; i++)
            {
                string filename = Files[i].Name;
                string fullName = Files[i].FullName;
                string prefix = Path.GetFileNameWithoutExtension(filename);
                // LOG("Trying to register: " + prefix + " " + fullName);
                if (filename == "." || filename == "..")
                    continue;
                string tactFileStr = File.ReadAllText(fullName);
                try
                {
                    Sensation test = Sensation.Parse(tactFileStr);
                    FeedbackMap.Add(prefix, test);
                }
                catch (Exception e) { LOG(e.ToString()); }

            }

            systemInitialized = true;
        }


        public void PlayBackHit(float xzAngle, float yShift)
        {
            /*
            OWOSensation sensation = OWOSensation.ShotEntry;
            // two parameters can be given to the pattern to move it on the vest:
            // 1. An angle in degrees [0, 360] to turn the pattern to the left
            // 2. A shift [-0.5, 0.5] in y-direction (up and down) to move it up or down
            if ((xzAngle < 90f))
            {
                if (yShift >= 0f) OWO.Send(sensation, OWOMuscle.Pectoral_Right);
                else OWO.Send(sensation, OWOMuscle.Abdominal_Right);
            }
            if ((xzAngle > 90f) && (xzAngle < 180f))
            {
                if (yShift >= 0f) OWO.Send(sensation, OWOMuscle.Dorsal_Right);
                else OWO.Send(sensation, OWOMuscle.Lumbar_Right);
            }
            if ((xzAngle > 180f) && (xzAngle < 270f))
            {
                if (yShift >= 0f) OWO.Send(sensation, OWOMuscle.Dorsal_Left);
                else OWO.Send(sensation, OWOMuscle.Lumbar_Left);
            }
            if ((xzAngle > 270f))
            {
                if (yShift >= 0f) OWO.Send(sensation, OWOMuscle.Pectoral_Right);
                else OWO.Send(sensation, OWOMuscle.Abdominal_Right);
            }
            */
            if ((xzAngle < 180f))
            {
                PlayBackFeedback("Hit_Front");
            }
            else PlayBackFeedback("Hit_Back");
        }

        public void Recoil(bool isRightHand, bool isTwoHanded)
        {
            if (isTwoHanded)
            {
                PlayBackFeedback("Recoil_both");
                return;
            }
            if (isRightHand) PlayBackFeedback("Recoil_R");
            else PlayBackFeedback("Recoil_L");
        }

        public void PlayBackFeedback(string feedback)
        {
            if (FeedbackMap.ContainsKey(feedback))
            {
                OWO.Send(FeedbackMap[feedback]);
            }
            else LOG("Feedback not registered: " + feedback);
        }

    }
}
