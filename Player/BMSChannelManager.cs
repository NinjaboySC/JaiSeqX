﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.Player.BassBuff;
using System.IO;

namespace JaiSeqX.Player
{
    public class BMSChannel
    {
        SoundEffectInstance[] voices;
        public SoundEffectInstance LastVoice;
       
        public int ActiveVoices;
        public byte lastBendMode = 12;
        private double inBendValue = 1;
        private double inBendValue2 = 1;
        public double bendValue {
            get
            {
                return inBendValue;
            }
            set
            {
                for (int i=0; i < 8; i++ )
                {
                    var fucktheratboys = voices[i];
                    if (fucktheratboys!=null)
                    {
                        fucktheratboys.Pitch = (float)(fucktheratboys.mPitchBendBase * value * inBendValue2 ); 
                    }
                }
                inBendValue = value; 
            }            
        }


        public double bendValue2
        {
            get
            {
                return inBendValue2;
            }
            set
            {
                for (int i = 0; i < 8; i++)
                {
                    var fucktheratboys = voices[i];
                    if (fucktheratboys != null)
                    {
                        fucktheratboys.Pitch = (float)(fucktheratboys.mPitchBendBase * value*inBendValue );
                    }
                }
                inBendValue = value;
            }
        }

        public BMSChannel()
        {
            voices = new SoundEffectInstance[16]; // Should only ever have 8 voices, but still. 
        }

        public void silence()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i]!=null)
                {
                    voices[i].Stop();
                }
            }
        }


        public bool addVoice(int index, SoundEffectInstance voice) // to add a voice to a channel
        {
            if (index < voices.Length) // Make sure the index isn't stupid.
            {
                if (voices[index] != null) // Check if we already have a sound playing there.
                {
                    stopVoice(index); // Stop it if we do. 
                }
                //voice.Pitch = (float)(voice.mPitchBendBase * inBendValue;
                // Reset pitch bend???
                
                voices[index] = voice; // Throw the voice into its index
                LastVoice = voice;
                ActiveVoices++;
                if (lastBendMode != 2)
                {
                    inBendValue = 1;
                }
                voice.Pitch = (float)(voice.mPitchBendBase * inBendValue);
                return true; // success
            }
            return false;
        }

        public bool stopVoice(int index)
        {
            if (index < voices.Length)
            { // Make sure the index isnt stupid
                if (voices[index] != null) // dont do any work if we dont have anything to do
                {
                    var voi = voices[index]; // grab the voice
                    voi.Stop(); // Stop the voice
                    voi.Dispose(); // feed it to GC
                    voices[index] = null; // clear its index in the voice table
                    ActiveVoices--;
                    return true; // good
                    
                }
            }
            return false; // we didnt do anything
        }

    }

    public class BMSChannelManager
    {
        SoundEffect[] Cache; // Sound cache
        string[] CacheStrings; // Maps the sound path to the cache index 
        public BMSChannel[] channels; // current channels
        int cacheHigh; // Highest number in the cache we have

        bool[] bending;

        double[] bendtarget;
        int[] bendtargetricks;
        int[] bendticks;
        int[] bendTargetInt;
        byte[] bendOctaves;

        float[] bendPitchBase;


        private byte[] bendCoefLUT;
        public BMSChannelManager()
        {
            
            Cache = new SoundEffect[1024]; // I HOPE that the engine doesn't need more than 1024 sounds at once.
            CacheStrings = new string[1024]; // ^

            channels = new BMSChannel[32];  // Usually no more than 16 channels.  Again, just to be safe

            bendtarget = new double[32];
            bending = new bool[32];
            bendPitchBase = new float[32];
            bendticks = new int[32];
            bendtargetricks = new int[32];
            bendTargetInt = new int[32];
            bendOctaves = new byte[32];


            bendCoefLUT = new byte[100];

            bendCoefLUT[12] = 2;
            bendCoefLUT[8] = 2;
            bendCoefLUT[2] = 12;

            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = new BMSChannel();  // Preallocating the channels
                bendOctaves[i] = 12;
            }

        }


        public float Lerp(float firstFloat, float secondFloat, float by)
        {
             return firstFloat * (1 - by) + secondFloat * by;
        }

        public bool doPitchBend(byte channel, double bend, int duration, byte type, int target, byte oct)
        {
            var chn = channels[channel];

            if (chn.LastVoice != null)
            {
                var voi = chn.LastVoice;
               //  Console.WriteLine("Add PitchBend: {0} {1} {2} ", channel, duration, bend);
            
                bending[channel] = true; // fuck
                bendticks[channel] = 0; // fuck
                bendtargetricks[channel] = duration; // fuck 

                //Console.WriteLine("Add Target {0} ", target);

                bendtarget[channel] = bend; // fuck
                bendTargetInt[channel] = target;
                bendOctaves[channel] = oct;

                return true;
            }

            return false;
        }


        private static void writeChannelWaveInfo(string path, int channel)
        {
            using (StreamWriter outputFile = new StreamWriter("BMSwavs.txt", true))
            {
                outputFile.WriteLine("channel {1} loads {0}", path, channel);
            }
        }

        public bool onTick()
        {
            for (int chn = 0; chn < channels.Length; chn++)
            {
                // bend 


                var bendChannel = channels[chn];
                bendChannel.lastBendMode = bendOctaves[chn];
                if (bending[chn])
                {
                  
                    bendticks[chn]++;
                    var targetTicks = bendtargetricks[chn];
                    /*
                  
                    var ticks = bendticks[chn];
                    var targetTicks = bendtargetricks[chn];
                    if (ticks > targetTicks)
                    {
                        bending[chn] = false;
                    }
                    float bendPercent = ((float)ticks / targetTicks) < 1 ? ((float)ticks / targetTicks) : 1;
                    double semitones = bendtarget[chn] * bendPercent;
                    double finalBendValue = Math.Pow(2, ((semitones * )));
                    */
                 
                    var ticks = bendticks[chn];
                    double semitones = bendTargetInt[chn];
                    //double semitones = bendtarget[chn];
                    //double finalValue = Math.Pow(2 , (semitones * bendOctaves[chn]) / 12);
                    int bendCoef = bendCoefLUT[bendOctaves[chn]];
                    double finalValue = Math.Pow(2, ((semitones)) / (4096 *  bendCoef )); // I DONT KNOW WHATS GOING ON ANY MORE
                    //Console.WriteLine(targetTicks);
                    bendChannel.bendValue = Lerp((float)bendChannel.bendValue, (float) finalValue, 1f);

                    if (ticks > targetTicks)
                    {
                        bending[chn] = false;
                    }

                }

            }

            return true;
        }
        public SoundEffect loadSound(string file, bool lo, int ls, int le, int csub)
        {
            for (int i = 0; i < CacheStrings.Length; i++)
            {
                if (CacheStrings[i] == null || i > cacheHigh) // if we've hit null, we've hit the end of our array. 
                {
                    break;  // So just stop the loop
                }
                else
                {
                    if (CacheStrings[i] == file) // If we find it.
                    {
                        return Cache[i];  //  Return the same index of the cache (will be our file)
                    }
                }
            }
#if DEBUG
            var b = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("loadSound {0} for channel {1}", file, csub);
            Console.ForegroundColor = b;
            writeChannelWaveInfo(file, csub);
           
#endif
            CacheStrings[cacheHigh] = file; // otherwise, it's not loaded. so we need to store it in our cache
            Cache[cacheHigh] = new SoundEffect(file, lo, ls, le); // Load the WAV for it.
            var ret = Cache[cacheHigh];  // Then set our return value (we store it, because we increment cacheHigh below)
            cacheHigh++; // Increment our next cache index.

            return ret; // Return our object.
        }

        public void startVoice(SoundEffectInstance snd, byte channel, byte voice)
        {
            if (channels[channel] != null) // check if the channel exists
            {
                var chn = channels[channel]; // if it does, reference it
                chn.addVoice(voice, snd); // store the voice
            }
        }

        public void silenceChannel(byte channel)
        {
            if (channels[channel] != null) // check if the channel exists
            {
                var chn = channels[channel]; // if it does, reference it
                chn.silence();
            }
        }
     
        public void stopVoice(byte channel, byte voice)
        {
            // see above, inverse.
            if (channels[channel] != null)
            {
                var chn = channels[channel];
                chn.stopVoice(voice);
            }
        }
    }
}
