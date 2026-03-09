using System;
using System.Collections.Generic;

namespace SantronReports.LegacyMatching
{
    public sealed class VoidingPhaseMetrics
    {
        // Primary
        public double VoidedVolumeMl;
        public double AvgFlowMlPerSec;          // voided volume / voiding time
        public double CompAvgFlowMlPerSec;      // voided volume / flow time
        public double PeakFlowMlPerSec;         // Qmax
        public double CompMaxFlowMlPerSec;      // 3-point average around Qmax

        // Times (seconds)
        public int DelayTimeSec;
        public int FlowTimeSec;
        public int VoidingTimeSec;
        public int IntervalTimeSec;             // voiding time - flow time
        public int MaxTimeSec;                  // time-to-Qmax from voiding start, in seconds

        // Pressures at Qmax
        public double PvesAtQmax;
        public double PabdAtQmax;
        public double PdetAtQmax;

        // Opening pressures (at first flow start)
        public double Opves;
        public double Opabd;
        public double Opdet;

        // Indices (sample indices, before conversion)
        public int DelayIndex;
        public int FlowTimeCount;
        public int VoidingTimeCount;
        public int MaxTimeCount;
        public int PeakIndex;

        // Nomogram indices
        public double BOOI;
        public double BCI;

        // Voiding max pressures (excluding marker 9..10 blocks)
        public double MaxPvesVp;
        public double MaxPabdVp;
        public double MaxPdetVp;

        // Derived
        public double Compru;
        public double Diuresis;
        public double Micres;
        public double Pmuo;
        public double Pclosure;

        // Residuals (pass-through)
        public double ResidualUrine;
        public double PretestResidualUrine;
    }

    public static class VoidingPhaseCalculator
    {
        //    public static VoidingPhaseMetrics Compute(
        //double[,] resample,
        //int startIndex,
        //double const2Ms,
        //IReadOnlyList<(int Code, int Index)> markers,
        //double infusedVolume,
        //double leakVolume,
        //double residualUrine,
        //double pretestResidualUrine,
        //double bcFlowVolume = 0.0,
        //int arraySize = 0)
        //    {
        //        if (resample == null) throw new ArgumentNullException(nameof(resample));
        //        int n = resample.GetLength(0);
        //        int ch = resample.GetLength(1);
        //        if (n <= 0) throw new ArgumentException("resample is empty", nameof(resample));
        //        if (ch <= 5)
        //            throw new ArgumentException(
        //                "resample must include column 5 (flow rate)",
        //                nameof(resample)
        //            );

        //        // First, find the BC marker from the markers list if it exists
        //        int bcMarkerIndex = -1;
        //        if (markers != null)
        //        {
        //            foreach (var (code, index) in markers)
        //            {
        //                // You need to replace 0 with your actual BC marker code
        //                // Common values might be 1, 2, or whatever your system uses
        //                if (code == 1) // Replace 1 with your actual BC marker code
        //                {
        //                    bcMarkerIndex = index;
        //                    System.Diagnostics.Debug.WriteLine($"Found BC marker at index {index}");
        //                    break;
        //                }
        //            }
        //        }

        //        arraySize = resample.GetLength(0);

        //        var mark9 = new bool[n];
        //        var mark10 = new bool[n];

        //        bool Z1 = false;  // Flow started

        //        int delayIdx = 0;
        //        int flowTimeCnt = 0;      // Cumulative count when flow is active
        //        int voidingStartIdx = -1;  // First flow start (initialize to -1)
        //        int voidingEndIdx = -1;    // Last flow end
        //        double maxFlow = 0.0;
        //        int maxFlowIdx = 0;       // Index where max flow occurs

        //        bool isFlowing = false;   // Currently in a flow period

        //        double artivolume = 0.0;
        //        double r1vol = 0.0;
        //        double r2vol = 0.0;
        //        int r1 = 0;
        //        int r2 = 0;

        //        System.Diagnostics.Debug.WriteLine($"Original startIndex  = {startIndex}");
        //        System.Diagnostics.Debug.WriteLine($"BC Marker Index      = {bcMarkerIndex}");
        //        System.Diagnostics.Debug.WriteLine($"const2Ms             = {const2Ms}");
        //        System.Diagnostics.Debug.WriteLine($"samplesPerSec        = {1000.0 / const2Ms:F2}");

        //        if (markers != null)
        //        {
        //            foreach (var (c, i) in markers)
        //            {
        //                if (i >= 0 && i < n)
        //                {
        //                    if (c == 9) mark9[i] = true;
        //                    if (c == 10) mark10[i] = true;
        //                }
        //            }
        //        }

        //        bool tempflag = false;

        //        // Variables to track max pressures
        //        double maxPdet = 0;
        //        double maxPves = 0;
        //        double maxPabd = 0;

        //        // First pass: Find voiding start and end indices
        //        // We need to scan from beginning to find where flow actually starts
        //        for (int i = 0; i < n; i++)
        //        {
        //            if (mark9[i] || mark10[i]) continue; // Skip marker blocks

        //            double FRate = resample[i, 5];

        //            // Detect first flow start
        //            if (!Z1 && FRate >= 1.0)
        //            {
        //                voidingStartIdx = i;    // This is where flow first starts
        //                Z1 = true;
        //                System.Diagnostics.Debug.WriteLine($"Flow started at index {i}");
        //            }

        //            if (Z1 && FRate >= 1.0)
        //            {
        //                voidingEndIdx = i;  // Update last flow point
        //            }

        //            if (Z1) break; // Once we find first flow, we can break
        //        }

        //        // Reset Z1 for second pass
        //        Z1 = false;

        //        // Second pass: Calculate metrics starting from appropriate point
        //        // Determine where to start scanning based on BC marker
        //        int scanStartIndex;
        //        bool hasBCMarker = bcMarkerIndex >= 0 && bcMarkerIndex < n;

        //        if (hasBCMarker && bcMarkerIndex <= voidingStartIdx)
        //        {
        //            // BC marker exists and is before voiding starts
        //            scanStartIndex = bcMarkerIndex;
        //            System.Diagnostics.Debug.WriteLine($"Using BC marker index {bcMarkerIndex} as start");
        //        }
        //        else
        //        {
        //            // No valid BC marker, start from beginning or from voiding start
        //            scanStartIndex = 0;
        //            System.Diagnostics.Debug.WriteLine($"No valid BC marker, starting from index 0");
        //        }

        //        System.Diagnostics.Debug.WriteLine($"Has BC Marker        = {hasBCMarker}");
        //        System.Diagnostics.Debug.WriteLine($"VoidingStartIdx      = {voidingStartIdx}");
        //        System.Diagnostics.Debug.WriteLine($"Scan Start Index     = {scanStartIndex}");

        //        for (int i = scanStartIndex; i < n; i++)
        //        {
        //            if (mark9[i])
        //            {
        //                tempflag = true;
        //                r1vol = resample[i, 4];
        //                r1 = i;
        //            }

        //            if (mark10[i])
        //            {
        //                tempflag = false;
        //                r2vol = resample[i, 4];
        //                r2 = i;
        //                artivolume += (r2vol - r1vol);
        //            }

        //            if (tempflag) continue;

        //            double FRate = resample[i, 5];
        //            double pdet = resample[i, 2];
        //            double pves = resample[i, 0];
        //            double pabd = resample[i, 1];

        //            // Track maximum pressures
        //            if (pdet > maxPdet) maxPdet = pdet;
        //            if (pves > maxPves) maxPves = pves;
        //            if (pabd > maxPabd) maxPabd = pabd;

        //            // Detect first flow start for delay calculation
        //            if (!Z1 && FRate >= 1.0)
        //            {
        //                delayIdx = i;  // This is where flow starts relative to scan start
        //                Z1 = true;
        //                System.Diagnostics.Debug.WriteLine($"Flow detected at index {i} during scan");
        //            }

        //            if (Z1)
        //            {
        //                // Flow Time: Cumulative time when flow is actually happening
        //                if (!isFlowing && FRate >= 1.0)
        //                {
        //                    isFlowing = true;
        //                }

        //                if (isFlowing && FRate >= 1.0)
        //                {
        //                    flowTimeCnt++;
        //                }

        //                if (isFlowing && FRate < 1.0)
        //                {
        //                    isFlowing = false;
        //                }

        //                // Track peak flow
        //                if (FRate > maxFlow)
        //                {
        //                    maxFlow = FRate;
        //                    maxFlowIdx = i;
        //                }
        //            }
        //        }

        //        double samplesPerSec = 1000.0 / const2Ms;

        //        // Calculate Delay Time
        //        int delaySec = 0;

        //        if (hasBCMarker && bcMarkerIndex >= 0 && bcMarkerIndex < n)
        //        {
        //            if (bcMarkerIndex < voidingStartIdx)
        //            {
        //                // BC marker is before voiding starts - calculate delay from BC to voiding start
        //                delaySec = (int)Math.Round((voidingStartIdx - bcMarkerIndex) / samplesPerSec);
        //                System.Diagnostics.Debug.WriteLine($"Calculating delay: ({voidingStartIdx} - {bcMarkerIndex}) / {samplesPerSec} = {delaySec} sec");
        //            }
        //            else
        //            {
        //                // BC marker is after voiding started - delay is 0
        //                System.Diagnostics.Debug.WriteLine($"BC marker ({bcMarkerIndex}) is after voiding start ({voidingStartIdx}), delay = 0");
        //                delaySec = 0;
        //            }
        //        }
        //        else
        //        {
        //            // No BC marker - delay is from beginning to voiding start
        //            delaySec = (int)Math.Round(voidingStartIdx / samplesPerSec);
        //            System.Diagnostics.Debug.WriteLine($"No BC marker, delay from start: {voidingStartIdx} / {samplesPerSec} = {delaySec} sec");
        //        }

        //        if (delaySec < 0) delaySec = 0;

        //        // Flow Time
        //        int flowTimeSec = (int)Math.Round(flowTimeCnt / samplesPerSec);
        //        if (flowTimeSec < 0) flowTimeSec = 0;

        //        // Voiding Time
        //        int voidingTimeCnt = (voidingEndIdx - voidingStartIdx) + 1;
        //        int voidingSec = (int)Math.Round(voidingTimeCnt / samplesPerSec);
        //        if (voidingSec < 0) voidingSec = 0;

        //        // Interval Time
        //        int intervalTimeSec = voidingSec - flowTimeSec;
        //        if (intervalTimeSec < 0) intervalTimeSec = 0;

        //        // Time to peak flow
        //        int maxTimeCnt = (maxFlowIdx - voidingStartIdx);
        //        int maxTimeSec = (int)Math.Round(maxTimeCnt / samplesPerSec);
        //        if (maxTimeSec < 0) maxTimeSec = 0;

        //        int peakIdx = maxFlowIdx;

        //        // Bounds checking
        //        if (peakIdx < 0) peakIdx = 0;
        //        if (peakIdx >= n) peakIdx = n - 1;

        //        double voidedVolume = 0;
        //        if (arraySize > 0 && arraySize - 1 < resample.GetLength(0))
        //        {
        //            voidedVolume = resample[arraySize - 1, 4] - bcFlowVolume - artivolume;
        //        }

        //        if (voidedVolume < 0) voidedVolume = 0;

        //        double avgFlow = voidingSec > 0 ? voidedVolume / voidingSec : 0;
        //        double compAvgFlow = flowTimeSec > 0 ? voidedVolume / flowTimeSec : 0;

        //        // Pressures at Qmax
        //        double pvesQ = 0, pabdQ = 0, pdetQ = 0;
        //        if (peakIdx >= 0 && peakIdx < n)
        //        {
        //            pvesQ = resample[peakIdx, 0];
        //            pabdQ = resample[peakIdx, 1];
        //            pdetQ = resample[peakIdx, 2];
        //        }

        //        // Opening pressures
        //        double opPdet = 0, opPves = 0, opPabd = 0;
        //        if (voidingStartIdx >= 0 && voidingStartIdx < n)
        //        {
        //            opPdet = resample[voidingStartIdx, 2];
        //            opPves = resample[voidingStartIdx, 0];
        //            opPabd = resample[voidingStartIdx, 1];
        //        }

        //        // 3-point average max flow
        //        double compMaxFlow;
        //        if (peakIdx <= 0 || peakIdx >= n - 1)
        //        {
        //            compMaxFlow = maxFlow;
        //        }
        //        else
        //        {
        //            double sum = resample[peakIdx - 1, 5] + resample[peakIdx, 5] + resample[peakIdx + 1, 5];
        //            compMaxFlow = sum / 3.0;
        //        }

        //        // Post-void residual
        //        double compru = infusedVolume + pretestResidualUrine - voidedVolume - leakVolume;
        //        if (compru < 0) compru = 0;

        //        var result = new VoidingPhaseMetrics
        //        {
        //            VoidedVolumeMl = voidedVolume,
        //            AvgFlowMlPerSec = avgFlow,
        //            CompAvgFlowMlPerSec = compAvgFlow,
        //            PeakFlowMlPerSec = maxFlow,
        //            CompMaxFlowMlPerSec = compMaxFlow,

        //            DelayTimeSec = delaySec,
        //            VoidingTimeSec = voidingSec,
        //            FlowTimeSec = flowTimeSec,
        //            IntervalTimeSec = intervalTimeSec,
        //            MaxTimeSec = maxTimeSec,

        //            PvesAtQmax = pvesQ,
        //            PabdAtQmax = pabdQ,
        //            PdetAtQmax = pdetQ,

        //            Opdet = opPdet,
        //            Opves = opPves,
        //            Opabd = opPabd,

        //            MaxPdetVp = maxPdet,
        //            MaxPvesVp = maxPves,
        //            MaxPabdVp = maxPabd,

        //            DelayIndex = delayIdx,
        //            FlowTimeCount = flowTimeCnt,
        //            VoidingTimeCount = voidingTimeCnt,
        //            MaxTimeCount = maxTimeCnt,
        //            PeakIndex = peakIdx,

        //            BOOI = pdetQ - (2 * maxFlow),
        //            BCI = pdetQ + (5 * maxFlow),

        //            ResidualUrine = residualUrine,
        //            PretestResidualUrine = pretestResidualUrine,
        //            Compru = compru
        //        };

        //        // Print results
        //        System.Diagnostics.Debug.WriteLine("========== VOIDING PHASE METRICS ==========");
        //        System.Diagnostics.Debug.WriteLine($"Has BC Marker        = {hasBCMarker}");
        //        System.Diagnostics.Debug.WriteLine($"BC Marker Index      = {bcMarkerIndex}");
        //        System.Diagnostics.Debug.WriteLine($"VoidingStartIdx      = {voidingStartIdx}");
        //        System.Diagnostics.Debug.WriteLine($"VoidingEndIdx        = {voidingEndIdx}");
        //        System.Diagnostics.Debug.WriteLine($"Delay Calculation    = ({voidingStartIdx} - {(hasBCMarker ? bcMarkerIndex : 0)}) / {samplesPerSec:F2}");
        //        System.Diagnostics.Debug.WriteLine($"DelayTimeSec         = {result.DelayTimeSec}");
        //        System.Diagnostics.Debug.WriteLine($"VoidedVolumeMl       = {result.VoidedVolumeMl:F2}");
        //        System.Diagnostics.Debug.WriteLine($"AvgFlowMlPerSec      = {result.AvgFlowMlPerSec:F2}");
        //        System.Diagnostics.Debug.WriteLine($"CompAvgFlowMlPerSec  = {result.CompAvgFlowMlPerSec:F2}");
        //        System.Diagnostics.Debug.WriteLine($"PeakFlowMlPerSec     = {result.PeakFlowMlPerSec:F2}");
        //        System.Diagnostics.Debug.WriteLine($"CompMaxFlowMlPerSec  = {result.CompMaxFlowMlPerSec:F2}");
        //        System.Diagnostics.Debug.WriteLine($"FlowTimeSec          = {result.FlowTimeSec}");
        //        System.Diagnostics.Debug.WriteLine($"VoidingTimeSec       = {result.VoidingTimeSec}");
        //        System.Diagnostics.Debug.WriteLine($"IntervalTimeSec      = {result.IntervalTimeSec}");
        //        System.Diagnostics.Debug.WriteLine($"MaxTimeSec           = {result.MaxTimeSec}");
        //        System.Diagnostics.Debug.WriteLine("============================================");

        //        return result;
        //    }



        public static VoidingPhaseMetrics Compute(
            double[,] resample,
            int startIndex,
            double const2Ms,
            IReadOnlyList<(int Code, int Index)> markers,
            double infusedVolume,
            double leakVolume,
            double residualUrine,
            double pretestResidualUrine,
            double bcFlowVolume = 0.0,
            int arraySize = 0)
        {
            if (resample == null) throw new ArgumentNullException(nameof(resample));
            int n = resample.GetLength(0);
            int ch = resample.GetLength(1);
            if (n <= 0) throw new ArgumentException("resample is empty", nameof(resample));
            if (ch <= 5)
                throw new ArgumentException(
                    "resample must include column 5 (flow rate)",
                    nameof(resample)
                );
            if (startIndex < 0) startIndex = 0;
            if (startIndex >= n) startIndex = n - 1;

            arraySize = resample.GetLength(0);

            var mark9 = new bool[n];
            var mark10 = new bool[n];

            bool Z1 = false;  // Flow started

            int delayIdx = 0;
            int flowTimeCnt = 0;      // Cumulative count when flow is active
            int voidingStartIdx = 0;  // First flow start
            int voidingEndIdx = 0;    // Last flow end
            double maxFlow = 0.0;
            int maxFlowIdx = 0;       // Index where max flow occurs

            bool isFlowing = false;   // Currently in a flow period

            double artivolume = 0.0;
            double r1vol = 0.0;
            double r2vol = 0.0;
            int r1 = 0;
            int r2 = 0;

            System.Diagnostics.Debug.WriteLine($"startIndex       = {startIndex}");
            System.Diagnostics.Debug.WriteLine($"const2Ms         = {const2Ms}");
            System.Diagnostics.Debug.WriteLine($"samplesPerSec    = {1000.0 / const2Ms:F2}");
            System.Diagnostics.Debug.WriteLine(
                $"duration(sec)    = {arraySize / (1000.0 / const2Ms):F2}"
            );

            System.Diagnostics.Debug.WriteLine(
                $"resample dims    = [{resample.GetLength(0)} x {resample.GetLength(1)}]"
            );

            if (markers != null)
            {
                foreach (var (c, i) in markers)
                {
                    if (i >= 0 && i < n)
                    {
                        if (c == 9) mark9[i] = true;
                        if (c == 10) mark10[i] = true;
                    }
                }
            }

            bool tempflag = false;

            // Variables to track max pressures
            double maxPdet = 0;
            double maxPves = 0;
            double maxPabd = 0;

            for (int i = startIndex; i < n; i++)
            {
                if (mark9[i])
                {
                    tempflag = true;
                    r1vol = resample[i, 4];
                    r1 = i;
                }

                if (mark10[i])
                {
                    tempflag = false;
                    r2vol = resample[i, 4];
                    r2 = i;
                    artivolume += (r2vol - r1vol);
                }

                if (tempflag) continue;

                double FRate = resample[i, 5];
                double pdet = resample[i, 2];
                double pves = resample[i, 0];
                double pabd = resample[i, 1];

                // Track maximum pressures from delay period through voiding (excluding marker blocks)
                if (pdet > maxPdet) maxPdet = pdet;
                if (pves > maxPves) maxPves = pves;
                if (pabd > maxPabd) maxPabd = pabd;

                // Detect first flow start (delay time)
                if (!Z1 && FRate >= 1.0)
                {
                    delayIdx = i;           // BC mark to this point
                    voidingStartIdx = i;    // This is where flow first starts
                    Z1 = true;

                    System.Diagnostics.Debug.WriteLine($"Flow started at index {i}, Pdet = {pdet}");
                }

                if (Z1)
                {
                    // Flow Time: Cumulative time when flow is actually happening
                    // Track transitions between flowing and not flowing
                    if (!isFlowing && FRate >= 1.0)
                    {
                        // Flow just started
                        isFlowing = true;
                    }

                    if (isFlowing && FRate >= 1.0)
                    {
                        // Currently flowing - increment flow time counter
                        flowTimeCnt++;
                        voidingEndIdx = i;  // Update last flow point
                    }

                    if (isFlowing && FRate < 1.0)
                    {
                        // Flow just stopped
                        isFlowing = false;
                    }

                    // Track peak flow
                    if (FRate > maxFlow)
                    {
                        maxFlow = FRate;
                        maxFlowIdx = i;

                        System.Diagnostics.Debug.WriteLine($"New max flow {maxFlow} at index {i}, Pdet = {pdet}");
                    }
                }
            }

            double samplesPerSec = 1000.0 / const2Ms;

            // Delay Time: BC mark (startIndex) to voiding start (first flow)
            //int delaySec = (int)((voidingStartIdx - startIndex) / samplesPerSec);
            //if (delaySec < 0) delaySec = 0;
             int delaySec = (int)(startIndex / samplesPerSec);
            // Flow Time: Cumulative time when flow > 0 (sum of all flow periods)
            int flowTimeSec = (int)(flowTimeCnt / samplesPerSec) + 1;

            // Voiding Time: Total time from first flow to last flow (including gaps)
            // This is the actual voiding duration, NOT including delay time
            int voidingTimeCnt = (voidingEndIdx - voidingStartIdx) + 1;
            int voidingSec = (int)(voidingTimeCnt / samplesPerSec) + 1;

            // Interval Time: voiding time - flow time (gaps when not flowing)
            int intervalTimeSec = voidingSec - flowTimeSec;
            if (intervalTimeSec < 0) intervalTimeSec = 0;

            // Time to peak flow: from voiding start to max flow
            int maxTimeCnt = (maxFlowIdx - voidingStartIdx);
            int maxTimeSec = (int)(maxTimeCnt / samplesPerSec);

            int peakIdx = maxFlowIdx;

            // Bounds checking for peakIdx
            if (peakIdx < 0) peakIdx = 0;
            if (peakIdx >= n) peakIdx = n - 1;

            double voidedVolume =
                resample[arraySize - 1, 4]
                - bcFlowVolume
                - artivolume;

            if (voidedVolume < 0) voidedVolume = 0;

            // Avg Flow = voided volume / voiding time
            double avgFlow = voidingSec > 0 ? voidedVolume / voidingSec : 0;

            // Compute Avg Flow Rate = voided volume / flow time
            double compAvgFlow = flowTimeSec > 0 ? voidedVolume / flowTimeSec : 0;

            // Pdet at Qmax: When we have max flow, that time pdet value
            double pvesQ = resample[peakIdx, 0];
            double pabdQ = resample[peakIdx, 1];
            double pdetQ = resample[peakIdx, 2];

            // Opening flow pressure: First time flow starts, that time pdet value
            double opPdet = 0, opPves = 0, opPabd = 0;
            if (voidingStartIdx >= 0 && voidingStartIdx < n)
            {
                opPdet = resample[voidingStartIdx, 2];
                opPves = resample[voidingStartIdx, 0];
                opPabd = resample[voidingStartIdx, 1];
            }

            // Compute max flow: 3-point average (peak flow, reading before peak, reading after peak)
            double compMaxFlow;
            if (peakIdx <= 0 || peakIdx >= n - 1)
            {
                compMaxFlow = maxFlow;
            }
            else
            {
                double sum = resample[peakIdx - 1, 5] + resample[peakIdx, 5] + resample[peakIdx + 1, 5];
                compMaxFlow = sum / 3.0;
            }

            // Calculate post-void residual
            double compru = infusedVolume + pretestResidualUrine - voidedVolume - leakVolume;
            if (compru < 0) compru = 0;

            var result = new VoidingPhaseMetrics
            {
                VoidedVolumeMl = voidedVolume,
                AvgFlowMlPerSec = avgFlow,
                CompAvgFlowMlPerSec = compAvgFlow,
                PeakFlowMlPerSec = maxFlow,
                CompMaxFlowMlPerSec = compMaxFlow,

                DelayTimeSec = delaySec,
                VoidingTimeSec = voidingSec,
                FlowTimeSec = flowTimeSec,
                IntervalTimeSec = intervalTimeSec,
                MaxTimeSec = maxTimeSec,

                PvesAtQmax = pvesQ,
                PabdAtQmax = pabdQ,
                PdetAtQmax = pdetQ,

                Opdet = opPdet,
                Opves = opPves,
                Opabd = opPabd,

                MaxPdetVp = maxPdet,
                MaxPvesVp = maxPves,
                MaxPabdVp = maxPabd,

                DelayIndex = delayIdx,
                FlowTimeCount = flowTimeCnt,
                VoidingTimeCount = voidingTimeCnt,
                MaxTimeCount = maxTimeCnt,
                PeakIndex = peakIdx,

                BOOI = pdetQ - (2 * maxFlow),
                BCI = pdetQ + (5 * maxFlow),

                ResidualUrine = residualUrine,
                PretestResidualUrine = pretestResidualUrine,
                Compru = compru
            };

            System.Diagnostics.Debug.WriteLine("========== VOIDING PHASE METRICS ==========");
            System.Diagnostics.Debug.WriteLine($"VoidedVolumeMl        = {result.VoidedVolumeMl}");
            System.Diagnostics.Debug.WriteLine($"AvgFlowMlPerSec       = {result.AvgFlowMlPerSec}");
            System.Diagnostics.Debug.WriteLine($"CompAvgFlowMlPerSec   = {result.CompAvgFlowMlPerSec}");
            System.Diagnostics.Debug.WriteLine($"PeakFlowMlPerSec      = {result.PeakFlowMlPerSec}");
            System.Diagnostics.Debug.WriteLine($"CompMaxFlowMlPerSec   = {result.CompMaxFlowMlPerSec}");

            System.Diagnostics.Debug.WriteLine($"DelayTimeSec          = {result.DelayTimeSec}");
            System.Diagnostics.Debug.WriteLine($"FlowTimeSec           = {result.FlowTimeSec}");
            System.Diagnostics.Debug.WriteLine($"VoidingTimeSec        = {result.VoidingTimeSec}");
            System.Diagnostics.Debug.WriteLine($"IntervalTimeSec       = {result.IntervalTimeSec}");
            System.Diagnostics.Debug.WriteLine($"MaxTimeSec            = {result.MaxTimeSec}");

            System.Diagnostics.Debug.WriteLine($"PvesAtQmax            = {result.PvesAtQmax}");
            System.Diagnostics.Debug.WriteLine($"PabdAtQmax            = {result.PabdAtQmax}");
            System.Diagnostics.Debug.WriteLine($"PdetAtQmax            = {result.PdetAtQmax}");

            System.Diagnostics.Debug.WriteLine($"Opening Pves          = {result.Opves}");
            System.Diagnostics.Debug.WriteLine($"Opening Pabd          = {result.Opabd}");
            System.Diagnostics.Debug.WriteLine($"Opening Pdet          = {result.Opdet}");

            System.Diagnostics.Debug.WriteLine($"MaxPvesVp             = {result.MaxPvesVp}");
            System.Diagnostics.Debug.WriteLine($"MaxPabdVp             = {result.MaxPabdVp}");
            System.Diagnostics.Debug.WriteLine($"MaxPdetVp             = {result.MaxPdetVp}");

            System.Diagnostics.Debug.WriteLine($"DelayIndex            = {result.DelayIndex}");
            System.Diagnostics.Debug.WriteLine($"FlowTimeCount         = {result.FlowTimeCount}");
            System.Diagnostics.Debug.WriteLine($"VoidingTimeCount      = {result.VoidingTimeCount}");
            System.Diagnostics.Debug.WriteLine($"MaxTimeCount          = {result.MaxTimeCount}");
            System.Diagnostics.Debug.WriteLine($"PeakIndex             = {result.PeakIndex}");

            System.Diagnostics.Debug.WriteLine($"BOOI                  = {result.BOOI}");
            System.Diagnostics.Debug.WriteLine($"BCI                   = {result.BCI}");

            System.Diagnostics.Debug.WriteLine($"ResidualUrine         = {result.ResidualUrine}");
            System.Diagnostics.Debug.WriteLine($"PretestResidualUrine  = {result.PretestResidualUrine}");
            System.Diagnostics.Debug.WriteLine($"Compru                = {result.Compru}");
            System.Diagnostics.Debug.WriteLine("============================================");

            return result;
        }
    }
}