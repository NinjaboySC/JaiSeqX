﻿

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI.Seq;
using JaiSeqX.JAI;
using System.IO;
using System.Threading;
using JaiSeqX.Player.BassBuff;
using SdlDotNet.Core;
using System.Diagnostics;


namespace JaiSeqX.Player
{
    public static class BMSPlayer
    {
        static byte[] BMSData; // byte data of our music
        public static int bpm; // beats per minute
        public static int ppqn; // pulses per quarter note
        public static double ticklen; // how long it takes before the thread continues
        public static Subroutine[] subroutines; // Container for our subroutines
        static Thread playbackThread; // read name
        public static bool[] halts; // Halted tracks
        public static bool[] mutes;  // Muted tracks
        public static float[] pans;
        public static float[] volumes;
        public static int[] updated; 
        public static int subroutine_count; // Internal counter for track ID (for creating new tracks)
        public static BMSChannelManager ChannelManager;
        static AABase AAF;
        private static Stopwatch tickTimer;
        private static int allticks = 0;

        public static bool startTrack = false; // hack hack hack 
        public static bool traceTicks = false;
       

        public static void LoadBMS(string file, ref AABase AudioData)
        {
          
            AAF = AudioData; // copy our data into here. 
            BMSData = File.ReadAllBytes(file); // Read the BMS file
            subroutines = new Subroutine[32]; // Initialize subroutine array. 
            halts = new bool[32];
            mutes = new bool[32];
            updated = new int[32];
            pans = new float[32];
            volumes = new float[32];

            for (int i=0; i < volumes.Length; i++)
            {
                volumes[i] = 1f;
            }

            ChannelManager = new BMSChannelManager();
            bpm = 1000; // Dummy value, should be set by the root track
            ppqn = 10; // Dummmy, ^ 
            
            updateTempo(); // Generate the trick length, also dummy.
            // Initialize first track.
            var root = new Subroutine(ref BMSData, 0x00); // Should always start at 0x000 of our data.
            subroutine_count = 1; 
            subroutines[0] = root; // stuff it into the subroutines array. 
            tickTimer = new Stopwatch();
        
            playbackThread = new Thread(new ThreadStart(doPlayback));
            playbackThread.Start(); // go go go
        }

       
        public static void updateTempo()
        {
            try {

           

                ticklen = (60000f / (float)(bpm)) / ((float)ppqn);    // lots of divison :D  
                allticks = (int)(tickTimer.ElapsedMilliseconds / ticklen);
                Console.WriteLine("new TL {0} at T {1}", ticklen, allticks);
                // Console.WriteLine("new ticksize {0} {1} {2}", ticklen, bpm, ppqn);

            } catch
            {
                // uuuuUUGH. ZERO. 
            }
        }

        private static void doPlayback()
        {
            tickTimer.Start();
            Engine.Init();  // Initialize the audio engine
            //while (startTrack == false) { }
            while (true)
            {
                trySequencerTick();
                Thread.Sleep(2);
            }
        }

        private static void trySequencerTick()
        {
            var ts = tickTimer.ElapsedMilliseconds;
            var tt_n = ts / ticklen;
            var totalTick = 0;
            while (allticks < tt_n) {
                if (totalTick == 0 && startTrack == false)
                {
                    try
                    {

                        tt_n = ts / ticklen; // whoops, update timing every tick just in case timing changes.
                        sequencerTick(); // run the sequencer tick. 
                                         // Just going to leave this for timing.
                        if (traceTicks == true)
                        {
                            Console.WriteLine("Tick");
                        }
                        
                        totalTick++;
                        tickTimer.Stop();
                    }
                    catch (Exception E)
                    {
                        Console.WriteLine("SEQUENCER MISSED TICK");
                        Console.WriteLine(E.ToString());
                    }
                }
                else
                {
                    if (startTrack == false)
                    {
                        while (Console.ReadKey().Key != ConsoleKey.Spacebar) { }
                        startTrack = true;
                        tickTimer.Start();
                    }
                    {
                        try
                        {

                            tt_n = ts / ticklen; // whoops, update timing every tick just in case timing changes.
                            sequencerTick(); // run the sequencer tick. 
                                             // Just going to leave this for timing.
                            if (traceTicks == true)
                            {
                                Console.WriteLine("Tick2");
                            }
                            totalTick++;
                        }
                        catch (Exception E)
                        {
                            Console.WriteLine("SEQUENCER MISSED TICK");
                            Console.WriteLine(E.ToString());
                        }
                    }
                }
            }
        }


        private static void sequencerTick()
        {
        
            allticks++;


            ChannelManager.onTick();
            for (int csub = 0; csub < subroutine_count; csub++)
            {
                var current_subroutine = subroutines[csub]; // grab the current subroutine
                if (halts[csub])
                {
                    continue; // skip over this one.
                }
                var current_state = current_subroutine.State; // Just for helper


                while (current_state.delay < 1 & halts[csub]==false) // we want to go until there's a delay. A delay counts as a BREAK command, all other commands are executed inline. 
                {
                    updated[csub] = 3;

                    var opcode = current_subroutine.loadNextOp(); // loads the next opcode
                                                                  /* State machine for sequencer */



#if JSQPlayer_StepDebugging
                    Console.WriteLine("TRK {0} | POS {1:X} | OP {2}", csub, current_subroutine.State.current_address, opcode);
                    Console.ReadKey();

                    


#endif
                    switch (opcode)
                    {
                        case JaiEventType.TIME_BASE:
                            if (csub == 0)
                            {
                                bpm = current_state.bpm;
                                ppqn = current_state.ppqn;
                            }
                           
                           
                            updateTempo();

                         
                            break;
                        case JaiEventType.DEBUG:
                            //Console.WriteLine("Debug: Track is {0}",csub);
                            break;
                        case JaiEventType.NOTE_ON:
                            {
                                var bankdata = AAF.IBNK[current_state.voice_bank];
                                if (bankdata!=null)
                                {



                                    var program = bankdata.Instruments[current_state.voice_program];
                                    if (program!=null)
                                    {
                                        var note = current_state.note;
                                        var vel = current_state.vel;
                                        //Console.WriteLine("{2}: {0} {1}", note, vel,csub);
                                        var notedata = program.Keys[note]; // these are interpolated, no need for checks.
                                        //Console.WriteLine("Requestr key {0}", note);
                                        var key = notedata.keys[vel]; // These too. 
                                        // Basically, if everything is valid up to this point, we should be good. (should, at least for the IBNK)
                                        if (key!=null)
                                        {
                                            try
                                            {
                                                var wsysid = key.wsysid;
                                                var waveid = key.wave;
                                                var wsys = AAF.WSYS[wsysid];
                                                if (wsys != null)
                                                {
                                                    var wave = wsys.waves[waveid];
                                                    var sound = ChannelManager.loadSound(wave.pcmpath,wave.loop,wave.loop_start,wave.loop_end,csub).CreateInstance();
                                                   
                                                    var pmul = program.Pitch * key.Pitch;
                                                    var vmul = program.Volume * key.Volume;
                                                    //Console.WriteLine(pmul);
                                                    var real_pitch = Math.Pow(2, ( (note - wave.key)  ) / 12f) * (pmul);
                                                    var true_volume = (Math.Pow(((float)vel) / 127, 2) * vmul) * 0.5;
                                                    sound.Volume = (float)(true_volume * 0.6) * volumes[csub];
                                                    sound.ShouldFade = true;
                                                    sound.FadeOutMS = 30;
                                                    if (program.IsPercussion)
                                                    {
                                                        real_pitch = (float)(key.Pitch * program.Pitch);
                                                        
                                                        sound.ShouldFade = true;
                                                        sound.FadeOutMS = 200; // no instant stops
                                                    }
                                                    sound.Pitch = (float) (real_pitch);
                                                    sound.mPitchBendBase = (float)real_pitch;
                                                    sound.Pan = pans[csub];
                                                    ChannelManager.startVoice(sound, (byte)csub, current_state.voice);

                                                   // while (startTrack == false && Console.ReadKey().Key != ConsoleKey.Spacebar) { }
                                                    //startTrack = true; //not

                                                    if (!mutes[csub]) // The sounds are created, so they're still startable even if they're not used. 
                                                    {
                                                        sound.Play();
                                                    }

                                                } else {
                                                    Console.WriteLine("Null WSYS??");
                                                }
                                            }catch (Exception E)
                                            {
                                                var b = Console.ForegroundColor;
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine("fuuuuuck");
                                                Console.WriteLine(E.ToString());
                                                Console.ForegroundColor = b;
                                            }

                                            
                                        }
                                    } else
                                    {
                                        Console.WriteLine("Null IBNK Program");
                                    }
                                } else
                                {
                                    Console.WriteLine("Null bank data {0}", current_state.voice_bank);
                                }
                            }
                            break;
                        case JaiEventType.NOTE_OFF:
                            {
                                ChannelManager.stopVoice((byte)csub, current_state.voice);
                                break;
                            }
                        case JaiEventType.NEW_TRACK:
                            {
                                Console.WriteLine("Add New Track");
                                var ns = new Subroutine(ref BMSData, current_state.track_address);
                                subroutines[subroutine_count] = ns;
                                subroutine_count++;
                                Console.WriteLine("Subroutine Count: " + subroutine_count);
                                break;
                            }
                        case JaiEventType.HALT:
                            {

                                
                                var b = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Track {0} halted by 0xFF opcode.", csub);
                                Console.ForegroundColor = b;

                                halts[csub] = true;
                                break;
                            }
                        case JaiEventType.PERF:
                            {
                                // Console.WriteLine("PERF CHANGE: {0} {1} {2}", current_state.perf, current_state.perf_value, current_state.perf_decimal);

                                if (current_state.perf == 0)
                                {
                                    var data = current_state.perf_decimal;
                                    volumes[csub] = (float)data;
                                }
                                if (current_state.perf == 9) { }

                                if (current_state.perf==1) // Pitch bend
                                {
                                    //Console.WriteLine("Pitch bend c {0} {1} {2}", csub, current_state.perf_value, current_state.perf_duration);
                                    ChannelManager.doPitchBend((byte)csub, current_state.perf_decimal, current_state.perf_duration, current_state.perf_type, current_state.perf_value,current_subroutine.octave);
                                 
                                } 

                                if (current_state.perf==3)
                                {
                                    var data = current_state.perf_decimal;
                                    pans[csub] = (float)data - 0.1f; 
                                }
                                                                
                               
                                break;
                            }
                        case JaiEventType.PARAM:
                            {

                                Console.WriteLine("REQUEST PARAM CHANGE: TRK: {2} , {0} {1}", current_state.param, current_state.param_value,csub);
                                if (current_state.param==7)
                                {
                                    current_subroutine.octave = (byte)current_state.param_value;
                                }
                                break;
                            }
                        case JaiEventType.JUMP:
                            {
                                
                                Console.WriteLine("Track {0} jumps to 0x{1:X}", csub, current_state.jump_address);
                                current_subroutine.jump(current_state.jump_address);
                                break;
                            }
                        case JaiEventType.CALL:
                            {
                                
                                Console.WriteLine("Track {0} unconditional call to 0x{1:X}" ,csub, current_state.jump_address);
                                current_state.track_stack_depth++;
                                if (current_state.track_stack_depth > 16)
                                {
                                    Console.WriteLine("==== Sequence Crash ====");
                                    Console.BackgroundColor = ConsoleColor.Red;
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("JSequenceStack overflow: Return map size exceeded 16");
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.BackgroundColor = ConsoleColor.Black;
                                    Console.WriteLine("Track Number: {0}", csub);
                                    Console.WriteLine("Stack: \n");
                                    Helpers.printJaiSeqStack(current_subroutine);
                                    while (true) { Console.ReadLine(); }
                                }
                                current_subroutine.AddrStack.Push((uint)current_subroutine.nextOpAddress());
                                current_subroutine.jump(current_state.jump_address);
                                break;
                            }
                        case JaiEventType.RET:
                            {
                                //Console.WriteLine("Track {0} return to 0x{1:X}");
                                current_state.track_stack_depth--;
                                var retn = current_subroutine.AddrStack.Pop();
                                Console.WriteLine("Track {0} return to 0x{1:X}",csub,retn);
                                current_subroutine.jump((int)retn);
                                
                                    break;
                            }
                        case JaiEventType.DELAY: // handled internally. 
                            
                            break;
                        case JaiEventType.UNKNOWN:
                            Console.WriteLine("Non-Fatal Opcode Miss: 0x{0:X}",current_subroutine.last_opcode);
                            break;
                        case JaiEventType.UNKNOWN_ALIGN_FAIL:
                            Console.WriteLine("==== Sequence Crash ====");
                            Console.WriteLine("Track Number: {0}", csub);
                            Console.WriteLine("Stack: \n");
                            Helpers.printJaiSeqStack(current_subroutine);
                            while (true) { Console.ReadLine(); }
                            break;
                    }

                }
                if (current_state.delay > 0) { // check if the delay is over 0
                    current_state.delay--; // if it is, this executes every tick, so subtract one tick from it. 
                }
            }
        }
    }
}
