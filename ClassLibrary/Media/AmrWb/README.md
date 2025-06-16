# AMR-WB Codec Source Files

The source code files in this directory implement the Adaptive Multi Rate Wide Band (AMR-WB) audio codec described in the following document.

> 3rd Generation Partnership Project; Technical Specification Group Services and System Aspects; ANSI-C code for the
Adaptive Multi-Rate - Wideband (AMR-WB) speech codec (Release 17), [3GPP TS 26.173 V17.1.1 (2023-03)](https://portal.3gpp.org/desktopmodules/Specifications/SpecificationDetails.aspx?specificationId=1421).

The C# source code files located here are a bit-precise implementation of the ANSI-C implementation provided in the above standard. The ANSI-C implementation is a fixed point implementation of the AMR-WB codec and is considered a reference implementation (i.e. the gold standard implementation).

With the exception of the following files, the file names match the file names in the ANSI-C implementation provided in 3GPP TS 26.173 V17.1.1 (2023-03).
1. AmrWb.cs
2. PacketEncoder.cs
3. PacketDecoder.cs

The C functions provided in 3GPP TS 26.173 V17.1.1 (2023-03) have been re-coded into C# and wrapped in a class called AmrWb. The AmrWb class is not intended for public consumption. Applications can use the AmrWbEncoder and the AmrWbDecoder classes in the SipLib.Media namespace for encoding and decoding audio with an AMR-WB codec.
