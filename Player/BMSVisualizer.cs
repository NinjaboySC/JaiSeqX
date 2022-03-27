﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SdlDotNet.Core;
using SdlDotNet.Graphics;
using System.Drawing;
using System.Runtime;
using System.Runtime.InteropServices;
using SdlDotNet.Input;
using System.Diagnostics;

namespace JaiSeqX.Player
{
    static class BMSVisualizer
    {
    
        static Thread RenderThread; // 
        static Surface IVideoSurface;
        static SdlDotNet.Graphics.Font myfont;
        static Process me;        
        public static void Init()
        {

            Events.TargetFps = 60; // 60 fps
            Events.Quit += (QuitEventHandler); // pizzaz, to make it so when we press the exit button it does.
            Events.Tick += (draw); // Drawing handler
            Events.KeyboardDown += KeyDown; // For channel mutes.
            
            myfont = new SdlDotNet.Graphics.Font("tahoma.ttf", 12); // Load the font
            RenderThread = new Thread(new ThreadStart(startDrawWindowThread)); // Create render thread
            me = Process.GetCurrentProcess();
            RenderThread.Start(); // Start it
        }

        private static void startDrawWindowThread()
        {
            IVideoSurface = Video.SetVideoMode(800, 800, 16, false, false, false, true); // Initialize video surface, 800x800, 16 bit
            Events.Run(); // Start the drawing / event loop
        }
        private static void KeyDown(object sender, KeyboardEventArgs kbe) 
        {                   // `, 1, 2, 3, 4, 5, 6, 7, 8, 9, q, w, e, r, t, y, u
            int[] keyOrder = {96, 49, 50, 51, 52, 53, 54, 55, 56, 57, 113, 119, 101, 114, 116, 121, 117};
            //var channel = (byte)kbe.Key - 97; // "A" , 97 is A. the alphabet continues upward in sequence, so any other letter above that will give us a range in A.

            var channel = -1;
            if (Array.IndexOf(keyOrder, (byte)kbe.Key) != -1)
            {
                channel = Array.IndexOf(keyOrder, (byte)kbe.Key);
            }
            
            Console.WriteLine("key {0}",channel);
            if (channel == -79)
            {
                BMSPlayer.ppqn--;
                BMSPlayer.updateTempo();

            } else if (channel==-80) {
                BMSPlayer.ppqn++;
                BMSPlayer.updateTempo();
     
            }
            else if (channel == -78)
            {
                BMSPlayer.bpm++;
                BMSPlayer.updateTempo();

            }
            else if (channel == -77)
            {
                BMSPlayer.bpm--;
                BMSPlayer.updateTempo();

            }


            if (channel > 0 & channel < BMSPlayer.mutes.Length) // Don't go below array bounds or mute track 0.
            {
                BMSPlayer.mutes[channel] = !BMSPlayer.mutes[channel]; // Toggle mute for that channel
                if (BMSPlayer.mutes[channel])
                {
                    BMSPlayer.ChannelManager.silenceChannel((byte)channel); // If we mute it, silence it too so we don't continue playing it.
                }
            }
        }

        private static void draw(object sender, TickEventArgs args)
        {
            IVideoSurface.Fill(Color.Black); // flush background black
            
            Point HeaderPos = new Point(5, 5); // Point for header drawing
            var HeaderText = myfont.Render("JaiSeqX by XayrGA ", Color.White); // yay me.

            Point RAMPos = new Point(150, 5); // Point for header drawing
           
            var ramstring = string.Format("MEM: {0}MB",me.PrivateMemorySize / (1024*1024));
            var RAMText = myfont.Render(ramstring, Color.White); // yay me.
            Video.Screen.Blit(RAMText, RAMPos);
            RAMText.Dispose();

            Point bpmpos = new Point(230, 5); // Point for header drawing
            var bpmstring = string.Format("BPM: {0}bpm", BMSPlayer.bpm);
            var bpmText = myfont.Render(bpmstring, Color.White); // yay me.
            Video.Screen.Blit(bpmText, bpmpos);
            bpmText.Dispose();


            Point ppqnpos = new Point(320, 5); // Point for header drawing
            var ppqnstring = string.Format("ppqn: {0}ppqn", BMSPlayer.ppqn);
            var ppqnText = myfont.Render(ppqnstring, Color.White); // yay me.
            Video.Screen.Blit(ppqnText, ppqnpos);
            ppqnText.Dispose();

            Video.Screen.Blit(HeaderText, HeaderPos); // Render it to the screen
            HeaderText.Dispose(); // Don't need it any more.
            RAMText.Dispose();
          
            for (int i = 0; i < BMSPlayer.subroutine_count; i++)
            {
                Point dest = new Point(5, (i * 35) + 30); // Position for channel indicator.
                Point dest2 = new Point(5, (i * 35) + 42); // Position for delay

                new SdlDotNet.Graphics.Primitives.Box(new Point(0, (i * 35) + 25) , new Size(800, 2)).Draw(IVideoSurface, Color.White, false, true); // Long line separating voices and track indicators.
                

                Surface surfVar;
                Surface surfVar2 = myfont.Render("", Color.Red);

                if (BMSPlayer.halts[i]==true)
                {
                    surfVar = myfont.Render("Track " + i + ": HALTED", Color.Red);
                    
                } else if(BMSPlayer.mutes[i] == true)
                    {
                        surfVar = myfont.Render("Track " + i + ": MUTED", Color.Yellow);
                    }
                else
                {
                    var col = Color.White;
                    if (BMSPlayer.updated[i] > 0)
                    {
                        BMSPlayer.updated[i]--;
                        col = Color.Green;
                    }
                    var CSubState = BMSPlayer.subroutines[i].State;
                    //var text2 = string.Format("Vel {3:X} | Note {0:X}, Bank {1:X}, Program {2:X} Delay {4} ActiveVoices {5}",CSubState.note,CSubState.voice_bank,CSubState.voice_program,CSubState.vel,CSubState.delay,BMSPlayer.ChannelManager.channels[i].ActiveVoices);
                    var text2 = string.Format("DEL {0}",CSubState.delay);
                    var text = string.Format("0x{0:X}", CSubState.current_address);
                    surfVar = myfont.Render("Track " + i + " (" + text + ")" , col);
                    surfVar2 = myfont.Render(text2, col);
                    for (int sub = 0; sub < BMSPlayer.ChannelManager.channels[i].ActiveVoices; sub++)
                    {
                        var sdest = dest;
                        sdest.Offset(140 + 27 * sub,0 );
                        var wtf = new SdlDotNet.Graphics.Primitives.Box(sdest, new Size(25, 25));
                        wtf.Draw(IVideoSurface, Color.Red, false, true);
                    }

                    var sdest2 = dest;
                    sdest2.Offset(140 + 300 + (int)(BMSPlayer.ChannelManager.channels[i].bendValue * 150) , 0);
                    var wtf2 = new SdlDotNet.Graphics.Primitives.Box(sdest2, new Size(25, 25));
                    wtf2.Draw(IVideoSurface, Color.Red, false, true);

                   



                }

                new SdlDotNet.Graphics.Primitives.Box(new Point(135, 0), new Size(2, 800)).Draw(IVideoSurface, Color.White, false, true); // Controls each line
                Video.Screen.Blit(surfVar, dest);
                Video.Screen.Blit(surfVar2, dest2);
                surfVar.Dispose();
                surfVar2.Dispose();

            }
            IVideoSurface.Update(); // Update the video surface. 
        }
        private static void QuitEventHandler(object sender, QuitEventArgs args)
        {
            Environment.Exit(0x00);
            Events.QuitApplication();
        }
    }
}
