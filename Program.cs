﻿


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JaiSeqX.JAI;
using JaiSeqX.JAI.Seq;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SdlDotNet.Core;
using Be.IO;
using JaiSeqX.JAI.Loaders;


namespace JaiSeqX
{
    public static class JaiSeqX
    {
     
       
        static void Main(string[] args)
        {

#if DEBUG
            args = new string[4];
            args[0] = "visu";
            args[1] = "GCKart.baa";
            args[2] = "0";
            args[3] = "iplrom.bms.bak";
#endif
            var data = File.ReadAllBytes(args[1]);

            JASystemLoader.loadJASystem(ref data);

           
            Console.ReadLine();
          

        }
    }
}
