//  ThermalDotNet
//  An ESC/POS serial thermal printer library
//  by yukimizake
//  https://github.com/yukimizake/ThermalDotNet

//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace ThermalPrinterNetCore
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Text;

    /// <summary>
    /// ESC/POS serial thermal printer library.
    /// https://github.com/yukimizake/ThermalDotNet
    /// This program is distributed under the GPLv3 license.
    /// </summary>
    public class ThermalPrinter
    {
        /// <summary>
        /// Delay between two picture lines. (in ms)
        /// </summary>
        public int PictureLineSleepTimeMs = 40;

        /// <summary>
        /// Delay between two text lines. (in ms)
        /// </summary>
        public int WriteLineSleepTimeMs = 0;

        private readonly byte heatingInterval = 2;
        private readonly byte heatingTime = 80;
        private readonly byte maxPrintingDots = 7;
        private readonly Stream stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThermalPrinter"/> class.
        /// </summary>
        /// <param name='stream'>Serial port used by printer.</param>
        /// <param name='maxPrintingDots'>Max printing dots (0-255), unit: (n+1)*8 dots, default: 7 ((7+1)*8 = 64 dots)</param>
        /// <param name='heatingTime'>Heating time (3-255), unit: 10µs, default: 80 (800µs)</param>
        /// <param name='heatingInterval'>Heating interval (0-255), unit: 10µs, default: 2 (20µs)</param>
        public ThermalPrinter(Stream stream, byte maxPrintingDots, byte heatingTime, byte heatingInterval)
        {
            this.EncodingName = "windows-1251";

            this.maxPrintingDots = maxPrintingDots;
            this.heatingTime = heatingTime;
            this.heatingInterval = heatingInterval;

            this.stream = stream;
            this.Reset();
            this.SetPrintingParameters(maxPrintingDots, heatingTime, heatingInterval);
            this.SetEncoding(this.EncodingName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThermalPrinter"/> class.
        /// </summary>
        /// <param name='stream'>Serial port used by printer.</param>
        public ThermalPrinter(Stream stream)
            : this(stream, 7, 80, 2)
        {
        }

        /// <summary>
        /// List of supported barcode types.
        /// </summary>
        public enum BarcodeType
        {
            /// <summary>
            /// UPC-A
            /// </summary>
            upc_a = 0,

            /// <summary>
            /// UPC-E
            /// </summary>
            upc_e = 1,

            /// <summary>
            /// EAN13
            /// </summary>
            ean13 = 2,

            /// <summary>
            /// EAN8
            /// </summary>
            ean8 = 3,

            /// <summary>
            /// CODE 39
            /// </summary>
            code39 = 4,

            /// <summary>
            /// I25
            /// </summary>
            i25 = 5,

            /// <summary>
            /// CODEBAR
            /// </summary>
            codebar = 6,

            /// <summary>
            /// CODE 93
            /// </summary>
            code93 = 7,

            /// <summary>
            /// CODE 128
            /// </summary>
            code128 = 8,

            /// <summary>
            /// CODE 11
            /// </summary>
            code11 = 9,

            /// <summary>
            /// MSI
            /// </summary>
            msi = 10
        }

        /// <summary>
        /// Returns a printing style.
        /// </summary>
        public enum PrintingStyle
        {
            /// <summary>
            /// White on black.
            /// </summary>
            Reverse = 1 << 1,

            /// <summary>
            /// Updown characters.
            /// </summary>
            Updown = 1 << 2,

            /// <summary>
            /// Bold characters.
            /// </summary>
            Bold = 1 << 3,

            /// <summary>
            /// Double height characters.
            /// </summary>
            DoubleHeight = 1 << 4,

            /// <summary>
            /// Double width characters.
            /// </summary>
            DoubleWidth = 1 << 5,

            /// <summary>
            /// Strikes text.
            /// </summary>
            DeleteLine = 1 << 6,

            /// <summary>
            /// Thin underline.
            /// </summary>
            Underline = 1 << 0,

            /// <summary>
            /// Thick underline.
            /// </summary>
            ThickUnderline = 1 << 7
        }

        /// <summary>
        /// Current encoding used by the printer.
        /// </summary>
        public string EncodingName { get; private set; }

        /// <summary>
        /// Sets bold mode off.
        /// </summary>
        public void BoldOff()
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(32);
            this.stream.WriteByte(0);
            this.stream.WriteByte(27);
            this.stream.WriteByte(69);
            this.stream.WriteByte(0);
        }

        /// <summary>
        /// Sets bold mode on.
        /// </summary>
        public void BoldOn()
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(32);
            this.stream.WriteByte(1);
            this.stream.WriteByte(27);
            this.stream.WriteByte(69);
            this.stream.WriteByte(1);
        }

        /// <summary>
        /// Prints the contents of the buffer and feeds n dots.
        /// </summary>
        /// <param name='dotsToFeed'>
        /// Number of dots to feed.
        /// </param>
        public void FeedDots(byte dotsToFeed)
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(74);
            this.stream.WriteByte(dotsToFeed);
        }

        /// <summary>
        /// Prints a horizontal line.
        /// </summary>
        /// <param name='length'>Line length (in characters) (max 32).</param>
        public void HorizontalLine(int length)
        {
            if (length > 0)
            {
                if (length > 32)
                {
                    length = 32;
                }

                for (int i = 0; i < length; i++)
                {
                    this.stream.WriteByte(0xC4);
                }
                this.stream.WriteByte(10);
            }
        }

        /// <summary>
        /// Idents the text.
        /// </summary>
        /// <param name='columns'>Number of columns.</param>
        public void Indent(byte columns)
        {
            if (columns < 0 || columns > 31)
            {
                columns = 0;
            }

            this.stream.WriteByte(27);
            this.stream.WriteByte(66);
            this.stream.WriteByte(columns);
        }

        /// <summary>
        /// Sets ialic mode off.
        /// </summary>
        public void ItalicOff()
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(32);
            this.stream.WriteByte(0);
            this.stream.WriteByte(27);
            this.stream.WriteByte(53);
            this.stream.WriteByte(0);
        }

        /// <summary>
        /// Sets italic mode on.
        /// </summary>
        public void ItalicOn()
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(32);
            this.stream.WriteByte(1);
            this.stream.WriteByte(27);
            this.stream.WriteByte(52);
            this.stream.WriteByte(1);
        }

        ///	<summary>
        /// Prints the contents of the buffer and feeds one line.
        /// </summary>
        public void LineFeed()
        {
            this.stream.WriteByte(10);
        }

        /// <summary>
        /// Prints the contents of the buffer and feeds n lines.
        /// </summary>
        /// <param name='lines'>Number of lines to feed.</param>
        public void LineFeed(byte lines)
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(100);
            this.stream.WriteByte(lines);
        }

        public void PaperCut()
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(105);
        }

        /// <summary>
        /// Prints the barcode data.
        /// </summary>
        /// <param name='type'>Type of barcode.</param>
        /// <param name='data'>Data to print.</param>
        public void PrintBarcode(BarcodeType type, string data)
        {
            byte[] originalBytes;
            byte[] outputBytes;

            if (type == BarcodeType.code93 || type == BarcodeType.code128)
            {
                originalBytes = System.Text.Encoding.UTF8.GetBytes(data);
                outputBytes = originalBytes;
            }
            else
            {
                originalBytes = System.Text.Encoding.UTF8.GetBytes(data.ToUpper());
                outputBytes = System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.GetEncoding(this.EncodingName), originalBytes);
            }

            switch (type)
            {
                case BarcodeType.upc_a:
                    if (data.Length == 11 || data.Length == 12)
                    {
                        this.stream.WriteByte(29);
                        this.stream.WriteByte(107);
                        this.stream.WriteByte(0);
                        this.stream.Write(outputBytes, 0, data.Length);
                        this.stream.WriteByte(0);
                    }
                    break;

                case BarcodeType.upc_e:
                    if (data.Length == 11 || data.Length == 12)
                    {
                        this.stream.WriteByte(29);
                        this.stream.WriteByte(107);
                        this.stream.WriteByte(1);
                        this.stream.Write(outputBytes, 0, data.Length);
                        this.stream.WriteByte(0);
                    }
                    break;

                case BarcodeType.ean13:
                    if (data.Length == 12 || data.Length == 13)
                    {
                        this.stream.WriteByte(29);
                        this.stream.WriteByte(107);
                        this.stream.WriteByte(2);
                        this.stream.Write(outputBytes, 0, data.Length);
                        this.stream.WriteByte(0);
                    }
                    break;

                case BarcodeType.ean8:
                    if (data.Length == 7 || data.Length == 8)
                    {
                        this.stream.WriteByte(29);
                        this.stream.WriteByte(107);
                        this.stream.WriteByte(3);
                        this.stream.Write(outputBytes, 0, data.Length);
                        this.stream.WriteByte(0);
                    }
                    break;

                case BarcodeType.code39:
                    if (data.Length > 1)
                    {
                        this.stream.WriteByte(29);
                        this.stream.WriteByte(107);
                        this.stream.WriteByte(4);
                        this.stream.Write(outputBytes, 0, data.Length);
                        this.stream.WriteByte(0);
                    }
                    break;

                case BarcodeType.i25:
                    if (data.Length > 1 || data.Length % 2 == 0)
                    {
                        this.stream.WriteByte(29);
                        this.stream.WriteByte(107);
                        this.stream.WriteByte(5);
                        this.stream.Write(outputBytes, 0, data.Length);
                        this.stream.WriteByte(0);
                    }
                    break;

                case BarcodeType.codebar:
                    if (data.Length > 1)
                    {
                        this.stream.WriteByte(29);
                        this.stream.WriteByte(107);
                        this.stream.WriteByte(6);
                        this.stream.Write(outputBytes, 0, data.Length);
                        this.stream.WriteByte(0);
                    }
                    break;

                case BarcodeType.code93: //todo: overload PrintBarcode method with a byte array parameter
                    if (data.Length > 1)
                    {
                        this.stream.WriteByte(29);
                        this.stream.WriteByte(107);
                        this.stream.WriteByte(7); //todo: use format 2 (init string : 29,107,72) (0x00 can be a value, too)
                        this.stream.Write(outputBytes, 0, data.Length);
                        this.stream.WriteByte(0);
                    }
                    break;

                case BarcodeType.code128: //todo: overload PrintBarcode method with a byte array parameter
                    if (data.Length > 1)
                    {
                        this.stream.WriteByte(29);
                        this.stream.WriteByte(107);
                        this.stream.WriteByte(8); //todo: use format 2 (init string : 29,107,73) (0x00 can be a value, too)
                        this.stream.Write(outputBytes, 0, data.Length);
                        this.stream.WriteByte(0);
                    }
                    break;

                case BarcodeType.code11:
                    if (data.Length > 1)
                    {
                        this.stream.WriteByte(29);
                        this.stream.WriteByte(107);
                        this.stream.WriteByte(9);
                        this.stream.Write(outputBytes, 0, data.Length);
                        this.stream.WriteByte(0);
                    }
                    break;

                case BarcodeType.msi:
                    if (data.Length > 1)
                    {
                        this.stream.WriteByte(29);
                        this.stream.WriteByte(107);
                        this.stream.WriteByte(10);
                        this.stream.Write(outputBytes, 0, data.Length);
                        this.stream.WriteByte(0);
                    }
                    break;
            }
        }

        public void PrintCodeTable()
        {
            for (byte b = 32; b < 255; b++)
            {
                this.WriteToBuffer(" " + b.ToString() + " ");
                this.stream.WriteByte(b);
            }
        }

        /// <summary>
        /// Prints the image. The image must be 384px wide.
        /// </summary>
        /// <param name='fileName'>Image file path.</param>
        public void PrintImage(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw (new Exception("File does not exist."));
            }

            this.PrintImage(new Bitmap(fileName));
        }

        /// <summary>
        /// Prints the image. The image must be 384px wide.
        /// </summary>
        /// <param name='image'>Image to print.</param>
        public void PrintImage(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;

            byte[,] imgArray = new byte[width, height];

            if (width != 384 || height > 65635)
            {
                throw new Exception("Image width must be 384px, height cannot exceed 65635px.");
            }

            // Processing image data
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < (image.Width / 8); x++)
                {
                    imgArray[x, y] = 0;
                    for (byte n = 0; n < 8; n++)
                    {
                        Color pixel = image.GetPixel(x * 8 + n, y);
                        if (pixel.GetBrightness() < 0.5)
                        {
                            imgArray[x, y] += (byte)(1 << n);
                        }
                    }
                }
            }

            // Print LSB first bitmap
            this.stream.WriteByte(18);
            this.stream.WriteByte(118);

            this.stream.WriteByte((byte)(height & 255));    //height LSB
            this.stream.WriteByte((byte)(height >> 8));     //height MSB

            for (int y = 0; y < height; y++)
            {
                System.Threading.Thread.Sleep(this.PictureLineSleepTimeMs);
                for (int x = 0; x < (width / 8); x++)
                {
                    this.stream.WriteByte(imgArray[x, y]);
                }
            }
        }

        public void PrintQRCode(string text)
        {
            // command（S-JIS）
            this.stream.WriteByte(0x1C);
            this.stream.WriteByte(0x43);
            this.stream.WriteByte(0x1);

            // command（QR code print）
            this.stream.WriteByte(0x1D);
            this.stream.WriteByte(0x51);
            this.stream.WriteByte(0x6);
            this.stream.WriteByte(0x4);
            this.stream.WriteByte(0x4);
            this.stream.WriteByte(0x14);
            this.stream.WriteByte(0x0);

            // command（QR code data）
            this.WriteToBuffer(text);
        }

        /// <summary>
        /// Resets the printer.
        /// </summary>
        public void Reset()
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(64);
        }

        /// <summary>
        /// Centers the text.
        /// </summary>
        public void SetAlignCenter()
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(97);
            this.stream.WriteByte(1);
        }

        /// <summary>
        /// Aligns the text to the left.
        /// </summary>
        public void SetAlignLeft()
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(97);
            this.stream.WriteByte(0);
        }

        /// <summary>
        /// Aligns the text to the right.
        /// </summary>
        public void SetAlignRight()
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(97);
            this.stream.WriteByte(2);
        }

        /// <summary>
        /// Sets the barcode left space.
        /// </summary>
        /// <param name='spacingDots'>Spacing dots.</param>
        public void SetBarcodeLeftSpace(byte spacingDots)
        {
            this.stream.WriteByte(29);
            this.stream.WriteByte(120);
            this.stream.WriteByte(spacingDots);
        }

        /// <summary>
        /// Selects large barcode mode.
        /// </summary>
        /// <param name='large'>Large barcode mode.</param>
        public void SetLargeBarcode(bool large)
        {
            if (large)
            {
                this.stream.WriteByte(29);
                this.stream.WriteByte(119);
                this.stream.WriteByte(3);
            }
            else
            {
                this.stream.WriteByte(29);
                this.stream.WriteByte(119);
                this.stream.WriteByte(2);
            }
        }

        /// <summary>
        /// Sets the line spacing.
        /// </summary>
        /// <param name='lineSpacing'>Line spacing (in dots), default value: 32 dots.</param>
        public void SetLineSpacing(byte lineSpacing)
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(51);
            this.stream.WriteByte(lineSpacing);
        }

        /// <summary>
        /// Sets the printing parameters.
        /// </summary>
        /// <param name='maxPrintingDots'>Max printing dots (0-255), unit: (n+1)*8 dots, default: 7 (beceause (7+1)*8 = 64 dots)</param>
        /// <param name='heatingTime'>Heating time (3-255), unit: 10µs, default: 80 (800µs)</param>
        /// <param name='heatingInterval'>Heating interval (0-255), unit: 10µs, default: 2 (20µs)</param>
        public void SetPrintingParameters(byte maxPrintingDots, byte heatingTime, byte heatingInterval)
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(55);
            this.stream.WriteByte(maxPrintingDots);
            this.stream.WriteByte(heatingTime);
            this.stream.WriteByte(heatingInterval);
        }

        /// <summary>
        /// Sets the text size.
        /// </summary>
        /// <param name='doubleWidth'>Double width</param>
        /// <param name='doubleHeight'>Double height</param>
        public void SetSize(bool doubleWidth, bool doubleHeight)
        {
            int sizeValue = (Convert.ToInt32(doubleWidth)) * (0xF0) + (Convert.ToInt32(doubleHeight)) * (0x0F);
            this.stream.WriteByte(29);
            this.stream.WriteByte(33);
            this.stream.WriteByte((byte)sizeValue);
        }

        /// <summary>
        /// Sets the printer offine.
        /// </summary>
        public void Sleep()
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(61);
            this.stream.WriteByte(0);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="ThermalDotNet.ThermalPrinter"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="ThermalDotNet.ThermalPrinter"/>.</returns>
        public override string ToString()
        {
            return string.Format("ThermalPrinter:\n\tthis.maxPrintingDots={1}," +
                "\n\tthis.heatingTime={2},\n\tthis.heatingInterval={3},\n\tPictureLineSleepTimeMs={4}," +
                "\n\tWriteLineSleepTimeMs={5},\n\tEncoding={6}", this.maxPrintingDots,
                this.heatingTime, this.heatingInterval, this.PictureLineSleepTimeMs, this.WriteLineSleepTimeMs, this.EncodingName);
        }

        /// <summary>
        /// Sets the printer online.
        /// </summary>
        public void WakeUp()
        {
            this.stream.WriteByte(27);
            this.stream.WriteByte(61);
            this.stream.WriteByte(1);
        }

        /// <summary>
        /// Sets white on black mode off.
        /// </summary>
        public void WhiteOnBlackOff()
        {
            this.stream.WriteByte(29);
            this.stream.WriteByte(66);
            this.stream.WriteByte(0);
        }

        /// <summary>
        /// Sets white on black mode on.
        /// </summary>
        public void WhiteOnBlackOn()
        {
            this.stream.WriteByte(29);
            this.stream.WriteByte(66);
            this.stream.WriteByte(1);
        }

        /// <summary>
        /// Prints the line of text.
        /// </summary>
        /// <param name='text'>Text to print.</param>
        public void WriteLine(string text)
        {
            this.WriteToBuffer(text);
            this.stream.WriteByte(10);
            System.Threading.Thread.Sleep(this.WriteLineSleepTimeMs);
        }

        /// <summary>
        /// Prints the line of text.
        /// </summary>
        /// <param name='text'>Text to print.</param>
        /// <param name='style'>Style of the text.</param>
        public void WriteLine(string text, PrintingStyle style)
        {
            this.WriteLine(text, (byte)style);
        }

        /// <summary>
        /// Prints the line of text.
        /// </summary>
        /// <param name='text'>Text to print.</param>
        /// <param name='style'>Style of the text. Can be the sum of PrintingStyle enums.</param>
        public void WriteLine(string text, byte style)
        {
            byte underlineHeight = 0;

            if (BitTest(style, 0))
            {
                style = BitClear(style, 0);
                underlineHeight = 1;
            }

            if (BitTest(style, 7))
            {
                style = BitClear(style, 7);
                underlineHeight = 2;
            }

            if (underlineHeight != 0)
            {
                this.stream.WriteByte(27);
                this.stream.WriteByte(45);
                this.stream.WriteByte(underlineHeight);
            }

            // Style on
            this.stream.WriteByte(27);
            this.stream.WriteByte(33);
            this.stream.WriteByte((byte)style);

            // Sends the text
            this.WriteLine(text);

            // Style off
            if (underlineHeight != 0)
            {
                this.stream.WriteByte(27);
                this.stream.WriteByte(45);
                this.stream.WriteByte(0);
            }

            this.stream.WriteByte(27);
            this.stream.WriteByte(33);
            this.stream.WriteByte(0);
        }

        /// <summary>
        /// Prints the line of text, double size.
        /// </summary>
        /// <param name='text'>Text to print.</param>
        public void WriteLine_Big(string text)
        {
            const byte DoubleHeight = 1 << 4;
            const byte DoubleWidth = 1 << 5;
            const byte Bold = 1 << 3;

            //big on
            this.stream.WriteByte(27);
            this.stream.WriteByte(33);
            this.stream.WriteByte(DoubleHeight + DoubleWidth + Bold);

            //Sends the text
            this.WriteLine(text);

            //big off
            this.stream.WriteByte(27);
            this.stream.WriteByte(33);
            this.stream.WriteByte(0);
        }

        /// <summary>
        /// Prints the line of text in bold.
        /// </summary>
        /// <param name='text'>Text to print.</param>
        public void WriteLine_Bold(string text)
        {
            // Bold on
            this.BoldOn();

            // Sends the text
            this.WriteLine(text);

            // Bold off
            this.BoldOff();

            this.LineFeed();
        }

        /// <summary>
        /// Prints the line of text, white on black.
        /// </summary>
        /// <param name='text'>Text to print.</param>
        public void WriteLine_Invert(string text)
        {
            //Sets inversion on
            this.stream.WriteByte(29);
            this.stream.WriteByte(66);
            this.stream.WriteByte(1);

            //Sends the text
            this.WriteLine(text);

            //Sets inversion off
            this.stream.WriteByte(29);
            this.stream.WriteByte(66);
            this.stream.WriteByte(0);

            this.LineFeed();
        }

        /// <summary>
        /// Sends the text to the printer buffer. Does not print until a line feed (0x10) is sent.
        /// </summary>
        /// <param name='text'>Text to print.</param>
        public void WriteToBuffer(string text)
        {
            text = text.Trim('\n').Trim('\r');
            byte[] originalBytes = Encoding.UTF8.GetBytes(text);
            byte[] outputBytes = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(this.EncodingName), originalBytes);
            this.stream.Write(outputBytes, 0, outputBytes.Length);
        }

        /// <summary>
        /// Return the given value with its n bit cleared.
        /// </summary>
        /// <param name="originalValue">The value to return</param>
        /// <param name="bit">The bit number to clear</param>
        /// <returns></returns>
        private static byte BitClear(byte originalValue, int bit)
        {
            return originalValue &= (byte)(~(1 << bit));
        }

        /// <summary>
        /// Return the given value with its n bit set.
        /// </summary>
        /// <param name="originalValue">The value to return</param>
        /// <param name="bit">The bit number to set</param>
        /// <returns></returns>
        private static byte BitSet(byte originalValue, byte bit)
        {
            return originalValue |= (byte)((byte)1 << bit);
        }

        /// <summary>
        /// Tests the value of a given bit.
        /// </summary>
        /// <param name="valueToTest">The value to test</param>
        /// <param name="testBit">The bit number to test</param>
        /// <returns></returns>
        private static bool BitTest(byte valueToTest, int testBit)
        {
            return ((valueToTest & (byte)(1 << testBit)) == (byte)(1 << testBit));
        }

        private void SetEncoding(string encoding)
        {
            switch (encoding)
            {
                case "IBM437":
                    this.stream.WriteByte(27);
                    this.stream.WriteByte(116);
                    this.stream.WriteByte(0);
                    break;

                case "ibm850":
                    this.stream.WriteByte(27);
                    this.stream.WriteByte(116);
                    this.stream.WriteByte(1);
                    break;

                case "windows-1251":
                    this.stream.WriteByte(27);
                    this.stream.WriteByte(116);
                    this.stream.WriteByte(15);
                    break;

                default:
                    break;
            }
        }
    }
}
