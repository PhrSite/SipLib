/*
 * Copyright @ 2015 Atlassian Pty Ltd
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
/*
 * WARNING: The use of G.729 may require a license fee and/or royalty fee in
 * some countries and is licensed by
 * <a href="http://www.sipro.com">SIPRO Lab Telecom</a>.
 */

/*
 * @author Lubomir Marinov (translation of ITU-T C source code to Java)
 */
namespace SipLib.Media;

/// <summary>
/// This is the base class for the G729Encoder and G729Decoder classes. This class only contains definitions of constants
/// used for encoding and decoding G.729 audio data.
/// </summary>
public class Ld8k
{
    /**
    * gain adjustment factor
    */
    public static float AGC_FAC = 0.9875f;

    /**
    * gain adjustment factor
    */
    public static float AGC_FAC1 = 1.0f - AGC_FAC;

    /// <summary>
    /// Alpha coefficient
    /// </summary>
    public static float ALPHA = -6.0f;

    /// <summary>
    /// Beta coefficient
    /// </summary>
    public static float BETA = 1.0f;

    /**
    * Definition of zero-bit in bit-stream.
    */
    public static short BIT_0 = 0x007f;

    /*---------------------------------------------------------------------------*
    * Constants for bitstream packing                                           *
    *---------------------------------------------------------------------------*/
    /**
    * Definition of one-bit in bit-stream.
    */
    public static short BIT_1 = 0x0081;

    /// <summary>
    /// Const 12
    /// </summary>
    public static float CONST12 = 1.2f;

    /*---------------------------------------------------------------------------*
    * Constants for fixed codebook.                                            *
    *---------------------------------------------------------------------------*/

    /// <summary>
    /// Size of correlation matrix
    /// </summary>
    internal static int DIM_RR = 616;

    /// <summary>
    /// Resolution for fractionnal delay
    /// </summary>
    internal static int F_UP_PST = 8;

    internal static int L_INTER4 = 4;

    internal static int UP_SAMP = 3;

    internal static int FIR_SIZE_ANA = UP_SAMP * L_INTER4 + 1;

    internal static int L_INTER10 = 10;

    internal static int FIR_SIZE_SYN = UP_SAMP * L_INTER10 + 1;

    /*
    * Largest floating point number
    */
    internal static float FLT_MAX_G729 = float.MaxValue;

    /*
    * Largest floating point number
    */
    internal static float FLT_MIN_G729 = -FLT_MAX_G729;

    /*
    * maximum adaptive codebook gain
    */
    internal static float GAIN_PIT_MAX = 1.2f;

    /*
    * LT weighting factor
    */
    internal static float GAMMA_G = 0.5f;

    internal static float GAMMA1_0 = 0.98f;

    internal static float GAMMA1_1 = 0.94f;

    /*---------------------------------------------------------------------------
    * Constants for postfilter.
    *---------------------------------------------------------------------------
    */
    /* short term pst parameters :  */
    /*
    * denominator weighting factor
    */
    internal static float GAMMA1_PST = 0.7f;

    internal static float GAMMA2_0_H = 0.7f;

    internal static float GAMMA2_0_L = 0.4f;

    internal static float GAMMA2_1 = 0.6f;

    /*
    * numerator  weighting factor
    */
    internal static float GAMMA2_PST = 0.55f;

    /*
    * tilt weighting factor when k1<0
    */
    internal static float GAMMA3_MINUS = 0.9f;

    /*
    * tilt weighting factor when k1>0
    */
    internal static float GAMMA3_PLUS = 0.2f;

    internal static float GAP1 = 0.0012f;

    internal static float GAP2 = 0.0006f;

    internal static float GAP3 = 0.0392f;

    /*
    * Maximum pitch gain if taming is needed
    */
    internal static float GP0999 = 0.9999f;

    /*--------------------------------------------------------------------------*
    * Constants for taming procedure.                           *
    *--------------------------------------------------------------------------*/
    /*
    * Maximum pitch gain if taming is needed
    */
    internal static float GPCLIP = 0.95f;

    /*
    * Maximum pitch gain if taming is needed
    */
    internal static float GPCLIP2 = 0.94f;

    /*
    * Resolution of lsp search.
    */
    internal static int GRID_POINTS = 60;

    internal static float INV_COEF = -0.032623f;

    internal static int L_SUBFR = 40;

    internal static float INV_L_SUBFR = 1.0f / L_SUBFR; /* =0.025 */

    /*
    * LPC update frame size
    */
    internal static int L_FRAME = 80;
    /*
    * Length for pitch interpolation
    */

    /*
    * upsampling ration for pitch search
    */

    /*
    * Length of filter for interpolation.
    */
    internal static int L_INTERPOL = 10 + 1;

    internal static float L_LIMIT = 0.005f;

    /*
    * Samples of next frame needed for LPC ana.
    */
    internal static int L_NEXT = 40;
    /*
    * Sub-frame size
    */

    /* long term pst parameters :   */
    /*
    * Sub-frame size + 1
    */
    internal static int L_SUBFRP1 = L_SUBFR + 1;

    /*
    * Total size of speech buffer
    */
    internal static int L_TOTAL = 240;

    /*---------------------------------------------------------------------------*
    * Constants for lpc analysis and lsp quantizer.                             *
    *---------------------------------------------------------------------------*/
    /*
    * LPC analysis window size.
    */
    internal static int L_WINDOW = 240;

    internal static int LH2_L = 16;

    internal static int LH_UP_L = LH2_L / 2;

    internal static int LH2_S = 4;

    internal static int LH_UP_S = LH2_S / 2;
    /*
    * length of long interp. subfilters
    */
    internal static int LH2_L_P1 = LH2_L + 1;
    /*
    * length of short interp. subfilters
    */

    /*
    * impulse response length
    */
    internal static int LONG_H_ST = 20;

    /*
    * LPC order.
    */
    internal static int M = 10;

    internal static float M_LIMIT = 3.135f;

    /*
    * MA prediction order for LSP.
    */
    internal static int MA_NP = 4;

    internal static int MAX_TIME = 75;

    /*-------------------------------------------------------------------------
    * gain quantizer  constants
    *-------------------------------------------------------------------------
    */
    /*
    * Average innovation energy
    */
    internal static float MEAN_ENER = 36.0f;

    /* Array sizes */
    internal static int PIT_MAX = 143;

    internal static int MEM_RES2 = PIT_MAX + 1 + LH_UP_L;

    /*
    * LT gain minimum
    */
    internal static float MIN_GPLT = 1.0f / (1.0f + GAMMA_G);

    /*
    * Number of modes for MA prediction.
    */
    internal static int MODE = 2;

    /*
    * LPC order+1.
    */
    internal static int MP1 = M + 1;

    /*
    * Size of vectors for cross-correlation between 2 pulses
    */
    internal static int MSIZE = 64;

    /*
    * Number of positions for each pulse
    */
    internal static int NB_POS = 8;

    /*
    * LPC order / 2.
    */
    internal static int NC = M / 2;

    /*
    * Number of entries in first stage.
    */
    internal static int NC0_B = 7;

    internal static int NC0 = 1 << NC0_B;
    /*
    * Number of bits in first stage.
    */

    /*
    * Number of entries in second stage.
    */
    internal static int NC1_B = 5;

    internal static int NC1 = 1 << NC1_B;
    /*
    * Number of bits in second stage.
    */

    /*
    * Pre-selecting order for #1
    */
    internal static int NCAN1 = 4;

    /*
    * Pre-selecting order for #2
    */
    internal static int NCAN2 = 8;

    /*
    * Codebook 1 size
    */
    internal static int NCODE1_B = 3;

    internal static int NCODE1 = 1 << NCODE1_B;
    /**
    * Number of Codebook-bit
    */

    /*
    * Codebook 2 size
    */
    internal static int NCODE2_B = 4;

    internal static int NCODE2 = 1 << NCODE2_B;
    /*
    * Number of Codebook-bit
    */

    /* ITU-T G.729 Software Package Release 2 (November 2006) */
    /* ITU-T G.729 Annex C - Reference C code for floating point implementation of G.729 Version 1.01 of 15.September.98
    */

    /*
    ----------------------------------------------------------------------
                    COPYRIGHT NOTICE
    ----------------------------------------------------------------------
    ITU-T G.729 Annex C ANSI C source code
    Copyright (C) 1998, AT&T, France Telecom, NTT, University of
    Sherbrooke.  All rights reserved.

    ----------------------------------------------------------------------
    */

    /*
    File : LD8K.H
    Used for the floating point version of G.729 main body
    (not for G.729A)
    */

    /*---------------------------------------------------------------------------
    * ld8k.h - include file for all ITU-T 8 kb/s CELP coder routines
    *---------------------------------------------------------------------------
    */

    internal static float PI = 3.14159265358979323846f;

    /*
    * pi*0.04
    */
    internal static float PI04 = PI * 0.04f;

    /*
    * pi*0.92
    */
    internal static float PI92 = PI * 0.92f;
    /*
    * Maximum pitch lag in samples
    */

    /*----------------------------------------------------------------------------
    * Constants for long-term predictor
    *----------------------------------------------------------------------------
    */
    /*
    * Minimum pitch lag in samples
    */
    internal static int PIT_MIN = 20;

    /*
    * Number of parameters per 10 ms frame.
    */
    internal static int PRM_SIZE = 11;

    /*
    * Bits per frame.
    */
    internal static int SERIAL_SIZE = 82;

    /*
    * Maximum value of pitch sharpening
    */
    internal static float SHARPMAX = 0.7945f;

    /*
    * minimum value of pitch sharpening
    */
    internal static float SHARPMIN = 0.2f;

    internal static int SIZ_RES2 = MEM_RES2 + L_SUBFR;

    internal static int SIZ_TAB_HUP_L = (F_UP_PST - 1) * LH2_L;

    internal static int SIZ_TAB_HUP_S = (F_UP_PST - 1) * LH2_S;

    internal static int SIZ_Y_UP = (F_UP_PST - 1) * L_SUBFRP1;

    /*
    * Size of bitstream frame.
    */
    internal static short SIZE_WORD = 80;

    /*
    * Step betweem position of the same pulse.
    */
    internal static int STEP = 5;

    /*
    * Definition of frame erasure flag.
    */
    internal static short SYNC_WORD = 0x6b21;

    /*
    * threshold LT pst switch off
    */
    internal static float THRESCRIT = 0.5f;

    /*
    * Error threshold taming
    */
    internal static float THRESH_ERR = 60000.0f;

    internal static float THRESH_H1 = 0.65f;

    internal static float THRESH_H2 = 0.43f;

    /*-------------------------------------------------------------------------
    *  pwf constants
    *-------------------------------------------------------------------------
    */

    internal static float THRESH_L1 = -1.74f;

    internal static float THRESH_L2 = -1.52f;

    /*--------------------------------------------------------------------------*
    * Example values for threshold and approximated worst case complexity:     *
    *                                                                          *
    *     threshold=0.40   maxtime= 75   extra=30   Mips =  6.0                *
    *--------------------------------------------------------------------------*/
    internal static float THRESHFCB = 0.40f;

    /*
    * Threshold to favor smaller pitch lags
    */
    internal static float THRESHPIT = 0.85f;

    /*
    * resolution of fractional delays
    */
}
