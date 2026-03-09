using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SantronChart;

namespace SantronWinApp
{
    /// <summary>
    /// Central place to define which lanes to show per test, with colors/scales/units
    /// and whether the test requires live video UI.
    /// Channels use the canonical order emitted by SignalProcessor:
    ///   0 PVES, 1 PABD, 2 PDET (derived), 3 VINF, 4 QVOL, 5 FRATE/UPP, 6 EMG
    /// </summary>
    public sealed class TestChannelManager
    {
        public sealed class TestDefinition
        {
            public string Name { get; set; }
            public bool RequiresVideo { get; set; }
            public int[] Indices { get; set; }                     // which columns from SampleFrame.Values
            public List<MultiChannelLiveChart.Channel> Lanes { get; set; } // UI lanes with label/color/scale/unit
        }

        private readonly ScaleAndColorModel _setup;

        // canonical units
        private const string UNIT_P = "cmH2O";
        private const string UNIT_VOL = "ml";
        private const string UNIT_RATE = "ml/s";
        private const string UNIT_EMG = "uV";

        public TestChannelManager(ScaleAndColorModel setup)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
        }

        // ---- public API ----------------------------------------------------

        public TestDefinition GetDefinition(string testName)
        {
            var t = (testName ?? "").Trim();

            // Normalize a few common variants
            string key = t.ToLowerInvariant();

            switch (key)
            {
                case "uroflowmetry":
                    return Uroflow(false);
                case "uroflowmetry + emg":
                case "uroflowmetry+emg":
                    return Uroflow(true);

                case "cystometry":
                    return Cystometry();
                case "cystometry + video":
                case "cystometry+video":
                    return Cystometry();

                case "pressure flow":
                case "pressure-flow":
                    return PressureFlow(false);
                case "pressure flow + emg":
                case "pressure-flow + emg":
                case "pressure flow+emg":
                    return PressureFlow(true);
                case "pressure flow + video":
                case "pressure-flow + video":
                    return PressureFlow(false);
                case "pressure flow + emg + video":
                case "pressure-flow + emg + video":
                    return PressureFlow(true);

                case "upp":
                    return UPP();

                case "whitaker test":
                case "whitaker":
                    return Whitaker();

                case "biofeedback":
                    return Biofeedback();

                case "anal manometry":
                case "anal":
                    return AnalManometry();

                // sensible full default (all lanes visible)
                default:
                    return FullSet();
            }
        
        }

  

        private TestDefinition Uroflow(bool withEmg)
        {
            var idx = withEmg
                ? new[] { (int)ChannelCol.QVOL, (int)ChannelCol.FRATE_OR_UPP, (int)ChannelCol.EMG }
                : new[] { (int)ChannelCol.QVOL, (int)ChannelCol.FRATE_OR_UPP };

            var lanes = new List<MultiChannelLiveChart.Channel>
            {
                CreateChannel(_setup.ChannelSix, _setup.ColorSix, _setup.PlotScaleSix, "ml"), //Qvol
                CreateChannel(_setup.ChannelSeven, _setup.ColorSeven, _setup.PlotScaleSeven, "ml/sec") //Qrate
            };

            if (withEmg)
                lanes.Add(CreateChannel(_setup.ChannelEight, _setup.ColorEight, _setup.PlotScaleEight, "uV")); //EMG

            lanes = lanes.Where(l => l != null).ToList();

            return Def("Uroflowmetry" + (withEmg ? " + EMG" : ""), false, idx, lanes);
        }



        private TestDefinition Cystometry()
        {
            var idx = new[] { (int)ChannelCol.PVES, (int)ChannelCol.PABD, (int)ChannelCol.PDET_or_PCLO_or_PRPG, (int)ChannelCol.VINF };
            var lanes = new List<MultiChannelLiveChart.Channel>
            {
                CreateChannel(_setup.ChannelZero, _setup.ColorZero, _setup.PlotScaleZero, "cmH2O"), //Pves
                CreateChannel(_setup.ChannelOne, _setup.ColorOne, _setup.PlotScaleOne, "cmH2O"), //Pabd
                CreateChannel(_setup.ChannelTwo, _setup.ColorTwo, _setup.PlotScaleTwo, "cmH2O"), //Pdet
                CreateChannel(_setup.ChannelFive, _setup.ColorFive, _setup.PlotScaleFive, "ml"), //Vinf
            };
            lanes = lanes.Where(l => l != null).ToList();

            return Def("Cystometry", false, idx, lanes);
        }


        private TestDefinition PressureFlow(bool withEmg)
        {
            var idx = withEmg
                ? new[] { (int)ChannelCol.PVES, (int)ChannelCol.PABD, (int)ChannelCol.PDET_or_PCLO_or_PRPG, (int)ChannelCol.VINF, (int)ChannelCol.QVOL, (int)ChannelCol.FRATE_OR_UPP, (int)ChannelCol.EMG }
                : new[] { (int)ChannelCol.PVES, (int)ChannelCol.PABD, (int)ChannelCol.PDET_or_PCLO_or_PRPG, (int)ChannelCol.VINF, (int)ChannelCol.QVOL, (int)ChannelCol.FRATE_OR_UPP };

            var lanes = new List<MultiChannelLiveChart.Channel>
            {
                CreateChannel(_setup.ChannelZero, _setup.ColorZero, _setup.PlotScaleZero, "cmH2O"), //Pves
                CreateChannel(_setup.ChannelOne, _setup.ColorOne, _setup.PlotScaleOne, "cmH2O"), //Pabd
                CreateChannel(_setup.ChannelTwo, _setup.ColorTwo, _setup.PlotScaleTwo, "cmH2O"), //Pdet
                CreateChannel(_setup.ChannelFive, _setup.ColorFive, _setup.PlotScaleFive, "ml"), //Vinf
                CreateChannel(_setup.ChannelSix, _setup.ColorSix, _setup.PlotScaleSix, "ml"), //Qvol
                CreateChannel(_setup.ChannelSeven, _setup.ColorSeven, _setup.PlotScaleSeven, "ml/sec") //Qrate
                
            };
            if (withEmg)
                lanes.Add(CreateChannel(_setup.ChannelEight, _setup.ColorEight, _setup.PlotScaleEight, "uV")); //EMG

            lanes = lanes.Where(l => l != null).ToList();

            return Def("Pressure Flow" + (withEmg ? " + EMG" : ""), false, idx, lanes);
        }

        // --- UPP (PVES, PURA, PCLO). Show length (raw ch 5) as numeric widget, not a lane.
        //Commint on 02/12/2025 
        //private TestDefinition UPP()
        //{
        //    var idx = new[] { (int)ChannelCol.PVES, (int)ChannelCol.PURA, (int)ChannelCol.PDET_or_PCLO_or_PRPG }; // use lane name "PCLO"
        //    var lanes = new List<MultiChannelLiveChart.Channel>
        //    {
        //       CreateChannel(_setup.ChannelZero, _setup.ColorZero, _setup.PlotScaleZero, "cmH2O"), //Pves
        //       CreateChannel(_setup.ChannelThree, _setup.ColorThree, _setup.PlotScaleThree, "cmH2O"), //Pura
        //       CreateChannel(_setup.ChannelFour, _setup.ColorFour, _setup.PlotScaleFour, "cmH2O") //Pclo
        //    };

        //    lanes = lanes.Where(l => l != null).ToList();

        //    return Def("UPP", false, idx, lanes);
        //}

        //This Code and New Plot channel on 02/12/2025
        private TestDefinition UPP()
        {
            var idx = new[] { (int)ChannelCol.PVES, (int)ChannelCol.PURA, (int)ChannelCol.PDET_or_PCLO_or_PRPG, (int)ChannelCol.FRATE_OR_UPP }; // use lane name "PCLO"
            var lanes = new List<MultiChannelLiveChart.Channel>
            {
               CreateChannel(_setup.ChannelZero, _setup.ColorZero, _setup.PlotScaleZero, "cmH2O"), //Pves
               CreateChannel(_setup.ChannelThree, _setup.ColorThree, _setup.PlotScaleThree, "cmH2O"), //Pura
               CreateChannel(_setup.ChannelFour, _setup.ColorFour, _setup.PlotScaleFour, "cmH2O"), //Pclo
               CreateChannel("Lura", "", "0-30", "cms") //Add Lura and Change on "OnPaint" Method for (MultiChannelLiveChart)
            };

            lanes = lanes.Where(l => l != null).ToList();

            return Def("UPP", false, idx, lanes);
        }

       


        private TestDefinition Whitaker()
        {
            var idx = new[] { (int)ChannelCol.PVES, (int)ChannelCol.PABD, (int)ChannelCol.PDET_or_PCLO_or_PRPG }; // PABD lane relabeled “PIRP”
            var lanes = new List<MultiChannelLiveChart.Channel>
            {
               CreateChannel(_setup.ChannelZero, _setup.ColorZero, _setup.PlotScaleZero, "cmH2O"), //Pves
               CreateChannel(_setup.ChannelTen, _setup.ColorTen, _setup.PlotScaleTen, "cmH2O"), //Pirp
               CreateChannel(_setup.ChannelNine, _setup.ColorNine, _setup.PlotScaleNine, "cmH2O"), //Prpg
            };

            lanes = lanes.Where(l => l != null).ToList();

            return Def("Whitaker", false, idx, lanes);
        }

        private TestDefinition Biofeedback()
        {
            var idx = new[] { (int)ChannelCol.EMG };
            var lanes = new List<MultiChannelLiveChart.Channel> 
            {
                CreateChannel(_setup.ChannelEight, _setup.ColorEight, _setup.PlotScaleEight, "uv"), // EMG
            };

            lanes = lanes.Where(l => l != null).ToList();

            return Def("Biofeedback", false, idx, lanes);
        }



        private TestDefinition AnalManometry()
        {
            var idx = new[] { (int)ChannelCol.PVES, (int)ChannelCol.PABD, (int)ChannelCol.PURA };
            var lanes = new List<MultiChannelLiveChart.Channel>
            {
                CreateChannel(_setup.ChannelTwelve, _setup.ColorTwelve, _setup.PlotScaleTwelve, "cmH2O"), // Pa1
                CreateChannel(_setup.ChannelThirteen, _setup.ColorThirteen, _setup.PlotScaleThirteen, "cmH2O"), // Pa2
                CreateChannel(_setup.ChannelFourteen, _setup.ColorFourteen, _setup.PlotScaleFourteen, "cmH2O"), // Pa3
            };

            lanes = lanes.Where(l => l != null).ToList();

            return Def("Anal Manometry", false, idx, lanes);
        }


        private TestDefinition FullSet()
        {
            var idx = new[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            var lanes = new List<MultiChannelLiveChart.Channel>();

            lanes.Add(CreateChannel(_setup.ChannelZero, _setup.ColorZero, _setup.PlotScaleZero, "cmH2O")); //Pves
            lanes.Add(CreateChannel(_setup.ChannelOne, _setup.ColorOne, _setup.PlotScaleOne, "cmH2O")); //Pabd
            lanes.Add(CreateChannel(_setup.ChannelTwo, _setup.ColorTwo, _setup.PlotScaleTwo, "cmH2O")); //Pdet
            lanes.Add(CreateChannel(_setup.ChannelFive, _setup.ColorFive, _setup.PlotScaleFive, "ml")); //Vinf
            lanes.Add(CreateChannel(_setup.ChannelSix, _setup.ColorSix, _setup.PlotScaleSix, "ml")); //Qvol
            lanes.Add(CreateChannel(_setup.ChannelSeven, _setup.ColorSeven, _setup.PlotScaleSeven, "ml/sec")); //Flow
            lanes.Add(CreateChannel(_setup.ChannelEight, _setup.ColorEight, _setup.PlotScaleEight, "uV")); //EMG
            lanes.Add(CreateChannel(_setup.ChannelThree, _setup.ColorThree, _setup.PlotScaleThree, "cmH2O"));//Pura

            // Remove nulls if any channel name is missing
            lanes = lanes.Where(l => l != null).ToList();

            return Def("CystoUroflowEMG (Full)", false, idx, lanes);
        }

        private static MultiChannelLiveChart.Channel CreateChannel(string name, string colorHex, string scale, string unit)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // Default color if not set
            string color = string.IsNullOrWhiteSpace(colorHex) ? "#000000" : colorHex;

            // Parse scale like "0-200"

            
            var (min, max) = ParseScale(scale);
            if (name.Contains("EMG"))
            { min = -max; }
            return new MultiChannelLiveChart.Channel(name, ColorTranslator.FromHtml(color), min, max, unit);
        }


        // ---- helpers -------------------------------------------------------
        // Choose saved scale if present, otherwise fallback (expects "min-max" text)
        private static string ScaleOr(string fallback, string chosen)
        {
            return !string.IsNullOrWhiteSpace(chosen) ? chosen : fallback;
        }
       
        private static MultiChannelLiveChart.Channel Lane(string name, string hex, string scale, string unit)
        {
            var parts = scale.Split('-');
            double min = double.Parse(parts[0]);
            double max = double.Parse(parts[1]);
            return new MultiChannelLiveChart.Channel(name, ColorTranslator.FromHtml(hex), min, max, unit);
        }

        private static TestDefinition Def(string name, bool requiresVideo, int[] idx, List<MultiChannelLiveChart.Channel> lanes)
            => new TestDefinition { Name = name, RequiresVideo = requiresVideo, Indices = idx, Lanes = lanes };



        private static (double min, double max) ParseScale(string scaleText)
        {
            if (string.IsNullOrWhiteSpace(scaleText)) return (0, 100);
            var parts = scaleText.Split('-');
            if (parts.Length == 1 && double.TryParse(parts[0], out var only)) return (0, only);
            double min = 0, max = 100;
            double.TryParse(parts[0], out min);
            double.TryParse(parts.Length > 1 ? parts[1] : "100", out max);
            return (min, max);
        }

        private static Color ColorOr(string fallbackHex, string chosenHex)
        {
            string hex = !string.IsNullOrWhiteSpace(chosenHex) ? chosenHex : fallbackHex;
            return ColorTranslator.FromHtml(hex);
        }

        
    }
}
